using LmsApp.Data;
using LmsApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Naikkan batas body Kestrel ke 100 MB — per-endpoint [RequestSizeLimit] tidak cukup
// jika body sudah mulai dibaca sebelum filter berjalan
builder.WebHost.ConfigureKestrel(k =>
    k.Limits.MaxRequestBodySize = 100 * 1024 * 1024);   // 100 MB

// ── Database ──────────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";

static bool IsSqlite(string s)    => s.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase);
static bool IsSqlServer(string s) => s.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                                  || s.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase)
                                  || s.Contains("Trusted_Connection=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<LmsDbContext>(options =>
{
    if      (IsSqlite(connStr))    options.UseSqlite(connStr);
    else if (IsSqlServer(connStr)) options.UseSqlServer(connStr);
    else                           options.UseNpgsql(connStr);
});

// ── Keycloak JWT Authentication ───────────────────────────────────────────────
var authority = builder.Configuration["Keycloak:Authority"]
    ?? throw new InvalidOperationException("Keycloak:Authority is not configured.");
var clientId = builder.Configuration["Keycloak:ClientId"]
    ?? throw new InvalidOperationException("Keycloak:ClientId is not configured.");
var allowedClientId = builder.Configuration["Keycloak:AllowedClientId"]
    ?? throw new InvalidOperationException("Keycloak:AllowedClientId is not configured.");
var requireHttps = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata", true);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Authority otomatis fetch OIDC discovery document dari Keycloak:
        // - mengambil JWKS untuk verifikasi signature
        // - mengambil issuer untuk validasi iss claim
        options.Authority = authority;
        options.RequireHttpsMetadata = requireHttps;

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            // ── Issuer ───────────────────────────────────────────────────────
            // Authority sudah auto-validate issuer via OIDC discovery.
            // Kita pin eksplisit sebagai defense-in-depth — token dari realm lain
            // dengan JWKS yang bocor tidak akan bisa masuk.
            ValidateIssuer = true,
            ValidIssuer = authority,    // harus tepat sama dengan iss di token

            // ── Audience ─────────────────────────────────────────────────────
            // Token harus mengandung "lms-app" di claim aud.
            // Keycloak perlu dikonfigurasi: dwi-mobile client → Audience mapper
            // → Included Client Audience: lms-app
            ValidateAudience = true,
            ValidAudience = clientId,   // "lms-app"

            // ── Lifetime ─────────────────────────────────────────────────────
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30), // toleransi clock skew antar server

            // ── Claims mapping ───────────────────────────────────────────────
            NameClaimType = "preferred_username",
            RoleClaimType = "roles",
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // ── azp (authorized party) ───────────────────────────────────
                // Setelah iss + aud lolos, pastikan token DIMINTA oleh DWI Mobile.
                // Ini mencegah token valid dari client lain (mis. Postman langsung
                // ke Keycloak dengan client lms-app) masuk ke LMS API.
                var azp = context.Principal?.FindFirst("azp")?.Value;
                if (string.IsNullOrEmpty(azp) || azp != allowedClientId)
                {
                    context.Fail($"Token ditolak: azp '{azp}' bukan client yang diizinkan.");
                    return Task.CompletedTask;
                }

                // ── sub (subject) ────────────────────────────────────────────
                // Pastikan token mengandung identitas user (bukan token mesin).
                var sub = context.Principal?.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(sub))
                {
                    context.Fail("Token ditolak: tidak mengandung 'sub' claim.");
                }

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                // Log detail error tanpa expose ke client
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── Claims Transformation (inject LMS role from DB) ───────────────────────────
builder.Services.AddScoped<IClaimsTransformation, AppUserClaimsTransformation>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:3000"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── App Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IForumService, ForumService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPrerequisiteService, PrerequisiteService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddHostedService<AssignmentReminderService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICourseSectionService, CourseSectionService>();
builder.Services.AddScoped<ICompletionService, CompletionService>();
builder.Services.AddScoped<IGradebookService, GradebookService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IPracticeQuizService, PracticeQuizService>();
builder.Services.AddSingleton<MandatoryExamTokenService>();
builder.Services.AddScoped<AiQuestionService>();
builder.Services.AddScoped<MaterialContextService>();
builder.Services.AddHttpClient(); // untuk webhook DWI Mobile

