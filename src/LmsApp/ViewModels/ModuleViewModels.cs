using System.ComponentModel.DataAnnotations;

namespace LmsApp.ViewModels;

public class ModuleCreateViewModel
{
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Judul modul wajib diisi")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }   // HTML dari Quill

    [Url(ErrorMessage = "Format URL tidak valid")]
    public string? VideoUrl { get; set; }

    [Range(1, 600, ErrorMessage = "Durasi antara 1-600 menit")]
    public int? DurationMinutes { get; set; }

    public bool IsPublished { get; set; } = true;
}
