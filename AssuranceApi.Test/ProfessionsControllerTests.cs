using AssuranceApi.Controllers;
using AssuranceApi.Profession.Models;
using AssuranceApi.Profession.Services;
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
    public class ProfessionsControllerTests
    {
        private readonly ProfessionModelValidator _validator;
        private readonly ILogger<ProfessionsController> _logger;

        private static readonly List<ProfessionModel> _allProfessions =
        [
            new()
            {
                Id = "1",
                Name = "Test 1",
                CreatedAt = new DateTime(2024, 04, 21),
                DeletedAt = null,
                DeletedBy = "",
                Description = "This is Test Profession 1",
                IsActive = false,
                UpdatedAt = new DateTime(2024, 04, 21),
            },
            new()
            {
                Id = "2",
                Name = "Test 2",
                CreatedAt = new DateTime(2024, 04, 22),
                DeletedAt = null,
                DeletedBy = "",
                Description = "This is Test Profession 2",
                IsActive = false,
                UpdatedAt = new DateTime(2024, 04, 22),
            },
            new()
            {
                Id = "3",
                Name = "Test 3",
                CreatedAt = new DateTime(2024, 04, 23),
                DeletedAt = null,
                DeletedBy = "",
                Description = "This is Test Profession 3",
                IsActive = true,
                UpdatedAt = new DateTime(2024, 04, 23),
            },
            new()
            {
                Id = "4",
                Name = "Test 4",
                CreatedAt = new DateTime(2024, 04, 24),
                DeletedAt = null,
                DeletedBy = "",
                Description = "This is Test Profession 4",
                IsActive = true,
                UpdatedAt = new DateTime(2024, 04, 24),
            },
        ];

        private static readonly List<ProfessionModel> _activeProfessions =
        [
            new()
            {
                Id = "3",
                Name = "Test 3",
                CreatedAt = new DateTime(2024, 04, 23),
                DeletedAt = null,
                DeletedBy = "",
                Description = "This is Test Profession 3",
                IsActive = true,
                UpdatedAt = new DateTime(2024, 04, 23),
            },
            new()
            {
                Id = "4",
                Name = "Test 4",
                CreatedAt = new DateTime(2024, 04, 24),
                DeletedAt = null,
                DeletedBy = "",
                Description = "This is Test Profession 4",
                IsActive = true,
                UpdatedAt = new DateTime(2024, 04, 24),
            },
        ];

        private static readonly ProfessionModel _newProfession = new()
        {
            Id = "test-five",
            Name = "Test 5",
            CreatedAt = new DateTime(2024, 04, 25),
            DeletedAt = null,
            DeletedBy = "",
            Description = "This is Test Profession 5",
            IsActive = false,
            UpdatedAt = new DateTime(2024, 04, 25),
        };

        private static readonly ProfessionModel _invalidProfessionModel = new()
        {
            Id = "",
            Name = "Invalid",
            CreatedAt = new DateTime(2024, 04, 25),
            DeletedAt = null,
            DeletedBy = "",
            Description = "This is an Invalid Profession",
            IsActive = false,
            UpdatedAt = new DateTime(2024, 04, 25),
        };

        private static readonly List<ProfessionHistory> _professionHistory =
        [
            new()
            {
                Id = "3",
                ChangedBy = "System",
                Changes = new()
                {
                    Name = new() { From = "Original Name", To = "New Name" },
                    Description = new() { From = "Original Description", To = "New Description" },
                },
                ProfessionId = "",
                Timestamp = new DateTime(2024, 04, 23),
            },
        ];

        public ProfessionsControllerTests(ITestOutputHelper output)
        {
            _validator = new ProfessionModelValidator();
            Log.Logger = new LoggerConfiguration()
                .WriteTo.TestOutput(output)
                .MinimumLevel.Verbose()
                .CreateLogger();

            _logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<ProfessionsController>();
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfActiveProffesions_WhenIncludeInactiveIsFalse()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetAll(false);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_activeProfessions);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfAllProffesions_WhenIncludeInactiveIsTrue()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetAll(true);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_allProfessions);
        }

        [Fact]
        public async Task GetAll_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProfessionsController(null, null, _validator, _logger);
            var response = await controller.GetAll(false);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithMatchingActiveProffesion_WhenIncludeInactiveIsFalse()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetById("3", false);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_activeProfessions[0]);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenThereIsNoMatchingActiveProfession()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetById("5", true);

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithMatchingInactiveProffesion_WhenIncludeInactiveIsTrue()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetById("1", true);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_allProfessions[0]);
        }

        [Fact]
        public async Task Create_ReturnsCreatedResult_WithNewProfessionId_WhenAValidProfessionIsPassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Create(_newProfession);

            response
                .Should()
                .BeOfType<CreatedResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_newProfession);
        }

        [Fact]
        public async Task Create_ReturnsBadRequestObjectResult_WhenAnInvalidProfessionIsPassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Create(_invalidProfessionModel);

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteAll_ReturnsOkResult_WhenAValidSetupIsUsed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.DeleteAll();

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo("All professions deleted");
        }

        [Fact]
        public async Task DeleteAll_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProfessionsController(null, null, _validator, _logger);
            var response = await controller.DeleteAll();

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task SoftDelete_ReturnsOkResult_WhenAValidIdIsPassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.SoftDelete("3");

            response.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task SoftDelete_ReturnsNotFoundResult_WhenAnInvalidIdIsPassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.SoftDelete("INVALID");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task SoftDelete_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProfessionsController(null, null, _validator, _logger);
            var response = await controller.SoftDelete("3");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Restore_ReturnsOkResult_WhenAValidIdIsPassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Restore("3");

            response.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task Restore_ReturnsNotFoundResult_WhenAnInvalidIdIsPassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Restore("INVALID");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Restore_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProfessionsController(null, null, _validator, _logger);
            var response = await controller.Restore("3");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetHistory_ReturnsOkResult_WithAValidHistory_WhenAValidIdIsPassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetHistory("3");

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_professionHistory);
        }

        [Fact]
        public async Task GetHistory_ReturnsOkResult_WithAnEmptyValidHistory_WhenAnInvalidIdIsPassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetHistory("INVALID");

            response.Should().BeOfType<OkObjectResult>();
            response
                .As<OkObjectResult>()
                .Value.As<ICollection<ProfessionHistory>>()
                .Count.Should()
                .Be(0);
        }

        [Fact]
        public async Task GetHistory_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProfessionsController(null, null, _validator, _logger);
            var response = await controller.GetHistory("3");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Seed_ReturnsOkResult_WithAValidMessage_WhenValidProfessionsArePassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Seed([.. _activeProfessions]);
            var responseMessage =
                $"{{ Message = Seeded '{_activeProfessions.Count}' professions successfully }}";

            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.ToString().Should().Be(responseMessage);
        }

        [Fact]
        public async Task Seed_ReturnsBadRequestObjectResult_WithAValidMessage_WhenAnInvalidProfessionsArePassed()
        {
            var mockProfessionPersistence = GetProfessionPersistenceMock();
            var mockProfessionHistoryPersistence = GetProfessionHistoryPersistenceMock();

            var controller = new ProfessionsController(
                mockProfessionPersistence,
                mockProfessionHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.Seed([_invalidProfessionModel]);

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Seed_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProfessionsController(null, null, _validator, _logger);
            var response = await controller.Seed([.. _activeProfessions]);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        private static IProfessionPersistence GetProfessionPersistenceMock()
        {
            var mockProfessionPersistence = Substitute.For<IProfessionPersistence>();

            mockProfessionPersistence.GetAllAsync().Returns(_allProfessions);
            mockProfessionPersistence.GetAllActiveAsync().Returns(_activeProfessions);
            mockProfessionPersistence.GetByIdAsync("1").Returns(_allProfessions[0]);
            mockProfessionPersistence.GetActiveByIdAsync("3").Returns(_activeProfessions[0]);
            mockProfessionPersistence.CreateAsync(Arg.Any<ProfessionModel>()).Returns(true);
            mockProfessionPersistence.SoftDeleteAsync("3", Arg.Any<string>()).Returns(true);
            mockProfessionPersistence.SoftDeleteAsync("INVALID", Arg.Any<string>()).Returns(false);
            mockProfessionPersistence.RestoreAsync("3").Returns(true);
            mockProfessionPersistence.RestoreAsync("INVALID").Returns(false);

            return mockProfessionPersistence;
        }

        private static IProfessionHistoryPersistence GetProfessionHistoryPersistenceMock()
        {
            var mockProfessionHistoryPersistence = Substitute.For<IProfessionHistoryPersistence>();

            mockProfessionHistoryPersistence.GetHistoryAsync("3").Returns(_professionHistory);
            mockProfessionHistoryPersistence
                .GetHistoryAsync("INVALID")
                .Returns(new List<ProfessionHistory>());

            return mockProfessionHistoryPersistence;
        }
    }
}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.