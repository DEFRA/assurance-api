using AssuranceApi.Data.Models;
using FluentAssertions;
using MongoDB.Bson;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssuranceApi.IntegrationTests
{
    public class DeliveryGroupIntegrationTests : IClassFixture<TestApplicationFactory>
    {
        private readonly TestApplicationFactory _factory;

        public DeliveryGroupIntegrationTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateDeliveryGroup_ReturnsCreated_WhenValidDeliveryGroupProvided()
        {
            // Arrange - Clear database and use authenticated client
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

            var deliveryGroup = new DeliveryGroupModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Test Delivery Group",
                IsActive = true,
                Status = "Active",
                Lead = "Test Lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1.0/deliverygroups", deliveryGroup);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task CreateDeliveryGroup_RequiresAuthentication()
        {
            // Arrange - Clear database and use unauthenticated client
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient();

            var deliveryGroup = new DeliveryGroupModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Test Delivery Group",
                IsActive = true,
                Status = "Active",
                Lead = "Test Lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1.0/deliverygroups", deliveryGroup);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDeliveryGroups_ReturnsEmptyList_WhenNoDeliveryGroupsExist()
        {
            // Arrange - Clear database for clean test
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

            // Act
            var response = await client.GetAsync("/api/v1.0/deliverygroups");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var deliveryGroups = JsonSerializer.Deserialize<List<DeliveryGroupModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            deliveryGroups.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDeliveryGroupById_ReturnsNotFound_WhenDeliveryGroupDoesNotExist()
        {
            // Arrange - Clear database
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

            // Act - Use a valid ObjectId format
            var nonExistentId = ObjectId.GenerateNewId().ToString();
            var response = await client.GetAsync($"/api/v1.0/deliverygroups/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetDeliveryGroupById_ReturnsDeliveryGroup_WhenDeliveryGroupExists()
        {
            // Arrange - Clear database and create deliveryGroup
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var deliveryGroupId = ObjectId.GenerateNewId().ToString();
            var deliveryGroup = new DeliveryGroupModel
            {
                Id = deliveryGroupId,
                Name = "Test Delivery Group",
                IsActive = true,
                Status = "Active",
                Lead = "Test Lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Create the deliveryGroup first
            await authenticatedClient.PostAsJsonAsync("/api/v1.0/deliverygroups", deliveryGroup);

            // Act - Use public client to test read access
            var response = await publicClient.GetAsync($"/api/v1.0/deliverygroups/{deliveryGroupId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var returnedDeliveryGroup = JsonSerializer.Deserialize<DeliveryGroupModel>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            returnedDeliveryGroup.Should().NotBeNull();
            returnedDeliveryGroup!.Id.Should().Be(deliveryGroupId);
            returnedDeliveryGroup.Name.Should().Be("Test Delivery Group");
            returnedDeliveryGroup.Status.Should().Be("Active");
            returnedDeliveryGroup.Lead.Should().Be("Test Lead");
        }

        [Fact]
        public async Task GetDeliveryGroups_ReturnsDeliveryGroups_WhenDeliveryGroupsExist()
        {
            // Arrange - Clear database and create delivery groups
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var numberOfDeliveryGroups = 2;
            var deliveryGroups = GenerateNumberOfDeliveryGroups(numberOfDeliveryGroups);

            foreach (var deliveryGroup in deliveryGroups.Values)
            {
                await authenticatedClient.PostAsJsonAsync("/api/v1.0/deliverygroups", deliveryGroup);
            }

            // Act - Use public client to test read access
            var response = await publicClient.GetAsync("/api/v1.0/deliverygroups");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var retrievedDeliveryGroups = JsonSerializer.Deserialize<List<DeliveryGroupModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            retrievedDeliveryGroups.Should().NotBeNull();
            retrievedDeliveryGroups!.Should().HaveCount(numberOfDeliveryGroups);

            for (var i = 0; i < numberOfDeliveryGroups; i++)
            {
                retrievedDeliveryGroups![i].Id.Should().Be(deliveryGroups.Values.ElementAt(i).Id);
            }
        }

        [Fact]
        public async Task UpdateDeliveryGroup_ReturnsOk_WhenValidUpdateProvided()
        {
            // Arrange - Clear database and create delivery group
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

            var deliveryGroupId = ObjectId.GenerateNewId().ToString();
            var originalDeliveryGroup = new DeliveryGroupModel
            {
                Id = deliveryGroupId,
                Name = "Original Delivery Group Name",
                IsActive = true,
                Status = "Active",
                Lead = "Original Lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Create the delivery group first
            await client.PostAsJsonAsync("/api/v1.0/deliverygroups", originalDeliveryGroup);

            var updatedDeliveryGroup = new DeliveryGroupModel
            {
                Id = deliveryGroupId,
                Name = "Updated Delivery Group Name",
                IsActive = true,
                Status = "Inactive",
                Lead = "Updated Lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1.0/deliverygroups/{deliveryGroupId}", updatedDeliveryGroup);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the update by fetching the delivery group
            var getResponse = await client.GetAsync($"/api/v1.0/deliverygroups/{deliveryGroupId}");
            var content = await getResponse.Content.ReadAsStringAsync();
            var fetchedDeliveryGroup = JsonSerializer.Deserialize<DeliveryGroupModel>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            fetchedDeliveryGroup!.Name.Should().Be("Updated Delivery Group Name");
            fetchedDeliveryGroup.Status.Should().Be("Inactive");
            fetchedDeliveryGroup.Lead.Should().Be("Updated Lead");
        }

        [Fact]
        public async Task UpdateDeliveryGroup_RequiresAuthentication()
        {
            // Arrange - Clear database and create delivery group
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient(); // Protected endpoint

            var deliveryGroupId = ObjectId.GenerateNewId().ToString();
            var updatedDeliveryGroup = new DeliveryGroupModel
            {
                Id = deliveryGroupId,
                Name = "Updated Delivery Group Name",
                IsActive = true,
                Status = "Active",
                Lead = "Updated Lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1.0/deliverygroups/{deliveryGroupId}", updatedDeliveryGroup);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteDeliveryGroup_ReturnsOk_WhenDeliveryGroupExists()
        {
            // Arrange - Clear database and create delivery group
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

            var deliveryGroupId = ObjectId.GenerateNewId().ToString();
            var deliveryGroup = new DeliveryGroupModel
            {
                Id = deliveryGroupId,
                Name = "Test Delivery Group",
                IsActive = true,
                Status = "Active",
                Lead = "Test Lead",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Create the delivery group first
            await client.PostAsJsonAsync("/api/v1.0/deliverygroups", deliveryGroup);

            // Act
            var response = await client.DeleteAsync($"/api/v1.0/deliverygroups/{deliveryGroupId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteDeliveryGroup_RequiresAuthentication()
        {
            // Arrange - Clear database and create delivery group
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient(); // Protected endpoint

            var deliveryGroupId = ObjectId.GenerateNewId().ToString();

            // Act
            var response = await client.DeleteAsync($"/api/v1.0/deliverygroups/{deliveryGroupId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static Dictionary<string, DeliveryGroupModel> GenerateNumberOfDeliveryGroups(int numberOfDeliveryGroups)
        {
            var deliveryGroups = new Dictionary<string, DeliveryGroupModel>(numberOfDeliveryGroups);

            for (var groupNumber = 0; groupNumber < numberOfDeliveryGroups; groupNumber++)
            {
                var groupId = ObjectId.GenerateNewId().ToString();

                var deliveryGroup = new DeliveryGroupModel
                {
                    Id = groupId,
                    Name = $"Delivery Group {groupNumber + 1}",
                    IsActive = true,
                    Status = "Active",
                    Lead = $"Lead {groupNumber + 1}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };

                deliveryGroups.Add(groupId, deliveryGroup);
            }

            return deliveryGroups;
        }
    }
}
