using RagChatbot.BLL.DTOs;
using System;
using System.Collections.Generic;

namespace RagChatbot.BLL.Services.Interfaces
{
    public enum AssignResult { Success, TeacherLimitReached }

    public interface IUserSubjectService
    {
        IEnumerable<SubjectDto> GetAssignedSubjects(int userId);
        IEnumerable<UserManageDto> GetAssignedUsers(Guid subjectId);
        IEnumerable<UserManageDto> GetAddableUsers(Guid subjectId, string requesterRole);
        AssignResult Assign(int userId, Guid subjectId);
        void Remove(int userId, Guid subjectId);
        int CountTeachersInSubject(Guid subjectId);

        // Người dùng có thuộc môn học này không (dùng để kiểm tra quyền truy cập)
        bool IsAssigned(int userId, Guid subjectId);
    }
}
