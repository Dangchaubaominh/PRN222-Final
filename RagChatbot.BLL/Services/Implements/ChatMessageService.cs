using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class ChatMessageService : IChatMessageService
    {
        private readonly IChatMessageRepository _repository;

        public ChatMessageService(IChatMessageRepository repository)
        {
            _repository = repository;
        }

        public int Save(int userId, Guid subjectId, string sender, string content, IReadOnlyList<SourceCitationDto>? sources = null)
        {
            if (string.IsNullOrWhiteSpace(content)) return 0;
            var message = new ChatMessage
            {
                UserId    = userId,
                SubjectId = subjectId,
                Sender    = sender,
                Content   = content,
                SourcesJson = sources is { Count: > 0 } ? JsonSerializer.Serialize(sources) : null,
                CreatedAt = DateTime.UtcNow
            };
            _repository.Add(message);
            return message.Id;
        }

        public void UpdateFeedback(int messageId, int userId, FeedbackType? feedback)
        {
            var msg = _repository.GetById(messageId);
            if (msg != null && msg.UserId == userId)
            {
                msg.Feedback = feedback;
                _repository.Update(msg);
            }
        }

        public IEnumerable<ChatMessageDto> GetHistory(int userId, Guid subjectId, int take = 50)
            => _repository.GetHistory(userId, subjectId, take).Select(m => new ChatMessageDto
            {
                Id        = m.Id,
                Sender    = m.Sender,
                Content   = m.Content,
                Sources   = DeserializeSources(m.SourcesJson),
                Feedback  = m.Feedback,
                CreatedAt = m.CreatedAt
            });

        private static IReadOnlyList<SourceCitationDto> DeserializeSources(string? sourcesJson)
        {
            if (string.IsNullOrWhiteSpace(sourcesJson))
                return Array.Empty<SourceCitationDto>();

            try
            {
                var sources = JsonSerializer.Deserialize<List<SourceCitationDto>>(sourcesJson);
                if (sources == null)
                    return Array.Empty<SourceCitationDto>();

                return sources;
            }
            catch (JsonException)
            {
                return Array.Empty<SourceCitationDto>();
            }
        }
    }
}
