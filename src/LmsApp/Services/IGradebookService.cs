using LmsApp.DTOs;
using LmsApp.Models;

namespace LmsApp.Services;

public interface IGradebookService
{
    // Grade item management
    Task<List<GradeItemConfigDto>> GetGradeItemsAsync(int courseId);
    Task<GradeItemConfigDto> CreateGradeItemAsync(int courseId, CreateGradeItemRequest req, string createdByUserId);
    Task<GradeItemConfigDto?> UpdateWeightAsync(int gradeItemId, double weight, string requestUserId);
    Task<bool> DeleteGradeItemAsync(int gradeItemId, string requestUserId);

    // Manual grading
    Task<CourseGradeEntry> SetManualGradeAsync(int gradeItemId, SetManualGradeRequest req, string gradedByUserId);

    // Gradebook views
    Task<WeightedGradebookView?> GetStudentViewAsync(int courseId, string userId);
    Task<List<WeightedStudentRow>> GetTeacherViewAsync(int courseId);
}
