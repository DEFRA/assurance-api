using AssuranceApi.Data.Models;
using FluentAssertions;
using MongoDB.Bson;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AssuranceApi.IntegrationTests
{
    public class ThemeIntegrationTests : IClassFixture<TestApplicationFactory>
    {
        private readonly TestApplicationFactory _factory;

        public ThemeIntegrationTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateTheme_ReturnsCreated_WhenValidThemeProvided()
        {
            // Arrange - Clear database and use authenticated client
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var theme = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Test Theme",
                Description = "A test theme for integration testing",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1.0/themes", theme);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task CreateTheme_RequiresAuthentication()
        {
            // Arrange - Clear database and use unauthenticated client
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient();

            var theme = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Test Theme",
                Description = "A test theme",
                ProjectIds = new List<string>(),
                IsActive = true
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1.0/themes", theme);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateTheme_ReturnsBadRequest_WhenNameIsEmpty()
        {
            // Arrange
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var theme = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "", // Invalid - empty name
                Description = "A test theme",
                ProjectIds = new List<string>(),
                IsActive = true
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1.0/themes", theme);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetThemes_ReturnsEmptyList_WhenNoThemesExist()
        {
            // Arrange - Clear database for clean test
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient();

            // Act
            var response = await client.GetAsync("/api/v1.0/themes");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var themes = JsonSerializer.Deserialize<List<ThemeModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            themes.Should().BeEmpty();
        }

        [Fact]
        public async Task GetThemeById_ReturnsNotFound_WhenThemeDoesNotExist()
        {
            // Arrange - Clear database
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateUnauthenticatedClient();

            // Act - Use a valid ObjectId format
            var nonExistentId = ObjectId.GenerateNewId().ToString();
            var response = await client.GetAsync($"/api/v1.0/themes/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetThemeById_ReturnsTheme_WhenThemeExists()
        {
            // Arrange - Clear database and create theme
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var themeId = ObjectId.GenerateNewId().ToString();
            var theme = new ThemeModel
            {
                Id = themeId,
                Name = "Test Theme",
                Description = "A test theme for integration testing",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Create the theme first
            var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", theme);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Act - Use public client to test read access
            var response = await publicClient.GetAsync($"/api/v1.0/themes/{themeId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var returnedTheme = JsonSerializer.Deserialize<ThemeModel>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            returnedTheme.Should().NotBeNull();
            returnedTheme!.Id.Should().Be(themeId);
            returnedTheme.Name.Should().Be("Test Theme");
            returnedTheme.Description.Should().Be("A test theme for integration testing");
        }

        [Fact]
        public async Task GetThemes_ReturnsThemes_WhenThemesExist()
        {
            // Arrange - Clear database and create themes
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var theme1 = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Theme Alpha",
                Description = "First test theme",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var theme2 = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Theme Beta",
                Description = "Second test theme",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", theme1);
            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", theme2);

            // Act
            var response = await publicClient.GetAsync("/api/v1.0/themes");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var themes = JsonSerializer.Deserialize<List<ThemeModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            themes.Should().NotBeNull();
            themes!.Count.Should().Be(2);
        }

        [Fact]
        public async Task GetThemes_ExcludesArchivedThemes_ByDefault()
        {
            // Arrange - Clear database and create themes
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var themeId = ObjectId.GenerateNewId().ToString();
            var activeTheme = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Active Theme",
                Description = "An active theme",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var archivedTheme = new ThemeModel
            {
                Id = themeId,
                Name = "Archived Theme",
                Description = "An archived theme",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", activeTheme);
            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", archivedTheme);

            // Archive the second theme
            await authenticatedClient.PutAsync($"/api/v1.0/themes/{themeId}/archive", null);

            // Act - Get themes without includeArchived flag
            var response = await publicClient.GetAsync("/api/v1.0/themes");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var themes = JsonSerializer.Deserialize<List<ThemeModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            themes.Should().NotBeNull();
            themes!.Count.Should().Be(1);
            themes[0].Name.Should().Be("Active Theme");
        }

        [Fact]
        public async Task GetThemes_IncludesArchivedThemes_WhenFlagSet()
        {
            // Arrange - Clear database and create themes
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var themeId = ObjectId.GenerateNewId().ToString();
            var activeTheme = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Active Theme",
                Description = "An active theme",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var archivedTheme = new ThemeModel
            {
                Id = themeId,
                Name = "Archived Theme",
                Description = "An archived theme",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", activeTheme);
            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", archivedTheme);

            // Archive the second theme
            await authenticatedClient.PutAsync($"/api/v1.0/themes/{themeId}/archive", null);

            // Act - Get themes with includeArchived flag
            var response = await publicClient.GetAsync("/api/v1.0/themes?includeArchived=true");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var themes = JsonSerializer.Deserialize<List<ThemeModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            themes.Should().NotBeNull();
            themes!.Count.Should().Be(2);
        }

        [Fact]
        public async Task UpdateTheme_ReturnsOk_WhenValidUpdate()
        {
            // Arrange - Clear database and create theme
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var themeId = ObjectId.GenerateNewId().ToString();
            var theme = new ThemeModel
            {
                Id = themeId,
                Name = "Original Theme",
                Description = "Original description",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await client.PostAsJsonAsync("/api/v1.0/themes", theme);

            // Update the theme
            var updatedTheme = new ThemeModel
            {
                Id = themeId,
                Name = "Updated Theme",
                Description = "Updated description",
                ProjectIds = new List<string> { "project-1", "project-2" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1.0/themes/{themeId}", updatedTheme);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the update
            var getResponse = await client.GetAsync($"/api/v1.0/themes/{themeId}");
            var content = await getResponse.Content.ReadAsStringAsync();
            var returnedTheme = JsonSerializer.Deserialize<ThemeModel>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            returnedTheme!.Name.Should().Be("Updated Theme");
            returnedTheme.Description.Should().Be("Updated description");
            returnedTheme.ProjectIds.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateTheme_RequiresAuthentication()
        {
            // Arrange
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var unauthenticatedClient = _factory.CreateUnauthenticatedClient();

            var themeId = ObjectId.GenerateNewId().ToString();
            var theme = new ThemeModel
            {
                Id = themeId,
                Name = "Test Theme",
                Description = "Test description",
                ProjectIds = new List<string>(),
                IsActive = true
            };

            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", theme);

            // Act - Try to update without authentication
            var response = await unauthenticatedClient.PutAsJsonAsync($"/api/v1.0/themes/{themeId}", theme);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ArchiveTheme_ReturnsNoContent_WhenThemeExists()
        {
            // Arrange - Clear database and create theme
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var themeId = ObjectId.GenerateNewId().ToString();
            var theme = new ThemeModel
            {
                Id = themeId,
                Name = "Theme to Archive",
                Description = "This theme will be archived",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await client.PostAsJsonAsync("/api/v1.0/themes", theme);

            // Act
            var response = await client.PutAsync($"/api/v1.0/themes/{themeId}/archive", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the theme is archived
            var getResponse = await client.GetAsync($"/api/v1.0/themes/{themeId}");
            var content = await getResponse.Content.ReadAsStringAsync();
            var archivedTheme = JsonSerializer.Deserialize<ThemeModel>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            archivedTheme!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task ArchiveTheme_ReturnsNotFound_WhenThemeDoesNotExist()
        {
            // Arrange
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var nonExistentId = ObjectId.GenerateNewId().ToString();

            // Act
            var response = await client.PutAsync($"/api/v1.0/themes/{nonExistentId}/archive", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RestoreTheme_ReturnsNoContent_WhenThemeExists()
        {
            // Arrange - Clear database, create and archive theme
            await _factory.ClearDatabaseAsync();
            var client = _factory.CreateAuthenticatedClient();

            var themeId = ObjectId.GenerateNewId().ToString();
            var theme = new ThemeModel
            {
                Id = themeId,
                Name = "Theme to Restore",
                Description = "This theme will be restored",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await client.PostAsJsonAsync("/api/v1.0/themes", theme);
            await client.PutAsync($"/api/v1.0/themes/{themeId}/archive", null);

            // Act
            var response = await client.PutAsync($"/api/v1.0/themes/{themeId}/restore", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the theme is restored
            var getResponse = await client.GetAsync($"/api/v1.0/themes/{themeId}");
            var content = await getResponse.Content.ReadAsStringAsync();
            var restoredTheme = JsonSerializer.Deserialize<ThemeModel>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            restoredTheme!.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetThemesByProject_ReturnsThemes_WhenProjectIsTagged()
        {
            // Arrange - Clear database and create themes with project associations
            await _factory.ClearDatabaseAsync();
            var authenticatedClient = _factory.CreateAuthenticatedClient();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var projectId = ObjectId.GenerateNewId().ToString();

            var themeWithProject = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Theme With Project",
                Description = "A theme associated with a project",
                ProjectIds = new List<string> { projectId },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var themeWithoutProject = new ThemeModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Theme Without Project",
                Description = "A theme not associated with the project",
                ProjectIds = new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", themeWithProject);
            await authenticatedClient.PostAsJsonAsync("/api/v1.0/themes", themeWithoutProject);

            // Act
            var response = await publicClient.GetAsync($"/api/v1.0/themes/by-project/{projectId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var themes = JsonSerializer.Deserialize<List<ThemeModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            themes.Should().NotBeNull();
            themes!.Count.Should().Be(1);
            themes[0].Name.Should().Be("Theme With Project");
        }

        [Fact]
        public async Task GetThemesByProject_ReturnsEmptyList_WhenNoThemesForProject()
        {
            // Arrange
            await _factory.ClearDatabaseAsync();
            var publicClient = _factory.CreateUnauthenticatedClient();

            var projectId = ObjectId.GenerateNewId().ToString();

            // Act
            var response = await publicClient.GetAsync($"/api/v1.0/themes/by-project/{projectId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var themes = JsonSerializer.Deserialize<List<ThemeModel>>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            themes.Should().BeEmpty();
        }
    }
}

