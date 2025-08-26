using AssuranceApi.Controllers;
using AssuranceApi.Data;
using AssuranceApi.Data.Models;
using AssuranceApi.Validators;
using Microsoft.AspNetCore.Mvc;
using Serilog.Extensions.Logging;
using Serilog;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using NSubstitute;
using FluentAssertions;

namespace AssuranceApi.Test
{
    public class DeliveryGroupsControllerTests
    {
        private readonly DeliveryGroupValidator _validator;
        private readonly ILogger<DeliveryGroupsController> _logger;

        private static readonly List<DeliveryGroupModel> _deliveryGroups =
        [
            new ()
            {
                Id = "ID-1",
                Name = "Group 1",
                IsActive = true,
                Status = "Active",
                Lead = "John Doe",
                CreatedAt = new DateTime(2024, 04, 21).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 21).ToUniversalTime()
            },
            new ()
            {
                Id = "ID-2",
                Name = "Group 2",
                IsActive = true,
                Status = "Active",
                Lead = "Jane Smith",
                CreatedAt = new DateTime(2024, 04, 22).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 22).ToUniversalTime()
            },
            new ()
            {
                Id = "ID-3",
                Name = "Group 3",
                IsActive = true,
                Status = "Inactive",
                Lead = "Bob Johnson",
                CreatedAt = new DateTime(2024, 04, 23).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 23).ToUniversalTime()
            },
            new ()
            {
                Id = "ID-4",
                Name = "Group 4",
                IsActive = true,
                Status = "Active",
                Lead = "Alice Brown",
                CreatedAt = new DateTime(2024, 04, 24).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 24).ToUniversalTime()
            },
            new ()
            {
                Id = "ID-5",
                Name = "Group 5",
                IsActive = true,
                Status = "Pending",
                Lead = "Charlie Wilson",
                CreatedAt = new DateTime(2024, 04, 25).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 25).ToUniversalTime()
            }
        ];

        public DeliveryGroupsControllerTests(ITestOutputHelper output)
        {
            _validator = new DeliveryGroupValidator();
            Log.Logger = new LoggerConfiguration()
                .WriteTo.TestOutput(output)
                .MinimumLevel.Verbose()
                .CreateLogger();

            _logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<DeliveryGroupsController>();
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfDeliveryGroups()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetAll();

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.As<List<DeliveryGroupModel>>().Count.Should().Be(5);
        }

        [Fact]
        public async Task GetAll_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryGroup()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                null!,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetAll();

            // Assert
            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetById_ExistingId_ReturnsOkResult_WithDeliveryGroup()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetById("ID-5");

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.As<DeliveryGroupModel>().Should().BeEquivalentTo(_deliveryGroups[4]);
        }

        [Fact]
        public async Task GetById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetById("INVALID");

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetById_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryGroup()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                null!,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetById("ANY");

            // Assert
            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Create_InvalidDeliveryGroup_ReturnsBadRequestObjectResult()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Create(new DeliveryGroupModel());

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_ValidDeliveryGroup_ReturnsCreatedAtAction()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Create(_deliveryGroups[0]);

            // Assert
            response.Should().BeOfType<CreatedResult>();
        }

        [Fact]
        public async Task Create_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryGroup()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                null!,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Create(_deliveryGroups[0]);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Update_ExistingId_ReturnsOkResult()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var originalDateTime = _deliveryGroups[0].UpdatedAt;
            var response = await controller.Update("ID-1", _deliveryGroups[0]);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.As<DeliveryGroupModel>().UpdatedAt.Should().NotBe(originalDateTime);
        }

        [Fact]
        public async Task Update_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var originalDateTime = _deliveryGroups[0].UpdatedAt;
            var response = await controller.Update("INVALID", _deliveryGroups[0]);

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Update_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryGroup()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                null!,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Update("ID-1", _deliveryGroups[0]);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Delete_ExistingId_ReturnsNoContent()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Delete("ID-1");

            // Assert
            response.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Delete("INVALID");

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Delete_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryGroup()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                null!,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Delete("INVALID");

            // Assert
            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Create_DeliveryGroupWithEmptyName_ReturnsBadRequestObjectResult()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                GetDeliveryGroupPersistenceMock(),
                _validator,
                _logger
            );

            var deliveryGroup = new DeliveryGroupModel
            {
                Id = "test-id",
                Name = "", // Empty name should fail validation
                Status = "Pending",
                Lead = "Test Lead",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var response = await controller.Create(deliveryGroup);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_DeliveryGroupWithEmptyStatus_ReturnsBadRequestObjectResult()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                GetDeliveryGroupPersistenceMock(),
                _validator,
                _logger
            );

            var deliveryGroup = new DeliveryGroupModel
            {
                Id = "test-id",
                Name = "Test Group",
                Status = "", // Empty status should fail validation
                Lead = "Test Lead",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var response = await controller.Create(deliveryGroup);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_DeliveryGroupWithNullName_ReturnsBadRequestObjectResult()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                GetDeliveryGroupPersistenceMock(),
                _validator,
                _logger
            );

            var deliveryGroup = new DeliveryGroupModel
            {
                Id = "test-id",
                Name = null!, // Null name should fail validation
                Status = "Pending",
                Lead = "Test Lead",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var response = await controller.Create(deliveryGroup);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Update_DeliveryGroupWithMismatchedId_ReturnsOkResult()
        {
            // Arrange  
            var controller = new DeliveryGroupsController(
                GetDeliveryGroupPersistenceMock(),
                _validator,
                _logger
            );

            var deliveryGroup = new DeliveryGroupModel
            {
                Id = "DIFFERENT-ID", // URL ID takes precedence over body ID
                Name = "Updated Group",
                Status = "Active",
                Lead = "Updated Lead",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var response = await controller.Update("ID-1", deliveryGroup);

            // Assert
            // Note: The controller uses the URL ID, not the body ID, following the same pattern as DeliveryPartners
            response.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_WithWhitespaceId_ReturnsNotFound()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                GetDeliveryGroupPersistenceMock(),
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetById("   "); // Whitespace-only ID

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Delete_WithWhitespaceId_ReturnsNotFound()
        {
            // Arrange
            var controller = new DeliveryGroupsController(
                GetDeliveryGroupPersistenceMock(),
                _validator,
                _logger
            );

            // Act
            var response = await controller.Delete("   "); // Whitespace-only ID

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        private static IDeliveryGroupPersistence GetDeliveryGroupPersistenceMock()
        {
            var mockDeliveryGroupPersistence = Substitute.For<IDeliveryGroupPersistence>();

            mockDeliveryGroupPersistence.GetAllAsync().Returns(_deliveryGroups);
            mockDeliveryGroupPersistence.GetByIdAsync("ID-1").Returns(_deliveryGroups[0]);
            mockDeliveryGroupPersistence.GetByIdAsync("ID-5").Returns(_deliveryGroups[4]);
            mockDeliveryGroupPersistence.UpdateAsync("ID-1", Arg.Any<DeliveryGroupModel>()).Returns(true);
            mockDeliveryGroupPersistence.UpdateAsync("INVALID", Arg.Any<DeliveryGroupModel>()).Returns(false);
            mockDeliveryGroupPersistence.CreateAsync(_deliveryGroups[0]).Returns(true);
            mockDeliveryGroupPersistence.DeleteAsync("ID-1").Returns(true);
            mockDeliveryGroupPersistence.DeleteAsync("INVALID").Returns(false);

            return mockDeliveryGroupPersistence;
        }
    }
}
