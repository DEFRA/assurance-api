using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.ServiceStandard.Services;
using FluentAssertions;
using NSubstitute;

namespace AssuranceApi.Test.ServiceStandard.Services;

public class ServiceStandardServiceTests
{
    private readonly IServiceStandardPersistence _serviceStandardPersistence = Substitute.For<IServiceStandardPersistence>();

    private ServiceStandardService CreateService()
    {
        return new ServiceStandardService(_serviceStandardPersistence);
    }

    [Fact]
    public async Task GetAllStandards_ShouldReturnActiveOnly_WhenIncludeInactiveIsFalse()
    {
        // Arrange
        var service = CreateService();
        var expectedStandards = new List<ServiceStandardModel>
        {
            new() { Id = "1", Number = 1, Name = "Standard 1", IsActive = true },
            new() { Id = "2", Number = 2, Name = "Standard 2", IsActive = true }
        };

        _serviceStandardPersistence.GetAllActiveAsync().Returns(expectedStandards);

        // Act
        var result = await service.GetAllStandardsAsync(includeInactive: false);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedStandards);
        await _serviceStandardPersistence.Received(1).GetAllActiveAsync();
        await _serviceStandardPersistence.DidNotReceive().GetAllAsync();
    }

    [Fact]
    public async Task GetAllStandards_ShouldReturnAll_WhenIncludeInactiveIsTrue()
    {
        // Arrange
        var service = CreateService();
        var expectedStandards = new List<ServiceStandardModel>
        {
            new() { Id = "1", Number = 1, Name = "Standard 1", IsActive = true },
            new() { Id = "2", Number = 2, Name = "Standard 2", IsActive = false }
        };

        _serviceStandardPersistence.GetAllAsync().Returns(expectedStandards);

        // Act
        var result = await service.GetAllStandardsAsync(includeInactive: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedStandards);
        await _serviceStandardPersistence.Received(1).GetAllAsync();
        await _serviceStandardPersistence.DidNotReceive().GetAllActiveAsync();
    }

    [Fact]
    public async Task GetStandardById_ShouldReturnStandard_WhenExists()
    {
        // Arrange
        var service = CreateService();
        var standardId = "standard-1";
        var expectedStandard = new ServiceStandardModel
        {
            Id = standardId,
            Number = 1,
            Name = "Understand users and their needs",
            Description = "Research to develop a deep knowledge of users",
            IsActive = true
        };

        _serviceStandardPersistence.GetActiveByIdAsync(standardId).Returns(expectedStandard);

        // Act
        var result = await service.GetStandardByIdAsync(standardId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Understand users and their needs");
        await _serviceStandardPersistence.Received(1).GetActiveByIdAsync(standardId);
    }

    [Fact]
    public async Task GetStandardById_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var service = CreateService();
        var standardId = "nonexistent";

        _serviceStandardPersistence.GetActiveByIdAsync(standardId).Returns((ServiceStandardModel?)null);

        // Act
        var result = await service.GetStandardByIdAsync(standardId);

        // Assert
        result.Should().BeNull();
        await _serviceStandardPersistence.Received(1).GetActiveByIdAsync(standardId);
    }

    [Fact]
    public async Task GetStandardById_ShouldReturnInactive_WhenIncludeInactiveIsTrue()
    {
        // Arrange
        var service = CreateService();
        var standardId = "inactive-standard";
        var expectedStandard = new ServiceStandardModel
        {
            Id = standardId,
            Number = 1,
            Name = "Inactive Standard",
            IsActive = false
        };

        _serviceStandardPersistence.GetByIdAsync(standardId).Returns(expectedStandard);

        // Act
        var result = await service.GetStandardByIdAsync(standardId, includeInactive: true);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();
        await _serviceStandardPersistence.Received(1).GetByIdAsync(standardId);
    }

    [Fact]
    public async Task SeedStandards_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var standards = new List<ServiceStandardModel>
        {
            new() { Id = "1", Number = 1, Name = "Standard 1" },
            new() { Id = "2", Number = 2, Name = "Standard 2" }
        };

        _serviceStandardPersistence.SeedStandardsAsync(standards).Returns(true);

        // Act
        var result = await service.SeedStandardsAsync(standards);

        // Assert
        result.Should().BeTrue();
        await _serviceStandardPersistence.Received(1).SeedStandardsAsync(standards);
    }

    [Fact]
    public async Task SeedStandards_ShouldReturnFalse_WhenFailed()
    {
        // Arrange
        var service = CreateService();
        var standards = new List<ServiceStandardModel>
        {
            new() { Id = "1", Number = 1, Name = "Standard 1" }
        };

        _serviceStandardPersistence.SeedStandardsAsync(standards).Returns(false);

        // Act
        var result = await service.SeedStandardsAsync(standards);

        // Assert
        result.Should().BeFalse();
        await _serviceStandardPersistence.Received(1).SeedStandardsAsync(standards);
    }

    [Fact]
    public async Task SoftDeleteStandard_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var standardId = "standard-1";
        var deletedBy = "test-user";

        _serviceStandardPersistence.SoftDeleteAsync(standardId, deletedBy).Returns(true);

        // Act
        var result = await service.SoftDeleteStandardAsync(standardId, deletedBy);

        // Assert
        result.Should().BeTrue();
        await _serviceStandardPersistence.Received(1).SoftDeleteAsync(standardId, deletedBy);
    }

    [Fact]
    public async Task RestoreStandard_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var standardId = "standard-1";

        _serviceStandardPersistence.RestoreAsync(standardId).Returns(true);

        // Act
        var result = await service.RestoreStandardAsync(standardId);

        // Assert
        result.Should().BeTrue();
        await _serviceStandardPersistence.Received(1).RestoreAsync(standardId);
    }

    [Fact]
    public async Task GetStandardsSummary_ShouldReturnCorrectCounts()
    {
        // Arrange
        var service = CreateService();
        var allStandards = new List<ServiceStandardModel>
        {
            new() { Id = "1", Number = 1, IsActive = true },
            new() { Id = "2", Number = 2, IsActive = true },
            new() { Id = "3", Number = 3, IsActive = false },
            new() { Id = "4", Number = 4, IsActive = false }
        };

        _serviceStandardPersistence.GetAllAsync().Returns(allStandards);

        // Act
        var result = await service.GetStandardsSummaryAsync();

        // Assert
        result.TotalStandards.Should().Be(4);
        result.ActiveStandards.Should().Be(2);
        result.InactiveStandards.Should().Be(2);
    }

    [Fact]
    public async Task ClearAllStandards_ShouldCallDeleteAll()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.ClearAllStandardsAsync();

        // Assert
        await _serviceStandardPersistence.Received(1).DeleteAllAsync();
    }
}

