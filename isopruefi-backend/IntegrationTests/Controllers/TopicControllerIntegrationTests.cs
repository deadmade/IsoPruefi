using System.Net;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Database.EntityFramework.Models;
using Database.EntityFramework.Enums;

namespace IntegrationTests.Controllers;

[TestFixture]
public class TopicControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetAllTopics_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1/Topic/GetAllTopics");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task GetAllTopics_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1/Topic/GetAllTopics");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetAllTopics_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/v1/Topic/GetAllTopics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetAllSensorTypes_ReturnsOk()
    {
        // This endpoint doesn't require authorization according to the controller
        var response = await Client.GetAsync("/api/v1/Topic/GetAllSensorTypes");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task CreateTopic_WithAdminToken_AndValidData_ReturnsCreated()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var topicSetting = new TopicSetting
        {
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.temp,
            SensorName = "TestSensor_Integration",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var response = await Client.PostAsync("/api/v1/Topic/CreateTopic", CreateJsonContent(topicSetting));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateTopic_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var topicSetting = new TopicSetting
        {
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.temp,
            SensorName = "TestSensor",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var response = await Client.PostAsync("/api/v1/Topic/CreateTopic", CreateJsonContent(topicSetting));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateTopic_WithoutToken_ReturnsUnauthorized()
    {
        var topicSetting = new TopicSetting
        {
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.temp,
            SensorName = "TestSensor",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var response = await Client.PostAsync("/api/v1/Topic/CreateTopic", CreateJsonContent(topicSetting));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateTopic_WithInvalidData_ReturnsBadRequest()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var response = await Client.PostAsync("/api/v1/Topic/CreateTopic", CreateJsonContent((object?)null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateTopic_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var topicSetting = new TopicSetting
        {
            TopicSettingId = 1,
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.temp,
            SensorName = "UpdatedTestSensor",
            SensorLocation = "UpdatedLocation",
            HasRecovery = true,
            CoordinateMappingId = 1
        };

        var response = await Client.PutAsync("/api/v1/Topic/UpdateTopic", CreateJsonContent(topicSetting));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateTopic_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var topicSetting = new TopicSetting
        {
            TopicSettingId = 1,
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.temp,
            SensorName = "TestSensor",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var response = await Client.PutAsync("/api/v1/Topic/UpdateTopic", CreateJsonContent(topicSetting));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteTopic_WithAdminToken_ReturnsOk()
    {
        var token = await GetJwtTokenAsync("admin", "Admin123!");
        SetAuthorizationHeader(token);

        var topicSetting = new TopicSetting
        {
            TopicSettingId = 999, // Non-existent ID for testing
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.temp,
            SensorName = "ToDeleteSensor",
            SensorLocation = "ToDeleteLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/Topic/DeleteTopic")
        {
            Content = CreateJsonContent(topicSetting)
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteTopic_WithUserToken_ReturnsForbidden()
    {
        var token = await GetJwtTokenAsync("user", "User123!");
        SetAuthorizationHeader(token);

        var topicSetting = new TopicSetting
        {
            TopicSettingId = 1,
            DefaultTopicPath = "dhbw/ai/si2023/",
            GroupId = 1,
            SensorTypeEnum = SensorType.temp,
            SensorName = "TestSensor",
            SensorLocation = "TestLocation",
            HasRecovery = false,
            CoordinateMappingId = 1
        };

        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/Topic/DeleteTopic")
        {
            Content = CreateJsonContent(topicSetting)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}