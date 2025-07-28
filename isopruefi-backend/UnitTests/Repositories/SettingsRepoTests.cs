using Database.EntityFramework;
using Database.EntityFramework.Models;
using Database.Repository.SettingsRepo;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Repositories;

/// <summary>
/// Unit tests for the SettingsRepo class, verifying settings repository operations and database interactions.
/// </summary>
[TestFixture]
public class SettingsRepoTests
{
    private ApplicationDbContext _context;
    private SettingsRepo _settingsRepo;

    /// <summary>
    /// Sets up test fixtures and initializes database context before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .Options;

        _context = new ApplicationDbContext(options);
        _settingsRepo = new SettingsRepo(_context);
    }

    /// <summary>
    /// Cleans up resources and disposes database context after each test execution.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    /// <summary>
    /// Tests that AddTopicSettingAsync throws ArgumentNullException when topic setting parameter is null.
    /// </summary>
    [Test]
    public async Task AddTopicSettingAsync_WithNullTopicSetting_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Func<Task> act = async () => await _settingsRepo.AddTopicSettingAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the constructor creates a valid instance when provided with valid database context.
    /// </summary>
    [Test]
    public void Constructor_WithValidContext_ShouldCreateInstance()
    {
        // Act
        Action act = () => new SettingsRepo(_context);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when database context parameter is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new SettingsRepo(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}