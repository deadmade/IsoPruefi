using Database.EntityFramework;
using Database.EntityFramework.Models;
using Database.Repository.SettingsRepo;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Repositories;

[TestFixture]
public class SettingsRepoTests
{
    private ApplicationDbContext _context;
    private SettingsRepo _settingsRepo;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .Options;

        _context = new ApplicationDbContext(options);
        _settingsRepo = new SettingsRepo(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task AddTopicSettingAsync_WithNullTopicSetting_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Func<Task> act = async () => await _settingsRepo.AddTopicSettingAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public void Constructor_WithValidContext_ShouldCreateInstance()
    {
        // Act
        Action act = () => new SettingsRepo(_context);

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new SettingsRepo(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}