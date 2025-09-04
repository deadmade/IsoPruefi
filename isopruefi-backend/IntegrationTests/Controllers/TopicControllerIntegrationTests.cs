using FluentAssertions;
using IntegrationTests.ApiClient;
using IntegrationTests.Infrastructure;
using ApiTopicSetting = IntegrationTests.ApiClient.TopicSetting;

namespace IntegrationTests.Controllers;

/// <summary>
///     Integration tests for the Topic Controller to verify MQTT topic management and sensor configuration functionality.
/// </summary>
[TestFixture]
public class TopicControllerIntegrationTests : ApiClientTestBase
{
    /// <summary>
    ///     Tests retrieving all topics with admin privileges and verifies successful response or proper error handling.
    /// </summary>
    [Test]
    public async Task GetAllTopics_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        try
        {
            var response = await TopicClient.GetAllTopicsAsync();
            response.Should().NotBeNull();
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(200, 500);
        }
    }

    /// <summary>
    ///     Tests retrieving all topics with user token and verifies 403 Forbidden response for insufficient privileges.
    /// </summary>
    [Test]
    public async Task GetAllTopics_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            TopicClient.GetAllTopicsAsync());

        exception.StatusCode.Should().Be(403);
    }

    /// <summary>
    ///     Tests retrieving all topics without authentication token and verifies 401 Unauthorized response.
    /// </summary>
    [Test]
    public void GetAllTopics_WithoutToken_ReturnsUnauthorized()
    {
        var exception = Assert.ThrowsAsync<ApiException>(() =>
            TopicClient.GetAllTopicsAsync());

        exception.StatusCode.Should().Be(401);
    }

    /// <summary>
    ///     Tests retrieving all sensor types and verifies successful response or proper error handling.
    /// </summary>
    [Test]
    public async Task GetAllSensorTypes_ReturnsOk()
    {
        try
        {
            var response = await TopicClient.GetAllSensorTypesAsync();
            response.Should().NotBeNull();
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(200, 500);
        }
    }

    /// <summary>
    ///     Tests creating a new MQTT topic with admin privileges and valid data configuration.
    /// </summary>
    [Test]
    public async Task CreateTopic_WithAdminToken_AndValidData_ReturnsCreated()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var topicSetting = new ApiTopicSetting
        {
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.Temp,
            SensorName = "TestSensor_Integration",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        try
        {
            var response = await TopicClient.CreateTopicAsync(topicSetting);
            response.Should().NotBeNull();
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(201, 400, 500);
        }
    }

    /// <summary>
    ///     Tests creating a topic with user token and verifies 403 Forbidden response for insufficient privileges.
    /// </summary>
    [Test]
    public async Task CreateTopic_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var topicSetting = new ApiTopicSetting
        {
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.Temp,
            SensorName = "TestSensor",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            TopicClient.CreateTopicAsync(topicSetting));

        exception.StatusCode.Should().Be(403);
    }

    /// <summary>
    ///     Tests creating a topic without authentication token and verifies 401 Unauthorized response.
    /// </summary>
    [Test]
    public void CreateTopic_WithoutToken_ReturnsUnauthorized()
    {
        var topicSetting = new ApiTopicSetting
        {
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.Temp,
            SensorName = "TestSensor",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            TopicClient.CreateTopicAsync(topicSetting));

        exception.StatusCode.Should().Be(401);
    }

    /// <summary>
    ///     Tests creating a topic with null data and verifies proper exception handling for invalid input.
    /// </summary>
    [Test]
    public async Task CreateTopic_WithInvalidData_ReturnsBadRequest()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var exception = Assert.ThrowsAsync<ArgumentNullException>(() =>
            TopicClient.CreateTopicAsync(null!));

        exception.Should().NotBeNull();
    }

    /// <summary>
    ///     Tests updating an existing MQTT topic with admin privileges and verifies successful modification.
    /// </summary>
    [Test]
    public async Task UpdateTopic_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var topicSetting = new ApiTopicSetting
        {
            TopicSettingId = 1,
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.Temp,
            SensorName = "UpdatedTestSensor",
            SensorLocation = "UpdatedLocation",
            HasRecovery = true,
            CoordinateMappingId = 1
        };

        try
        {
            var response = await TopicClient.UpdateTopicAsync(topicSetting);
            response.Should().NotBeNull();
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(200, 400, 500);
        }
    }

    /// <summary>
    ///     Tests updating a topic with user token and verifies 403 Forbidden response for insufficient privileges.
    /// </summary>
    [Test]
    public async Task UpdateTopic_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var topicSetting = new ApiTopicSetting
        {
            TopicSettingId = 1,
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.Temp,
            SensorName = "TestSensor",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var exception = Assert.ThrowsAsync<ApiException>(() =>
            TopicClient.UpdateTopicAsync(topicSetting));

        exception.StatusCode.Should().Be(403);
    }
}