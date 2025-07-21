using Database.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.EntityFramework;

public class SettingsContext : DbContext
{
   public DbSet<GeneralSetting> GeneralSettings { get; set; } = null!;
   public DbSet<TopicSetting> TopicSettings { get; set; } = null!;
   
   //TODO: Set connection from environment variable or configuration file
   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      => optionsBuilder.UseNpgsql(@"Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase");
}