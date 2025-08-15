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
    public class DeliveryPartnersControllerTests
    {
        private readonly DeliveryPartnerValidator _validator;
        private readonly ILogger<DeliveryPartnersController> _logger;

        private static readonly List<DeliveryPartnerModel> _deliveryPartners =
        [
            new ()
            {
                Id = "ID-1",
                Name = "Partner 1",
                IsActive = true,
                CreatedAt = new DateTime(2024, 04, 21).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 21).ToUniversalTime()
            },
            new ()
            {
                Id = "ID-2",
                Name = "Partner 2",
                IsActive = true,
                CreatedAt = new DateTime(2024, 04, 22).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 22).ToUniversalTime()
            },
            new ()
            {
                Id = "ID-3",
                Name = "Partner 3",
                IsActive = true,
                CreatedAt = new DateTime(2024, 04, 23).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 23).ToUniversalTime()
            },
            new ()
            {
                Id = "ID-4",
                Name = "Partner 4",
                IsActive = true,
                CreatedAt = new DateTime(2024, 04, 24).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 24).ToUniversalTime()
            },
            new ()
            {
                Id = "ID-5",
                Name = "Partner 5",
                IsActive = true,
                CreatedAt = new DateTime(2024, 04, 25).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 25).ToUniversalTime()
            }
        ];

        public DeliveryPartnersControllerTests(ITestOutputHelper output)
        {
            _validator = new DeliveryPartnerValidator();
            Log.Logger = new LoggerConfiguration()
                .WriteTo.TestOutput(output)
                .MinimumLevel.Verbose()
                .CreateLogger();

            _logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<DeliveryPartnersController>();
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfDeliveryPartners()
        {
            // Arrange
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetAll();

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.As<List<DeliveryPartnerModel>>().Count.Should().Be(5);
        }

        [Fact]
        public async Task GetAll_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryPartner()
        {
            // Arrange
            var controller = new DeliveryPartnersController(
                null,
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
        public async Task GetById_ExistingId_ReturnsOkResult_WithDeliveryPartner()
        {
            // Arrange
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetById("ID-5");

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.As<DeliveryPartnerModel>().Should().BeEquivalentTo(_deliveryPartners[4]);
        }

        [Fact]
        public async Task GetById_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetById("INVALID");

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetById_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryPartner()
        {
            // Arrange
            var controller = new DeliveryPartnersController(
                null,
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
        public async Task Create_InvalidDeliveryPartner_ReturnsBadRequestObjectResult()
        {
            // Arrange
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Create(new DeliveryPartnerModel());

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_ValidDeliveryPartner_ReturnsCreatedAtAction()
        {
            // Arrange
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Create(_deliveryPartners[0]);

            // Assert
            response.Should().BeOfType<CreatedResult>();
        }

        [Fact]
        public async Task Create_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryPartner()
        {
            // Arrange
            var controller = new DeliveryPartnersController(
                null,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Create(_deliveryPartners[0]);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Update_ExistingId_ReturnsOkResult()
        {
            // Arrange
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            // Act
            var originalDateTime = _deliveryPartners[0].UpdatedAt;
            var response = await controller.Update("ID-1", _deliveryPartners[0]);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.As<DeliveryPartnerModel>().UpdatedAt.Should().NotBe(originalDateTime);
        }

        [Fact]
        public async Task Update_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            // Act
            var originalDateTime = _deliveryPartners[0].UpdatedAt;
            var response = await controller.Update("INVALID", _deliveryPartners[0]);

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Update_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryPartner()
        {
            // Arrange
            var controller = new DeliveryPartnersController(
                null,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Update("ID-1", _deliveryPartners[0]);

            // Assert
            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Delete_ExistingId_ReturnsNoContent()
        {
            // Arrange
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
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
            var mockDeliveryPartnerPersistence = GetDeliveryModelPersistenceMock();

            var controller = new DeliveryPartnersController(
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Delete("INVALID");

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Delete_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingDeliveryPartner()
        {
            // Arrange
            var controller = new DeliveryPartnersController(
                null,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Delete("INVALID");

            // Assert
            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        private static IDeliveryPartnerPersistence GetDeliveryModelPersistenceMock()
        {
            var mockDeliveryPartnerPersistence = Substitute.For<IDeliveryPartnerPersistence>();

            mockDeliveryPartnerPersistence.GetAllAsync().Returns(_deliveryPartners);
            mockDeliveryPartnerPersistence.GetByIdAsync("ID-1").Returns(_deliveryPartners[0]);
            mockDeliveryPartnerPersistence.GetByIdAsync("ID-5").Returns(_deliveryPartners[4]);
            mockDeliveryPartnerPersistence.UpdateAsync("ID-1", Arg.Any<DeliveryPartnerModel>()).Returns(true);
            mockDeliveryPartnerPersistence.UpdateAsync("INVALID", Arg.Any<DeliveryPartnerModel>()).Returns(false);
            mockDeliveryPartnerPersistence.CreateAsync(_deliveryPartners[0]).Returns(true);
            mockDeliveryPartnerPersistence.DeleteAsync("ID-1").Returns(true);
            mockDeliveryPartnerPersistence.DeleteAsync("INVALID").Returns(false);

            return mockDeliveryPartnerPersistence;
        }
    }
}
