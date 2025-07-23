using Database.EntityFramework.Models;

namespace Database.Repository.SettingsRepository;

public interface ISettingsRepo
{
    Task<List<TopicSetting>> GetTopicSettingsAsync();
}