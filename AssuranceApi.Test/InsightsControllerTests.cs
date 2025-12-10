using AssuranceApi.Controllers;
using AssuranceApi.Data;
using AssuranceApi.Insights.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

namespace AssuranceApi.Test;

public class InsightsControllerTests
{
    private readonly ILogger<InsightsController> _logger;

    private static readonly List<DeliveryNeedingUpdate> _deliveriesNeedingUpdates =
    [
        new()
        {
            Id = "proj-1",
            Name = "Animal Health Platform",
            Status = "AMBER",
            LastServiceStandardUpdate = DateTime.UtcNow.AddDays(-60),
            DaysSinceStandardUpdate = 60
        },
        new()
        {
            Id = "proj-2",
            Name = "Farming Investment Fund Portal",
            Status = "RED",
            LastServiceStandardUpdate = DateTime.UtcNow.AddDays(-42),
            DaysSinceStandardUpdate = 42
        },
        new()
        {
            Id = "proj-3",
            Name = "Environmental Permits System",
            Status = "AMBER",
            LastServiceStandardUpdate = DateTime.UtcNow.AddDays(-35),
            DaysSinceStandardUpdate = 35
        }
    ];

    private static readonly List<WorseningStandardsDelivery> _deliveriesWithWorseningStandards =
    [
        new()
        {
            Id = "proj-2",
            Name = "Farming Investment Fund Portal",
            Status = "RED",
            StandardChanges =
            [
                new()
                {
                    StandardNumber = 1,
                    StandardName = "Understand users and their needs",
                    StatusHistory = ["GREEN", "GREEN", "GREEN", "GREEN", "AMBER"]
                },
                new()
                {
                    StandardNumber = 5,
                    StandardName = "Make sure everyone can use the service",
                    StatusHistory = ["GREEN", "GREEN", "GREEN", "AMBER", "RED"]
                }
            ]
        },
        new()
        {
            Id = "proj-3",
            Name = "Environmental Permits System",
            Status = "AMBER",
            StandardChanges =
            [
                new()
                {
                    StandardNumber = 9,
                    StandardName = "Create a secure service",
                    StatusHistory = ["GREEN", "GREEN", "GREEN", "GREEN", "RED"]
                }
            ]
        }
    ];

    public InsightsControllerTests(ITestOutputHelper output)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.TestOutput(output)
            .MinimumLevel.Verbose()
            .CreateLogger();

