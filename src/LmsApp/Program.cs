using LmsApp.Data;
using LmsApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Gunakan SQLite jika connection string dimulai dengan "Data Source"
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddDbContext<LmsDbContext>(options =>
{
    if (connStr.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connStr);
    else
        options.UseNpgsql(connStr);
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
    var isSqlite = connStr.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase);

    if (isSqlite)
    {
        // SQLite: drop + recreate jika tabel baru belum ada (EnsureCreated tidak update schema)
        if (app.Environment.IsDevelopment())
        {
            try
            {
                _ = db.CourseResources.Any();
                _ = db.Conversations.Any();
                _ = db.ActivityLogs.Any();
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
    }

    await DataSeeder.SeedAsync(db);
}

app.Run();
