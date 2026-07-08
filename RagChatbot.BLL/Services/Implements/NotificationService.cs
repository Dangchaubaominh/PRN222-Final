using System;
using System.Collections.Generic;
using System.Linq;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;

namespace RagChatbot.BLL.Services.Implements
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;

        public NotificationService(INotificationRepository repository)
        {
            _repository = repository;
        }

        public NotificationDto Create(int userId, string message, string type = "info", string? linkUrl = null)
        {
            var entity = new Notification
            {
                UserId    = userId,
                Message   = message,
                Type      = type,
                LinkUrl   = linkUrl,
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            };
            _repository.Add(entity);
            return ToDto(entity);
        }

        public IEnumerable<NotificationDto> GetRecent(int userId, int take = 20)
            => _repository.GetByUser(userId, take).Select(ToDto);

        public int GetUnreadCount(int userId) => _repository.CountUnread(userId);

        public void MarkAllRead(int userId) => _repository.MarkAllRead(userId);

        public void Delete(int id, int userId) => _repository.Delete(id, userId);

        public void DeleteAll(int userId) => _repository.DeleteAll(userId);

        private static NotificationDto ToDto(Notification n) => new NotificationDto
        {
            Id        = n.Id,
            Message   = n.Message,
            Type      = n.Type,
            LinkUrl   = n.LinkUrl,
            IsRead    = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}
