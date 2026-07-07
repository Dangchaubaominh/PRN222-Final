using System;

namespace RagChatbot.DAL.Entities
{
    public enum FeedbackType
    {
        Upvote = 1,
        Downvote = -1,
        WrongSource = -2
    }
}
