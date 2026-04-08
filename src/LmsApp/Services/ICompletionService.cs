using LmsApp.DTOs;
using LmsApp.Models;

namespace LmsApp.Services;

public interface ICompletionService
{
    /// <summary>
    /// Cek apakah student sudah memenuhi semua completion rules course ini.
    /// Dipanggil setelah setiap modul, assignment, atau quiz selesai.
    /// </summary>
    Task<CompletionStatusDto> GetStatusAsync(int courseId, string userId);

    /// <summary>
    /// Cek completion dan langsung terbitkan sertifikat jika syarat terpenuhi.
    /// Idempotent: tidak akan buat duplikat jika sertifikat sudah ada.
    /// Kembalikan Certificate jika berhasil diterbitkan, null jika belum memenuhi syarat.
    /// </summary>
    Task<Certificate?> TryIssueCertificateAsync(int courseId, string userId, string userName);

    /// <summary>
    /// Ambil sertifikat milik user untuk course tertentu. Null jika belum ada.
    /// </summary>
    Task<CertificateDto?> GetCertificateAsync(int courseId, string userId);

    /// <summary>
    /// Semua sertifikat milik user (untuk halaman "Sertifikat Saya").
    /// </summary>
    Task<IEnumerable<CertificateDto>> GetUserCertificatesAsync(string userId);

    /// <summary>
    /// Verifikasi sertifikat berdasarkan nomor (publik, tanpa auth).
    /// </summary>
    Task<CertificateDto?> VerifyAsync(string certificateNumber);
}
