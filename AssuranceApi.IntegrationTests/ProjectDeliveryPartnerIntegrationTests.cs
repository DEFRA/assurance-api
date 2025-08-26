using AssuranceApi.Data.Models;
using AssuranceApi.Project.Models;
using Elastic.CommonSchema;
using FluentAssertions;
using MongoDB.Bson;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AssuranceApi.IntegrationTests
{
    public class ProjectDeliveryPartnerIntegrationTests : IClassFixture<TestApplicationFactory>
    {
        private readonly TestApplicationFactory _factory;
        private static string _invalidProjectId = "68ae1bd0ca72c04e527689e9";

        public ProjectDeliveryPartnerIntegrationTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateProjectDeliveryPartner_ReturnsCreated_WhenValidRequest()
        {
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var project = await CreateProject(client);
            var deliveryPartner = await CreateDeliveryPartner(client);

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = project.Id;
            projectDeliveryPartner.DeliveryPartnerId = deliveryPartner.Id;

            var response = await client.PostAsJsonAsync($"/api/v1.0/projects/{project.Id}/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().NotBeNullOrWhiteSpace();

            var createdProjectDeliveryPartner = JsonSerializer.Deserialize<ProjectDeliveryPartnerModel>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            createdProjectDeliveryPartner.Should().NotBeNull();
            createdProjectDeliveryPartner!.ProjectId.Should().Be(project.Id);
            createdProjectDeliveryPartner!.DeliveryPartnerId.Should().Be(deliveryPartner.Id);
        }

        [Fact]
        public async Task CreateProjectDeliveryPartner_ReturnsBadRequest_WhenInvalidRequest()
        {
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();

            var response = await client.PostAsJsonAsync($"/api/v1.0/projects/INVALID/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateProjectDeliveryPartner_ReturnsNotFound_WhenInvalidProjectIdIsProvided()
        {
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var project = await CreateProject(client);

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = _invalidProjectId;
            projectDeliveryPartner.DeliveryPartnerId = "1";

            var response = await client.PostAsJsonAsync($"/api/v1.0/projects/{projectDeliveryPartner.ProjectId}/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateProjectDeliveryPartner_ReturnsNotFound_WhenInvalidDeliveryPartnerIdIsProvided()
        {
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var project = await CreateProject(client);

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = project.Id;
            projectDeliveryPartner.DeliveryPartnerId = "INVALID";

            var response = await client.PostAsJsonAsync($"/api/v1.0/projects/{project.Id}/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateProjectDeliveryPartner_ReturnsUnauthorized_WhenNoAuthTokenIsProvided()
        {
            var client = _factory.CreateUnauthenticatedClient();

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = _invalidProjectId;

            var response = await client.PostAsJsonAsync($"/api/v1.0/projects/{projectDeliveryPartner.ProjectId}/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetProjectDeliveryPartnersByProjectId_ReturnsListWithMatchingItem_WhenValidItemsAreUsed()
        {
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();

            var project = await CreateProject(authenticatedClient);
            var deliveryPartner = await CreateDeliveryPartner(authenticatedClient);

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = project.Id;
            projectDeliveryPartner.DeliveryPartnerId = deliveryPartner.Id;

            var response = await authenticatedClient.PostAsJsonAsync($"/api/v1.0/projects/{project.Id}/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().NotBeNullOrWhiteSpace();

            var createdProjectDeliveryPartner = JsonSerializer.Deserialize<ProjectDeliveryPartnerModel>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            createdProjectDeliveryPartner.Should().NotBeNull();
            createdProjectDeliveryPartner!.ProjectId.Should().Be(project.Id);
            createdProjectDeliveryPartner!.DeliveryPartnerId.Should().Be(deliveryPartner.Id);


            var unauthenticatedClient = _factory.CreateUnauthenticatedClient();
            response = await unauthenticatedClient.GetAsync($"/api/v1.0/projects/{project.Id}/deliverypartners");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().NotBeNullOrWhiteSpace();

            var retrievedProjectDeliveryPartners = JsonSerializer.Deserialize<List<ProjectDeliveryPartnerModel>>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            retrievedProjectDeliveryPartners.Count.Should().Be(1);
            retrievedProjectDeliveryPartners[0].Should().BeEquivalentTo(createdProjectDeliveryPartner);
        }

        [Fact]
        public async Task GetProjectDeliveryPartner_ReturnsMatchingItem_WhenValidItemsAreUsed()
        {
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();

            var project = await CreateProject(authenticatedClient);
            var deliveryPartner = await CreateDeliveryPartner(authenticatedClient);

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = project.Id;
            projectDeliveryPartner.DeliveryPartnerId = deliveryPartner.Id;

            var response = await authenticatedClient.PostAsJsonAsync($"/api/v1.0/projects/{project.Id}/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().NotBeNullOrWhiteSpace();

            var createdProjectDeliveryPartner = JsonSerializer.Deserialize<ProjectDeliveryPartnerModel>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            createdProjectDeliveryPartner.Should().NotBeNull();
            createdProjectDeliveryPartner!.ProjectId.Should().Be(project.Id);
            createdProjectDeliveryPartner!.DeliveryPartnerId.Should().Be(deliveryPartner.Id);


            var unauthenticatedClient = _factory.CreateUnauthenticatedClient();
            response = await unauthenticatedClient.GetAsync($"/api/v1.0/projects/{project.Id}/deliverypartners/{deliveryPartner.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().NotBeNullOrWhiteSpace();

            var retrievedProjectDeliveryPartner = JsonSerializer.Deserialize<ProjectDeliveryPartnerModel>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            retrievedProjectDeliveryPartner.Should().BeEquivalentTo(createdProjectDeliveryPartner);
        }

        [Fact]
        public async Task UpdateProjectDeliveryPartner_ReturnsMatchingItem_WhenValidItemsAreUsed()
        {
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();

            var project = await CreateProject(authenticatedClient);
            var deliveryPartner = await CreateDeliveryPartner(authenticatedClient);

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = project.Id;
            projectDeliveryPartner.DeliveryPartnerId = deliveryPartner.Id;

            var response = await authenticatedClient.PostAsJsonAsync($"/api/v1.0/projects/{project.Id}/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().NotBeNullOrWhiteSpace();

            var createdProjectDeliveryPartner = JsonSerializer.Deserialize<ProjectDeliveryPartnerModel>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            createdProjectDeliveryPartner.Should().NotBeNull();
            createdProjectDeliveryPartner!.ProjectId.Should().Be(project.Id);
            createdProjectDeliveryPartner!.DeliveryPartnerId.Should().Be(deliveryPartner.Id);


            // Update the created item
            createdProjectDeliveryPartner.EngagementManager = "Jane Smith";
            createdProjectDeliveryPartner.EngagementEnded = new DateTime(2024, 06, 30).ToUniversalTime();

            response = await authenticatedClient.PutAsJsonAsync($"/api/v1.0/projects/{project.Id}/deliverypartners/{deliveryPartner.Id}", createdProjectDeliveryPartner);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the updated item
            var unauthenticatedClient = _factory.CreateUnauthenticatedClient();
            response = await unauthenticatedClient.GetAsync($"/api/v1.0/projects/{project.Id}/deliverypartners/{deliveryPartner.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().NotBeNullOrWhiteSpace();

            var retrievedProjectDeliveryPartner = JsonSerializer.Deserialize<ProjectDeliveryPartnerModel>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            retrievedProjectDeliveryPartner.Should().BeEquivalentTo(createdProjectDeliveryPartner);
        }

        [Fact]
        public async Task UpdateProjectDeliveryPartner_ReturnsUnauthorized_WhenNoAuthTokenIsProvided()
        {
            var client = _factory.CreateUnauthenticatedClient();

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = _invalidProjectId;

            var response = await client.PutAsJsonAsync($"/api/v1.0/projects/{projectDeliveryPartner.ProjectId}/deliverypartners/INVALID", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteProjectDeliveryPartner_ReturnsNotFound_WhenValidItemsAreUsed()
        {
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();

            var project = await CreateProject(authenticatedClient);
            var deliveryPartner = await CreateDeliveryPartner(authenticatedClient);

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = project.Id;
            projectDeliveryPartner.DeliveryPartnerId = deliveryPartner.Id;

            var response = await authenticatedClient.PostAsJsonAsync($"/api/v1.0/projects/{project.Id}/deliverypartners", projectDeliveryPartner);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().NotBeNullOrWhiteSpace();

            var createdProjectDeliveryPartner = JsonSerializer.Deserialize<ProjectDeliveryPartnerModel>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            createdProjectDeliveryPartner.Should().NotBeNull();
            createdProjectDeliveryPartner!.ProjectId.Should().Be(project.Id);
            createdProjectDeliveryPartner!.DeliveryPartnerId.Should().Be(deliveryPartner.Id);

            // Verify the created item
            var unauthenticatedClient = _factory.CreateUnauthenticatedClient();
            response = await unauthenticatedClient.GetAsync($"/api/v1.0/projects/{project.Id}/deliverypartners/{deliveryPartner.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Delete the created item
            response = await authenticatedClient.DeleteAsync($"/api/v1.0/projects/{project.Id}/deliverypartners/{deliveryPartner.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the deleted item
            response = await unauthenticatedClient.GetAsync($"/api/v1.0/projects/{project.Id}/deliverypartners/{deliveryPartner.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteProjectDeliveryPartner_ReturnsUnauthorized_WhenNoAuthTokenIsProvided()
        {
            var client = _factory.CreateUnauthenticatedClient();

            var projectDeliveryPartner = CreateProjectDeliveryPartnerModel();
            projectDeliveryPartner.ProjectId = _invalidProjectId;

            var response = await client.DeleteAsync($"/api/v1.0/projects/{projectDeliveryPartner.ProjectId}/deliverypartners/INVALID");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task<ProjectModel> CreateProject(HttpClient client)
        {
            var project = new ProjectModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Test Project",
                Commentary = "A test project for integration testing",
                Status = "GREEN",
                Phase = "Discovery",
                Tags = new List<string> { "test", "integration" },
                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1.0/projects", project);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return project;
        }

        private static async Task<DeliveryPartnerModel> CreateDeliveryPartner(HttpClient client)
        {
            var deliveryPartner = new DeliveryPartnerModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Test Delivery Partner",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1.0/deliverypartners", deliveryPartner);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            return deliveryPartner;
        }

        private static ProjectDeliveryPartnerModel CreateProjectDeliveryPartnerModel()
        {
            return new ProjectDeliveryPartnerModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ProjectId = string.Empty,
                DeliveryPartnerId = string.Empty,
                EngagementManager = "John Doe",
                EngagementStarted = new DateTime(2024, 04, 21).ToUniversalTime(),
                EngagementEnded = DateTime.MinValue
            };
        }
    }
}