// Client khusus DekaLLM — pakai SocketsHttpHandler dengan managed TLS
builder.Services.AddHttpClient("dekallm", client =>
{
    client.Timeout = TimeSpan.FromSeconds(300);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
    {
        RemoteCertificateValidationCallback = (_, _, _, _) => true,
        EnabledSslProtocols =
            System.Security.Authentication.SslProtocols.Tls12 |
            System.Security.Authentication.SslProtocols.Tls13,
    },
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
});

// Client lama untuk OpenAI (dipertahankan jika sewaktu-waktu dipakai)
builder.Services.AddHttpClient("openai", client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    // Pakai .NET managed TLS stack (bukan Windows SChannel)
    // agar tidak bergantung pada cipher/protocol Windows
    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
    {
        RemoteCertificateValidationCallback = (_, _, _, _) => true,
        EnabledSslProtocols =
            System.Security.Authentication.SslProtocols.Tls12 |
            System.Security.Authentication.SslProtocols.Tls13,
    },
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Global: 100 req/menit per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Upload endpoint: lebih ketat — 10 req/menit per IP
    options.AddFixedWindowLimiter("upload", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });

    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Terlalu banyak request. Coba lagi nanti." }, token);
    };
});

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Keycloak access token. Format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LMS API v1"));
}

// Global exception handler — jangan expose stack trace ke client
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(new
    {
        message = "Terjadi kesalahan pada server. Silakan coba lagi."
    });
}));

app.UseRateLimiter();
app.UseCors("Frontend");
app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseAuthentication();
if (app.Environment.IsDevelopment())
    app.UseMiddleware<MockAuthMiddleware>(); // bypass JWT pakai X-Mock-User-Id header
app.UseAuthorization();
app.MapControllers();

