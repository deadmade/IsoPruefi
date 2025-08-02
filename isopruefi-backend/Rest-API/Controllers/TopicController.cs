using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Database.Repository.SettingsRepo;
using Database.EntityFramework.Models;
using Microsoft.AspNetCore.Authorization;

namespace Rest_API.Controllers;

/// <summary>
/// Controller for managing MQTT topic settings
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
[Produces("application/json")]
[Consumes("application/json")]
public class TopicController : ControllerBase
{
    private readonly ISettingsRepo _settingsRepo;

    /// <summary>
    /// Initializes a new instance of the TopicController
    /// </summary>
    /// <param name="settingsRepo">Settings repository for topic operations</param>
    public TopicController(ISettingsRepo settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }

    /// <summary>
    /// Gets all available topic settings
    /// </summary>
    /// <returns>A list of all topic settings</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<TopicSetting>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize]
    public async Task<ActionResult<List<TopicSetting>>> GetAllTopics()
    {
        try
        {
            var topics = await _settingsRepo.GetTopicSettingsAsync();
            return Ok(topics);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving topics", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new topic setting
    /// </summary>
    /// <param name="topicSetting">The topic setting to create</param>
    /// <returns>The ID of the created topic setting</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize] 
    public async Task<ActionResult> CreateTopic([FromBody] TopicSetting topicSetting)
    {
        try
        {
            if (topicSetting == null) return BadRequest(new { message = "Topic setting is required" });

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var topicId = await _settingsRepo.AddTopicSettingAsync(topicSetting);

            return CreatedAtAction(
                nameof(GetAllTopics),
                new { id = topicId },
                new { id = topicId, message = "Topic created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the topic", error = ex.Message });
        }
    }
}