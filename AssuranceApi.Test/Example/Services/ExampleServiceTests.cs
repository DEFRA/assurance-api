using AssuranceApi.Example.Models;
using AssuranceApi.Example.Services;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;

namespace AssuranceApi.Test.Example.Services;

public class ExampleServiceTests
{
    private readonly IExamplePersistence _persistence = Substitute.For<IExamplePersistence>();

    private ExampleService CreateService()
    {
        return new ExampleService(_persistence);
    }

    [Fact]
    public async Task GetByName_ShouldReturnModel_WhenFound()
    {
        // Arrange
        var service = CreateService();
        var expectedName = "TestName";
        var expectedModel = new ExampleModel
        {
            Id = new ObjectId(),
            Name = expectedName,
            Value = "Test Value",
            Counter = 5,
        };

        _persistence.GetByExampleName(expectedName).Returns(expectedModel);

        // Act
        var result = await service.GetByNameAsync(expectedName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(expectedName);
        await _persistence.Received(1).GetByExampleName(expectedName);
    }

    [Fact]
    public async Task GetByName_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var service = CreateService();
        var nonExistentName = "NonExistent";

        _persistence.GetByExampleName(nonExistentName).Returns((ExampleModel?)null);

        // Act
        var result = await service.GetByNameAsync(nonExistentName);

        // Assert
        result.Should().BeNull();
        await _persistence.Received(1).GetByExampleName(nonExistentName);
    }

    [Fact]
    public async Task CreateExample_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var newExample = new ExampleModel
        {
            Id = new ObjectId(),
            Name = "NewExample",
            Value = "New Value",
            Counter = 0,
        };

        _persistence.CreateAsync(newExample).Returns(true);

        // Act
        var result = await service.CreateExampleAsync(newExample);

        // Assert
        result.Should().BeTrue();
        await _persistence.Received(1).CreateAsync(newExample);
    }

    [Fact]
    public async Task CreateExample_ShouldReturnFalse_WhenFailed()
    {
        // Arrange
        var service = CreateService();
        var newExample = new ExampleModel
        {
            Id = new ObjectId(),
            Name = "FailedExample",
            Value = "Failed Value",
            Counter = 0,
        };

        _persistence.CreateAsync(newExample).Returns(false);

        // Act
        var result = await service.CreateExampleAsync(newExample);

        // Assert
        result.Should().BeFalse();
        await _persistence.Received(1).CreateAsync(newExample);
    }

    [Fact]
    public async Task GetAllExamples_ShouldReturnList()
    {
        // Arrange
        var service = CreateService();
        var expectedExamples = new List<ExampleModel>
        {
            new()
            {
                Id = new ObjectId(),
                Name = "Test1",
                Value = "Value1",
                Counter = 1,
            },
            new()
            {
                Id = new ObjectId(),
                Name = "Test2",
                Value = "Value2",
                Counter = 2,
            },
        };

        _persistence.GetAllAsync().Returns(expectedExamples);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedExamples);
        await _persistence.Received(1).GetAllAsync();
    }
}

// Simple service layer that wraps the persistence layer
public class ExampleService
{
    private readonly IExamplePersistence _persistence;

    public ExampleService(IExamplePersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<ExampleModel?> GetByNameAsync(string name)
    {
        return await _persistence.GetByExampleName(name);
    }

    public async Task<bool> CreateExampleAsync(ExampleModel example)
    {
        return await _persistence.CreateAsync(example);
    }

    public async Task<IEnumerable<ExampleModel>> GetAllAsync()
    {
        return await _persistence.GetAllAsync();
    }

    public async Task<bool> UpdateExampleAsync(ExampleModel example)
    {
        return await _persistence.UpdateAsync(example);
    }

    public async Task<bool> DeleteExampleAsync(string name)
    {
        return await _persistence.DeleteAsync(name);
    }
}