// Service layer for service standards management
public class ServiceStandardService
{
    private readonly IServiceStandardPersistence _serviceStandardPersistence;

    public ServiceStandardService(IServiceStandardPersistence serviceStandardPersistence)
    {
        _serviceStandardPersistence = serviceStandardPersistence;
    }

    public async Task<List<ServiceStandardModel>> GetAllStandardsAsync(bool includeInactive = false)
    {
        return includeInactive 
            ? await _serviceStandardPersistence.GetAllAsync()
            : await _serviceStandardPersistence.GetAllActiveAsync();
    }

    public async Task<ServiceStandardModel?> GetStandardByIdAsync(string id, bool includeInactive = false)
    {
        return includeInactive
            ? await _serviceStandardPersistence.GetByIdAsync(id)
            : await _serviceStandardPersistence.GetActiveByIdAsync(id);
    }

    public async Task<bool> SeedStandardsAsync(List<ServiceStandardModel> standards)
    {
        return await _serviceStandardPersistence.SeedStandardsAsync(standards);
    }

    public async Task<bool> SoftDeleteStandardAsync(string id, string deletedBy)
    {
        return await _serviceStandardPersistence.SoftDeleteAsync(id, deletedBy);
    }

    public async Task<bool> RestoreStandardAsync(string id)
    {
        return await _serviceStandardPersistence.RestoreAsync(id);
    }

    public async Task<StandardsSummary> GetStandardsSummaryAsync()
    {
        var allStandards = await _serviceStandardPersistence.GetAllAsync();
        
        return new StandardsSummary
        {
            TotalStandards = allStandards.Count,
            ActiveStandards = allStandards.Count(s => s.IsActive),
            InactiveStandards = allStandards.Count(s => !s.IsActive)
        };
    }

    public async Task ClearAllStandardsAsync()
    {
        await _serviceStandardPersistence.DeleteAllAsync();
    }
}

// DTO for standards summary
public class StandardsSummary
{
    public int TotalStandards { get; set; }
    public int ActiveStandards { get; set; }
    public int InactiveStandards { get; set; }
} 