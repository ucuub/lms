using System.ComponentModel.DataAnnotations;
using LmsApp.Models;

namespace LmsApp.ViewModels;

public class CourseDetailsViewModel
{
    public Course Course { get; set; } = null!;
    public bool IsEnrolled { get; set; }
    public int EnrollmentCount { get; set; }
}

public class CourseCreateViewModel
{
    [Required(ErrorMessage = "Judul kursus wajib diisi")]
    [MaxLength(200, ErrorMessage = "Judul maksimal 200 karakter")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Deskripsi wajib diisi")]
    public string Description { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = false;
}
