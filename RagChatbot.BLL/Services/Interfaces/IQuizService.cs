using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IQuizService
    {
        Task<QuizDto> GenerateQuizAsync(Guid documentId, int numberOfQuestions, int userId);
        Task<QuizDto> GetQuizByIdAsync(int quizId);
        Task<List<QuizDto>> GetQuizzesByDocumentAsync(Guid documentId);
        Task<QuizResultDto> SubmitQuizAsync(int quizId, int userId, Dictionary<int, string> answers);
        Task<List<QuizResultDto>> GetUserQuizResultsAsync(int userId, Guid? documentId = null);
    }
}