        _logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<InsightsController>();
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsOkResult_WithPrioritisationResponse()
    {
        // Arrange
        var mockPersistence = GetInsightsPersistenceMock();
        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<OkObjectResult>();
        var result = response.As<OkObjectResult>().Value.As<PrioritisationResponse>();
        result.DeliveriesNeedingStandardUpdates.Should().HaveCount(3);
        result.DeliveriesWithWorseningStandards.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsOkResult_WithDeliveriesNeedingUpdates()
    {
        // Arrange
        var mockPersistence = GetInsightsPersistenceMock();
        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<OkObjectResult>();
        var result = response.As<OkObjectResult>().Value.As<PrioritisationResponse>();
        result.DeliveriesNeedingStandardUpdates.Should().BeEquivalentTo(_deliveriesNeedingUpdates);
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsOkResult_WithWorseningStandards()
    {
        // Arrange
        var mockPersistence = GetInsightsPersistenceMock();
        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<OkObjectResult>();
        var result = response.As<OkObjectResult>().Value.As<PrioritisationResponse>();
        result.DeliveriesWithWorseningStandards.Should().BeEquivalentTo(_deliveriesWithWorseningStandards);
    }

    [Fact]
    public async Task GetPrioritisation_UsesDefaultThresholds_WhenNotProvided()
    {
        // Arrange
        var mockPersistence = GetInsightsPersistenceMock();
        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        await controller.GetPrioritisation();

        // Assert
        await mockPersistence.Received(1).GetDeliveriesNeedingStandardUpdatesAsync(14);
        await mockPersistence.Received(1).GetDeliveriesWithWorseningStandardsAsync(14, 5);
    }

    [Fact]
    public async Task GetPrioritisation_UsesCustomThresholds_WhenProvided()
    {
        // Arrange
        var mockPersistence = GetInsightsPersistenceMock();
        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        await controller.GetPrioritisation(standardThreshold: 21, worseningDays: 7);

        // Assert
        await mockPersistence.Received(1).GetDeliveriesNeedingStandardUpdatesAsync(21);
        await mockPersistence.Received(1).GetDeliveriesWithWorseningStandardsAsync(7, 5);
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsEmptyLists_WhenNoDeliveriesNeedAttention()
    {
        // Arrange
        var mockPersistence = Substitute.For<IInsightsPersistence>();
        mockPersistence.GetDeliveriesNeedingStandardUpdatesAsync(Arg.Any<int>())
            .Returns(new List<DeliveryNeedingUpdate>());
        mockPersistence.GetDeliveriesWithWorseningStandardsAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(new List<WorseningStandardsDelivery>());

        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<OkObjectResult>();
        var result = response.As<OkObjectResult>().Value.As<PrioritisationResponse>();
        result.DeliveriesNeedingStandardUpdates.Should().BeEmpty();
        result.DeliveriesWithWorseningStandards.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
    {
        // Arrange
        var controller = new InsightsController(null!, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<ObjectResult>();
        response.As<ObjectResult>().StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsObjectResult_With500Result_WhenPersistenceThrows()
    {
        // Arrange
        var mockPersistence = Substitute.For<IInsightsPersistence>();
        mockPersistence.GetDeliveriesNeedingStandardUpdatesAsync(Arg.Any<int>())
            .Returns<List<DeliveryNeedingUpdate>>(x => throw new Exception("Database connection failed"));

        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<ObjectResult>();
        response.As<ObjectResult>().StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPrioritisation_DeliveriesNeedingUpdates_ContainsExpectedFields()
    {
        // Arrange
        var mockPersistence = GetInsightsPersistenceMock();
        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<OkObjectResult>();
        var result = response.As<OkObjectResult>().Value.As<PrioritisationResponse>();
        var firstDelivery = result.DeliveriesNeedingStandardUpdates.First();

        firstDelivery.Id.Should().NotBeNullOrEmpty();
        firstDelivery.Name.Should().NotBeNullOrEmpty();
        firstDelivery.Status.Should().NotBeNullOrEmpty();
        firstDelivery.LastServiceStandardUpdate.Should().NotBeNull();
        firstDelivery.DaysSinceStandardUpdate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPrioritisation_WorseningStandards_ContainsExpectedFields()
    {
        // Arrange
        var mockPersistence = GetInsightsPersistenceMock();
        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<OkObjectResult>();
        var result = response.As<OkObjectResult>().Value.As<PrioritisationResponse>();
        var firstDelivery = result.DeliveriesWithWorseningStandards.First();

        firstDelivery.Id.Should().NotBeNullOrEmpty();
        firstDelivery.Name.Should().NotBeNullOrEmpty();
        firstDelivery.Status.Should().NotBeNullOrEmpty();
        firstDelivery.StandardChanges.Should().NotBeEmpty();

        var firstChange = firstDelivery.StandardChanges.First();
        firstChange.StandardNumber.Should().BeGreaterThan(0);
        firstChange.StandardName.Should().NotBeNullOrEmpty();
        firstChange.StatusHistory.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPrioritisation_StatusHistory_ContainsValidStatuses()
    {
        // Arrange
        var validStatuses = new[] { "GREEN", "AMBER", "RED" };
        var mockPersistence = GetInsightsPersistenceMock();
        var controller = new InsightsController(mockPersistence, _logger);

        // Act
        var response = await controller.GetPrioritisation();

        // Assert
        response.Should().BeOfType<OkObjectResult>();
        var result = response.As<OkObjectResult>().Value.As<PrioritisationResponse>();

        foreach (var delivery in result.DeliveriesWithWorseningStandards)
        {
            foreach (var change in delivery.StandardChanges)
            {
                foreach (var status in change.StatusHistory)
                {
                    validStatuses.Should().Contain(status);
                }
            }
        }
    }

    private static IInsightsPersistence GetInsightsPersistenceMock()
    {
        var mockPersistence = Substitute.For<IInsightsPersistence>();

        mockPersistence.GetDeliveriesNeedingStandardUpdatesAsync(Arg.Any<int>())
            .Returns(_deliveriesNeedingUpdates);

        mockPersistence.GetDeliveriesWithWorseningStandardsAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(_deliveriesWithWorseningStandards);

        return mockPersistence;
    }
}

