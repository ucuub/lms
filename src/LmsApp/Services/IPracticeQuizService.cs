using LmsApp.DTOs;

namespace LmsApp.Services;

public interface IPracticeQuizService
{
    Task<List<PracticeQuizDto>> GetAllAsync(string userId);
    Task<PracticeQuizDto> CreateAsync(CreatePracticeQuizRequest req, string userId, string userName);
    Task DeleteAsync(int quizId, string userId);

    Task<StartPracticeResponse> StartAttemptAsync(int quizId, string userId, string userName);
    Task<PracticeResultDto> SubmitAttemptAsync(int attemptId, string userId, SubmitPracticeRequest req);
    Task<PracticeResultDto> GetResultAsync(int attemptId, string userId);
    Task<List<PracticeAttemptSummaryDto>> GetMyAttemptsAsync(string userId);
}