// ── Auto-create schema + seed data on startup ─────────────────────────────────
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
    var isSqlite    = IsSqlite(connStr);
    var isSqlServer = IsSqlServer(connStr);

    if (isSqlServer)
    {
        // SQL Server: database WCI_APP sudah ada (shared DB), jadi EnsureCreated() tidak buat tabel.
        // Cek apakah tabel LMS di schema "lms" sudah ada — jika belum, buat semua tabel.
        var lmsTablesExist = false;
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'lms' AND TABLE_NAME = 'Courses'");
            lmsTablesExist = db.Courses.Any();
        }
        catch { /* tabel belum ada */ }

        if (!lmsTablesExist)
        {
            var creator = db.GetService<IRelationalDatabaseCreator>() as RelationalDatabaseCreator;
            if (creator != null)
            {
                try { creator.CreateTables(); }
                catch (Exception ex)
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("CreateTables: {Message}", ex.Message);
                }
            }
        }
        else
        {
            // Add new columns to existing tables (safe ALTER TABLE IF NOT EXISTS)
            var alterSqls = new[]
            {
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('lms.MandatoryExamSessions') AND name='IsLinkToken') ALTER TABLE lms.MandatoryExamSessions ADD IsLinkToken BIT NOT NULL DEFAULT 0",
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('lms.MandatoryExamSessions') AND name='ParentSessionId') ALTER TABLE lms.MandatoryExamSessions ADD ParentSessionId INT NULL",
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('lms.MandatoryExamSessions') AND name='MaxUsageCount') ALTER TABLE lms.MandatoryExamSessions ADD MaxUsageCount INT NOT NULL DEFAULT 5",
                "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('lms.MandatoryExamSessions') AND name='CurrentUsageCount') ALTER TABLE lms.MandatoryExamSessions ADD CurrentUsageCount INT NOT NULL DEFAULT 0",
            };
            foreach (var sql in alterSqls)
            {
                try { await db.Database.ExecuteSqlRawAsync(sql); }
                catch { /* ignore if fails */ }
            }
        }
    }
    else if (isSqlite)
    {
        // SQLite: drop + recreate jika tabel baru belum ada (EnsureCreated tidak update schema)
        if (app.Environment.IsDevelopment())
        {
            try
            {
                _ = db.CourseResources.Any();
                _ = db.Conversations.Any();
                _ = db.ActivityLogs.Any();
                _ = db.PracticeQuizzes.Any();
                _ = db.CourseSections.Any();
                // Deteksi kolom SectionId di CourseModules — jika kolom belum ada akan throw
                _ = db.CourseModules.Any(m => m.SectionId == null);
                // Deteksi tabel Exam baru
                _ = db.Exams.Any();
                // Deteksi tabel QuestionSet baru
                _ = db.QuestionSets.Any();
                // Deteksi tabel CourseQuestionBank baru
                _ = db.CourseQuestionBanks.Any();
                // Deteksi kolom PublicAccessCode di MandatoryExams
                _ = db.MandatoryExams.Any(e => e.PublicAccessCode == null);
                // Deteksi kolom IsLinkToken di MandatoryExamSessions
                _ = db.MandatoryExamSessions.Any(s => s.IsLinkToken == false);
                // Deteksi kolom ShowAnswers di Quizzes
                _ = db.Quizzes.Any(q => q.ShowAnswers == false);
            }
            catch
            {
                db.Database.EnsureDeleted();
            }
        }
        db.Database.EnsureCreated();
    }
    else
    {
        // PostgreSQL: EnsureCreated buat DB + semua tabel jika DB baru.
        // Untuk DB yang sudah ada, jalankan CREATE TABLE IF NOT EXISTS untuk tabel baru.
        db.Database.EnsureCreated();

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Conversations" (
                "Id"        SERIAL PRIMARY KEY,
                "User1Id"   TEXT NOT NULL DEFAULT '',
                "User1Name" TEXT NOT NULL DEFAULT '',
                "User2Id"   TEXT NOT NULL DEFAULT '',
                "User2Name" TEXT NOT NULL DEFAULT '',
                "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Messages" (
                "Id"             SERIAL PRIMARY KEY,
                "ConversationId" INTEGER NOT NULL
                    REFERENCES "Conversations"("Id") ON DELETE CASCADE,
                "SenderId"   TEXT NOT NULL DEFAULT '',
                "SenderName" TEXT NOT NULL DEFAULT '',
                "Content"    TEXT NOT NULL DEFAULT '',
                "IsRead"     BOOLEAN NOT NULL DEFAULT FALSE,
                "CreatedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ActivityLogs" (
                "Id"          SERIAL PRIMARY KEY,
                "UserId"      TEXT NOT NULL DEFAULT '',
                "UserName"    TEXT NOT NULL DEFAULT '',
                "Action"      TEXT NOT NULL DEFAULT '',
                "EntityType"  TEXT NOT NULL DEFAULT '',
                "EntityId"    INTEGER,
                "EntityTitle" TEXT,
                "CourseId"    INTEGER,
                "Metadata"    TEXT,
                "Timestamp"   TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "PracticeQuizzes" (
                "Id"               SERIAL PRIMARY KEY,
                "Title"            TEXT NOT NULL DEFAULT '',
                "Description"      TEXT,
                "QuestionCount"    INTEGER NOT NULL DEFAULT 10,
                "ShuffleQuestions" BOOLEAN NOT NULL DEFAULT TRUE,
                "ShuffleOptions"   BOOLEAN NOT NULL DEFAULT TRUE,
                "TimeLimitMinutes" INTEGER,
                "IsActive"         BOOLEAN NOT NULL DEFAULT TRUE,
                "CreatedBy"        TEXT NOT NULL DEFAULT '',
                "CreatedByName"    TEXT NOT NULL DEFAULT '',
                "CreatedAt"        TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "PracticeAttempts" (
                "Id"              SERIAL PRIMARY KEY,
                "PracticeQuizId"  INTEGER NOT NULL
                    REFERENCES "PracticeQuizzes"("Id") ON DELETE CASCADE,
                "UserId"          TEXT NOT NULL DEFAULT '',
                "UserName"        TEXT NOT NULL DEFAULT '',
                "StartedAt"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "SubmittedAt"     TIMESTAMPTZ,
                "Score"           DOUBLE PRECISION,
                "TotalQuestions"  INTEGER NOT NULL DEFAULT 0,
                "CorrectAnswers"  INTEGER
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "PracticeAttemptAnswers" (
                "Id"               SERIAL PRIMARY KEY,
                "AttemptId"        INTEGER NOT NULL
                    REFERENCES "PracticeAttempts"("Id") ON DELETE CASCADE,
                "BankQuestionId"   INTEGER NOT NULL
                    REFERENCES "QuestionBank"("Id") ON DELETE RESTRICT,
                "SelectedOptionId" INTEGER
                    REFERENCES "QuestionBankOptions"("Id") ON DELETE SET NULL,
                "DisplayOrder"     INTEGER NOT NULL DEFAULT 0
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "QuestionSets" (
                "Id"               SERIAL PRIMARY KEY,
                "Title"            TEXT NOT NULL DEFAULT '',
                "Description"      TEXT,
                "TimeLimitMinutes" INTEGER,
                "MaxAttempts"      INTEGER NOT NULL DEFAULT 1,
                "PassScore"        INTEGER NOT NULL DEFAULT 60,
                "IsPublished"      BOOLEAN NOT NULL DEFAULT FALSE,
                "CreatedBy"        TEXT NOT NULL DEFAULT '',
                "CreatedByName"    TEXT NOT NULL DEFAULT '',
                "CreatedAt"        TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "QuestionSetQuestions" (
                "Id"             SERIAL PRIMARY KEY,
                "QuestionSetId"  INTEGER NOT NULL
                    REFERENCES "QuestionSets"("Id") ON DELETE CASCADE,
                "BankQuestionId" INTEGER,
                "Text"           TEXT NOT NULL DEFAULT '',
                "Type"           INTEGER NOT NULL DEFAULT 0,
                "Points"         INTEGER NOT NULL DEFAULT 10,
                "Order"          INTEGER NOT NULL DEFAULT 0
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "QuestionSetOptions" (
                "Id"         SERIAL PRIMARY KEY,
                "QuestionId" INTEGER NOT NULL
                    REFERENCES "QuestionSetQuestions"("Id") ON DELETE CASCADE,
                "Text"       TEXT NOT NULL DEFAULT '',
                "IsCorrect"  BOOLEAN NOT NULL DEFAULT FALSE
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "QuestionSetAttempts" (
                "Id"             SERIAL PRIMARY KEY,
                "QuestionSetId"  INTEGER NOT NULL
                    REFERENCES "QuestionSets"("Id") ON DELETE CASCADE,
                "UserId"         TEXT NOT NULL DEFAULT '',
                "UserName"       TEXT NOT NULL DEFAULT '',
                "StartedAt"      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "SubmittedAt"    TIMESTAMPTZ,
                "Score"          INTEGER NOT NULL DEFAULT 0,
                "MaxScore"       INTEGER NOT NULL DEFAULT 0,
                "IsPassed"       BOOLEAN NOT NULL DEFAULT FALSE
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "QuestionSetAnswers" (
                "Id"               SERIAL PRIMARY KEY,
                "AttemptId"        INTEGER NOT NULL
                    REFERENCES "QuestionSetAttempts"("Id") ON DELETE CASCADE,
                "QuestionId"       INTEGER NOT NULL
                    REFERENCES "QuestionSetQuestions"("Id") ON DELETE RESTRICT,
                "SelectedOptionId" INTEGER,
                "EssayAnswer"      TEXT,
                "IsCorrect"        BOOLEAN,
                "EarnedPoints"     INTEGER,
                "Feedback"         TEXT
            )
            """);

        // ── Course Question Bank ──────────────────────────────────────────────
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "CourseQuestionBanks" (
                "Id"            SERIAL PRIMARY KEY,
                "CourseId"      INTEGER NOT NULL
                    REFERENCES "Courses"("Id") ON DELETE CASCADE,
                "ModuleId"      INTEGER
                    REFERENCES "CourseModules"("Id") ON DELETE SET NULL,
                "Text"          TEXT NOT NULL DEFAULT '',
                "Type"          INTEGER NOT NULL DEFAULT 0,
                "Points"        INTEGER NOT NULL DEFAULT 10,
                "Explanation"   TEXT,
                "CreatedBy"     TEXT NOT NULL DEFAULT '',
                "CreatedByName" TEXT NOT NULL DEFAULT '',
                "CreatedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "CourseQuestionBankOptions" (
                "Id"                    SERIAL PRIMARY KEY,
                "CourseQuestionBankId"  INTEGER NOT NULL
                    REFERENCES "CourseQuestionBanks"("Id") ON DELETE CASCADE,
                "Text"                  TEXT NOT NULL DEFAULT '',
                "IsCorrect"             BOOLEAN NOT NULL DEFAULT FALSE
            )
            """);

        // ── Mandatory Exam ────────────────────────────────────────────────────
        // Tambah kolom PublicAccessCode jika belum ada (upgrade DB yang sudah ada)
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "MandatoryExams"
            ADD COLUMN IF NOT EXISTS "PublicAccessCode" TEXT
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_MandatoryExams_PublicAccessCode"
            ON "MandatoryExams" ("PublicAccessCode")
            WHERE "PublicAccessCode" IS NOT NULL
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "MandatoryExams" (
                "Id"               SERIAL PRIMARY KEY,
                "Title"            TEXT NOT NULL DEFAULT '',
                "Description"      TEXT,
                "TimeLimitMinutes" INTEGER,
                "MaxAttempts"      INTEGER NOT NULL DEFAULT 1,
                "PassScore"        INTEGER NOT NULL DEFAULT 60,
                "IsActive"         BOOLEAN NOT NULL DEFAULT TRUE,
                "CreatedBy"        TEXT NOT NULL DEFAULT '',
                "CreatedByName"    TEXT NOT NULL DEFAULT '',
                "CreatedAt"        TIMESTAMPTZ NOT NULL DEFAULT NOW()
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "MandatoryExamQuestions" (
                "Id"      SERIAL PRIMARY KEY,
                "ExamId"  INTEGER NOT NULL
                    REFERENCES "MandatoryExams"("Id") ON DELETE CASCADE,
                "Text"    TEXT NOT NULL DEFAULT '',
                "Type"    INTEGER NOT NULL DEFAULT 0,
                "Points"  INTEGER NOT NULL DEFAULT 10,
                "Order"   INTEGER NOT NULL DEFAULT 0
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "MandatoryExamOptions" (
                "Id"         SERIAL PRIMARY KEY,
                "QuestionId" INTEGER NOT NULL
                    REFERENCES "MandatoryExamQuestions"("Id") ON DELETE CASCADE,
                "Text"       TEXT NOT NULL DEFAULT '',
                "IsCorrect"  BOOLEAN NOT NULL DEFAULT FALSE
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "MandatoryExamAssignments" (
                "Id"          SERIAL PRIMARY KEY,
                "ExamId"      INTEGER NOT NULL
                    REFERENCES "MandatoryExams"("Id") ON DELETE CASCADE,
                "UserId"      TEXT NOT NULL DEFAULT '',
                "UserName"    TEXT NOT NULL DEFAULT '',
                "Status"      INTEGER NOT NULL DEFAULT 0,
                "AssignedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "CompletedAt" TIMESTAMPTZ,
                UNIQUE ("ExamId", "UserId")
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "MandatoryExamAttempts" (
                "Id"           SERIAL PRIMARY KEY,
                "AssignmentId" INTEGER NOT NULL
                    REFERENCES "MandatoryExamAssignments"("Id") ON DELETE CASCADE,
                "ExamId"       INTEGER NOT NULL,
                "UserId"       TEXT NOT NULL DEFAULT '',
                "StartedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "SubmittedAt"  TIMESTAMPTZ,
                "Score"        INTEGER,
                "MaxScore"     INTEGER,
                "IsPassed"     BOOLEAN
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "MandatoryExamAnswers" (
                "Id"               SERIAL PRIMARY KEY,
                "AttemptId"        INTEGER NOT NULL
                    REFERENCES "MandatoryExamAttempts"("Id") ON DELETE CASCADE,
                "QuestionId"       INTEGER NOT NULL
                    REFERENCES "MandatoryExamQuestions"("Id") ON DELETE RESTRICT,
                "SelectedOptionId" INTEGER,
                "EssayAnswer"      TEXT,
                "IsCorrect"        BOOLEAN,
                "EarnedPoints"     INTEGER,
                "Feedback"         TEXT
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "MandatoryExamSessions" (
                "Id"          SERIAL PRIMARY KEY,
                "ExamId"      INTEGER NOT NULL
                    REFERENCES "MandatoryExams"("Id") ON DELETE CASCADE,
                "UserId"      TEXT NOT NULL DEFAULT '',
                "TokenJti"    TEXT NOT NULL UNIQUE,
                "GeneratedBy" TEXT NOT NULL DEFAULT '',
                "GeneratedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "ExpiresAt"   TIMESTAMPTZ NOT NULL,
                "UsedAt"      TIMESTAMPTZ,
                "IsRevoked"   BOOLEAN NOT NULL DEFAULT FALSE
            )
            """);
    }

    await DataSeeder.SeedAsync(db);
}

app.Run();
