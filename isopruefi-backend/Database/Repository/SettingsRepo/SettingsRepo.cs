using Database.EntityFramework;
using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Repository.SettingsRepository;

public class SettingsRepo : ISettingsRepo
{
    private SettingsContext _settingsContext;
    
    public SettingsRepo(SettingsContext settingsContext)
    {
        _settingsContext = settingsContext;
    }
    
    public Task<List<TopicSetting>> GetTopicSettingsAsync()
    {
        return _settingsContext.TopicSettings.ToListAsync();
    }
}