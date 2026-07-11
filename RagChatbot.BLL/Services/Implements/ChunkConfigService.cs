using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.DAL.Entities;
using RagChatbot.DAL.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Implements
{
    public class ChunkConfigService : IChunkConfigService
    {
        private readonly IChunkConfigRepository _repository;

        public ChunkConfigService(IChunkConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<SubjectChunkConfig> GetForSubjectAsync(Guid subjectId)
        {
            var config = await _repository.GetBySubjectIdAsync(subjectId);
            if (config == null)
            {
                return new SubjectChunkConfig
                {
                    SubjectId = subjectId,
                    MaxWordsPerChunk = 400,
                    OverlapSentences = 2,
                    Strategy = "Semantic"
                };
            }
            return config;
        }

        public async Task SaveAsync(SubjectChunkConfig config)
        {
            var existing = await _repository.GetBySubjectIdAsync(config.SubjectId);
            if (existing == null)
            {
                await _repository.AddAsync(config);
            }
            else
            {
                existing.MaxWordsPerChunk = config.MaxWordsPerChunk;
                existing.OverlapSentences = config.OverlapSentences;
                existing.Strategy = config.Strategy;
                await _repository.UpdateAsync(existing);
            }
        }
    }
}
