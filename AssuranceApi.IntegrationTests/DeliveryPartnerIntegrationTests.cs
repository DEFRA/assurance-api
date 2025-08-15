using AssuranceApi.Data.Models;
using AssuranceApi.Project.Models;
using FluentAssertions;
using MongoDB.Bson;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssuranceApi.IntegrationTests
{
    public class DeliveryPartnerIntegrationTests : IClassFixture<TestApplicationFactory>
    {
        private readonly TestApplicationFactory _factory;

        public DeliveryPartnerIntegrationTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateDeliveryPartner_ReturnsCreated_WhenValidDeliveryPartnerProvided()
        {
            // Arrange - Clear database and use authenticated client
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

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
        }

        [Fact]
        public async Task CreateDeliveryPartner_RequiresAuthentication()
        {
            // Arrange - Clear database and use unauthenticated client
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient();

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
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDeliveryPartners_ReturnsEmptyList_WhenNoDeliveryPartnersExist()
        {
            // Arrange - Clear database for clean test
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

            // Act
            var response = await client.GetAsync("/api/v1.0/deliverypartners");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var projects = JsonSerializer.Deserialize<List<ProjectModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            projects.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDeliveryPartnerById_ReturnsNotFound_WhenDeliveryPartnerDoesNotExist()
        {
            // Arrange - Clear database
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

            // Act - Use a valid ObjectId format
            var nonExistentId = ObjectId.GenerateNewId().ToString();
            var response = await client.GetAsync($"/api/v1.0/deliverypartners/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetDeliveryPartnerById_ReturnsDeliveryPartner_WhenDelieryPartnerExists()
        {
            // Arrange - Clear database and create deliveryPartner
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var deliveryPartnerId = ObjectId.GenerateNewId().ToString();
            var deliveryPartner = new DeliveryPartnerModel
            {
                Id = deliveryPartnerId,
                Name = "Test Delivery Partner",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Create the deliveryPartner first
            await authenticatedClient.PostAsJsonAsync("/api/v1.0/deliverypartners", deliveryPartner);

            // Act - Use public client to test read access
            var response = await publicClient.GetAsync($"/api/v1.0/deliverypartners/{deliveryPartnerId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var returnedProject = JsonSerializer.Deserialize<ProjectModel>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            returnedProject.Should().NotBeNull();
            returnedProject!.Id.Should().Be(deliveryPartnerId);
            returnedProject.Name.Should().Be("Test Delivery Partner");
        }

        [Fact]
        public async Task GetDeliveryPartners_ReturnsDeliveryPartners_WhenDeliveryPartnersExist()
        {
            // Arrange - Clear database and create projects
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var numberOfDeliveryPartners = 2;
            var deliveryPartners = GenerateNumberOfDeliveryPartners(numberOfDeliveryPartners);

            foreach (var deliveryPartner in deliveryPartners.Values)
            {
                await authenticatedClient.PostAsJsonAsync("/api/v1.0/deliverypartners", deliveryPartner);
            }

            // Act - Use public client to test read access
            var response = await publicClient.GetAsync("/api/v1.0/deliverypartners");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var retrievedDeliveryPartners = JsonSerializer.Deserialize<List<ProjectModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            retrievedDeliveryPartners.Should().NotBeNull();
            retrievedDeliveryPartners!.Should().HaveCount(numberOfDeliveryPartners);

            for (var i = 0; i < numberOfDeliveryPartners; i++)
            {
                retrievedDeliveryPartners![i].Id.Should().Be(deliveryPartners.Values.ElementAt(i).Id);
            }
        }

        [Fact]
        public async Task UpdateDeliveryPartner_ReturnsOk_WhenValidUpdateProvided()
        {
            // Arrange - Clear database and create project
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

            var deliveryPartnerId = ObjectId.GenerateNewId().ToString();
            var originalDeliveryPartner = new DeliveryPartnerModel
            {
                Id = deliveryPartnerId,
                Name = "Original Delivery Partner Name",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Create the project first
            await client.PostAsJsonAsync("/api/v1.0/deliverypartners", originalDeliveryPartner);

            var updatedDeliveryPartner = new DeliveryPartnerModel
            {
                Id = deliveryPartnerId,
                Name = "Updated Delivery Partner Name",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1.0/deliverypartners/{deliveryPartnerId}", updatedDeliveryPartner);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the update by fetching the project
            var getResponse = await client.GetAsync($"/api/v1.0/deliverypartners/{deliveryPartnerId}");
            var content = await getResponse.Content.ReadAsStringAsync();
            var fetchedDeliveryPartner = JsonSerializer.Deserialize<ProjectModel>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            fetchedDeliveryPartner!.Name.Should().Be("Updated Delivery Partner Name");
        }

        [Fact]
        public async Task UpdateDeliveryPartner_RequiresAuthentication()
        {
            // Arrange - Clear database and create project
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient(); // Protected endpoint

            var deliveryPartnerId = ObjectId.GenerateNewId().ToString();
            var updatedDeliveryPartner = new DeliveryPartnerModel
            {
                Id = deliveryPartnerId,
                Name = "Updated Delivery Partner Name",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1.0/deliverypartners/{deliveryPartnerId}", updatedDeliveryPartner);

            // Assert
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteDeliveryPartner_ReturnsOk_WhenDeliveryPartnerExists()
        {
            // Arrange - Clear database and create project
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

            var deliveryPartnerId = ObjectId.GenerateNewId().ToString();
            var deliveryPartner = new DeliveryPartnerModel
            {
                Id = deliveryPartnerId,
                Name = "Test Delivery Partner",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Create the project first
            await client.PostAsJsonAsync("/api/v1.0/deliverypartners", deliveryPartner);

            // Act
            var response = await client.DeleteAsync($"/api/v1.0/deliverypartners/{deliveryPartnerId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteDeliveryPartner_RequiresAuthentication()
        {
            // Arrange - Clear database and create project
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient(); // Protected endpoint

            var deliveryPartnerId = ObjectId.GenerateNewId().ToString();

            // Act
            var response = await client.DeleteAsync($"/api/v1.0/deliverypartners/{deliveryPartnerId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static Dictionary<string, DeliveryPartnerModel> GenerateNumberOfDeliveryPartners(int numberOfDeliveryPartners)
        {
            var deliveryPartners = new Dictionary<string, DeliveryPartnerModel>(numberOfDeliveryPartners);

            for (var partnerNumber = 0; partnerNumber < numberOfDeliveryPartners; partnerNumber++)
            {
                var partnerId = ObjectId.GenerateNewId().ToString();

                var deliveryPartner = new DeliveryPartnerModel
                {
                    Id = partnerId,
                    Name = $"Delivery Partner {partnerNumber + 1}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                deliveryPartners.Add(partnerId, deliveryPartner);
            }

            return deliveryPartners;
        }
    }
}
