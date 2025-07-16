using AssuranceApi.Controllers;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.ServiceStandard.Services;
using AssuranceApi.Validators;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

namespace AssuranceApi.Test
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public class ServiceStandardsControllerTests
    {
        private readonly ServiceStandardValidator _validator;

        private readonly ILogger<ServiceStandardsController> _logger;

        private static readonly List<ServiceStandardModel> _activeServiceStandards =
        [
            new ServiceStandardModel()
            {
                CreatedAt = new DateTime(2024, 04, 21),
                DeletedAt = null,
                DeletedBy = null,
                Description = "Description for Service Standard 1",
                Guidance = "Guidance for Service Standard 1",
                Id = "1",
                IsActive = true,
                Name = "Service Standard 1",
                Number = 1,
                UpdatedAt = new DateTime(2024, 04, 21),
            },
            new ServiceStandardModel()
            {
                CreatedAt = new DateTime(2024, 04, 22),
                DeletedAt = null,
                DeletedBy = null,
                Description = "Description for Service Standard 2",
                Guidance = "Guidance for Service Standard 2",
                Id = "2",
                IsActive = true,
                Name = "Service Standard 2",
                Number = 2,
                UpdatedAt = new DateTime(2024, 04, 22),
            },
        ];

        private static readonly List<ServiceStandardModel> _allServiceStandards =
        [
            new ServiceStandardModel()
            {
                CreatedAt = new DateTime(2024, 04, 21),
                DeletedAt = null,
                DeletedBy = null,
                Description = "Description for Service Standard 1",
                Guidance = "Guidance for Service Standard 1",
                Id = "1",
                IsActive = true,
                Name = "Service Standard 1",
                Number = 1,
                UpdatedAt = new DateTime(2024, 04, 21),
            },
            new ServiceStandardModel()
            {
                CreatedAt = new DateTime(2024, 04, 22),
                DeletedAt = null,
                DeletedBy = null,
                Description = "Description for Service Standard 2",
                Guidance = "Guidance for Service Standard 2",
                Id = "2",
                IsActive = true,
                Name = "Service Standard 2",
                Number = 2,
                UpdatedAt = new DateTime(2024, 04, 22),
            },
            new ServiceStandardModel()
            {
                CreatedAt = new DateTime(2024, 04, 23),
                DeletedAt = new DateTime(2024, 04, 23),
                DeletedBy = "System",
                Description = "Description for Service Standard 3",
                Guidance = "Guidance for Service Standard 3",
                Id = "3",
                IsActive = true,
                Name = "Service Standard 3",
                Number = 3,
                UpdatedAt = new DateTime(2024, 04, 23),
            },
            new ServiceStandardModel()
            {
                CreatedAt = new DateTime(2024, 04, 24),
                DeletedAt = new DateTime(2024, 04, 24),
                DeletedBy = "System",
                Description = "Description for Service Standard 4",
                Guidance = "Guidance for Service Standard 4",
                Id = "4",
                IsActive = true,
                Name = "Service Standard 4",
                Number = 4,
                UpdatedAt = new DateTime(2024, 04, 24),
            },
        ];

        private static readonly List<ServiceStandardHistory> _serviceStandardHistory =
        [
            new()
            {
                StandardId = "1",
                Id = "1",
                ChangedBy = "System",
                Changes = new ServiceStandardChanges()
                {
                    Description = new ServiceStandardDescriptionChange()
                    {
                        From = "Description 1",
                        To = "Description 2",
                    },
                    Guidance = new ServiceStandardGuidanceChange() { From = "Guidance 1", To = "Guidance 2" },
                    Name = new ServiceStandardNameChange() { From = "Name 1", To = "Name 2" },
                },
                Timestamp = new DateTime(2024, 04, 25),
            },
            new()
            {
                StandardId = "2",
                Id = "2",
                ChangedBy = "System",
                Changes = new ServiceStandardChanges()
                {
                    Description = new ServiceStandardDescriptionChange()
                    {
                        From = "Description 1",
                        To = "Description 2",
                    },
                    Guidance = new ServiceStandardGuidanceChange() { From = "Guidance 1", To = "Guidance 2" },
                    Name = new ServiceStandardNameChange() { From = "Name 1", To = "Name 2" },
                },
                Timestamp = new DateTime(2024, 04, 26),
            },
        ];

        public ServiceStandardsControllerTests(ITestOutputHelper output)
        {
            _validator = new ServiceStandardValidator();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.TestOutput(output)
                .MinimumLevel.Verbose()
                .CreateLogger();

            _logger = new SerilogLoggerFactory(
                Log.Logger
            ).CreateLogger<ServiceStandardsController>();
        }

        [Fact]
        public async Task Create_ReturnsCreatedResult_AndThereIsAValidHistory_WhenAValidStandardIsPassed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );

            var validModel = GetNewInstanceOfServiceStandardModelToDiscardChanges();
            var response = await controller.Create(validModel);

            response
                .Should()
                .BeOfType<CreatedResult>();
        }

        [Fact]

        public async Task Create_ReturnsBadRequestResult_WhenModelNumberIsInvalid()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );

            var invalidModel = GetNewInstanceOfServiceStandardModelToDiscardChanges();
            invalidModel.Number = 21;

            var response = await controller.Create(invalidModel);

            response
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [Fact]

        public async Task Create_ReturnsBadRequestResult_WhenModelDescriptionIsInvalid()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );

            var invalidModel = GetNewInstanceOfServiceStandardModelToDiscardChanges();
            invalidModel.Description = string.Empty;

            var response = await controller.Create(invalidModel);

            response
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        
        public async Task Create_ReturnsBadRequestResult_WhenModelNameIsInvalid()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );

            var invalidModel = GetNewInstanceOfServiceStandardModelToDiscardChanges();
            invalidModel.Name = string.Empty;  

            var response = await controller.Create(invalidModel);

            response
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ServiceStandardsController(null, null, _validator, _logger);
            var validModel = GetNewInstanceOfServiceStandardModelToDiscardChanges();

            var response = await controller.Create(validModel);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Create_ReturnsConflictObjectResult_WhenADuplicateModelIsPassed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            mockServiceStandardPersistence.CreateAsync(Arg.Any<ServiceStandardModel>()).Returns(false);

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );

            var duplicateModel = GetNewInstanceOfServiceStandardModelToDiscardChanges();
            duplicateModel.Id = "3";
            var response = await controller.Create(duplicateModel);

            response.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfActiveServiceStandards_WhenIncludeInactiveIsFalse()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetAll(false);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_activeServiceStandards);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfActiveServiceStandards_WhenIncludeInactiveIsTrue()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetAll(true);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_allServiceStandards);
        }

        [Fact]
        public async Task GetAll_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ServiceStandardsController(null, null, _validator, _logger);
            var response = await controller.GetAll(true);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithActiveServiceStandard_WhenIncludeInactiveIsFalse()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetById("1", false);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_activeServiceStandards[0]);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithInactiveServiceStandard_WhenIncludeInactiveIsTrue()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetById("3", true);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_allServiceStandards[2]);
        }

        [Fact]
        public async Task GetById_NotFoundResult_WhenAnInvalidIdIsPassed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetById("99", true);

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetById_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ServiceStandardsController(null, null, _validator, _logger);
            var response = await controller.GetById("99", true);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Seed_ReturnsOkResult_WithAValidMessage_WhenValidServiceStandardsArePassed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Seed([.. _activeServiceStandards]);
            var responseMessage =
                $"{{ Message = Seeded '{_activeServiceStandards.Count}' professions successfully }}";

            response.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task Seed_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ServiceStandardsController(null, null, _validator, _logger);
            var response = await controller.Seed([.. _activeServiceStandards]);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteAll_ReturnsOkResult_WhenAValidSetupIsUsed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.DeleteAll();

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo("All service standards deleted");
        }

        [Fact]
        public async Task DeleteAll_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ServiceStandardsController(null, null, _validator, _logger);
            var response = await controller.DeleteAll();

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task SoftDelete_ReturnsOkResult_WhenAValidIdIsUsed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.SoftDelete("1");

            response.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task SoftDelete_ReturnsNotFoundResult_WhenAValidIdIsUsed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.SoftDelete("INVALID");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task SoftDelete_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ServiceStandardsController(null, null, _validator, _logger);
            var response = await controller.SoftDelete("1");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Restore_ReturnsOkResult_WhenAValidIdIsUsed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Restore("1");

            response.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task Restore_ReturnsNotFoundResult_WhenAValidIdIsUsed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Restore("INVALID");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Restore_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ServiceStandardsController(null, null, _validator, _logger);
            var response = await controller.Restore("1");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetHistory_ReturnsOkObjectResult_WithAValidListOfChanges_WhenAValidIdIsUsed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetHistory("1");

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_serviceStandardHistory);
        }

        [Fact]
        public async Task GetHistory_ReturnsOkObjectResult_WithEmptyResults_WhenAnInvalidIdIsUsed()
        {
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockServiceStandardHistoryPersistence = GetServiceStandardHistoryPersistenceMock();

            var controller = new ServiceStandardsController(
                mockServiceStandardPersistence,
                mockServiceStandardHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetHistory("99");

            response.Should().BeOfType<OkObjectResult>();
            response
                .As<OkObjectResult>()
                .Value.As<ICollection<ServiceStandardHistory>>()
                .Count.Should()
                .Be(0);
        }

        [Fact]
        public async Task GetHistory_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ServiceStandardsController(null, null, _validator, _logger);
            var response = await controller.GetHistory("99");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        private static IServiceStandardPersistence GetServiceStandardPersistenceMock()
        {
            var mockServiceStandardPersistence = Substitute.For<IServiceStandardPersistence>();

            mockServiceStandardPersistence.GetAllAsync().Returns(_allServiceStandards);
            mockServiceStandardPersistence.GetAllActiveAsync().Returns(_activeServiceStandards);
            mockServiceStandardPersistence.GetByIdAsync("3").Returns(_allServiceStandards[2]);
            mockServiceStandardPersistence.GetActiveByIdAsync("1").Returns(_activeServiceStandards[0]);
            mockServiceStandardPersistence.SeedStandardsAsync(Arg.Any<List<ServiceStandardModel>>()).Returns(true);
            mockServiceStandardPersistence.SoftDeleteAsync("1", Arg.Any<string>()).Returns(true);
            mockServiceStandardPersistence.SoftDeleteAsync("INVALID", Arg.Any<string>()).Returns(false);
            mockServiceStandardPersistence.RestoreAsync("1").Returns(true);
            mockServiceStandardPersistence.RestoreAsync("INVALID").Returns(false);
            mockServiceStandardPersistence.CreateAsync(Arg.Any<ServiceStandardModel>()).Returns(true);

            return mockServiceStandardPersistence;
        }

        private static IServiceStandardHistoryPersistence GetServiceStandardHistoryPersistenceMock()
        {
            var mockServiceStandardHistoryPersistence =
                Substitute.For<IServiceStandardHistoryPersistence>();

            mockServiceStandardHistoryPersistence
                .GetHistoryAsync("1")
                .Returns(_serviceStandardHistory);
            mockServiceStandardHistoryPersistence.GetHistoryAsync("99").Returns([]);

            return mockServiceStandardHistoryPersistence;
        }

        private static ServiceStandardModel GetNewInstanceOfServiceStandardModelToDiscardChanges()
        {
            return new ServiceStandardModel()
            {
                CreatedAt = new DateTime(2024, 04, 21),
                DeletedAt = null,
                DeletedBy = null,
                Description = "Description for Service Standard 1",
                Guidance = "Guidance for Service Standard 1",
                Id = "1",
                IsActive = true,
                Name = "Service Standard 1",
                Number = 1,
                UpdatedAt = new DateTime(2024, 04, 21)
            };
        }
    }
}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.