using AssuranceApi.Controllers;
using AssuranceApi.Data.Models;
using AssuranceApi.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Serilog;
using Xunit.Abstractions;
using AssuranceApi.Data;
using FluentAssertions;
using NSubstitute;
using AssuranceApi.Project.Models;
using NSubstitute.ExceptionExtensions;
using FluentAssertions.Primitives;
using NSubstitute.Core.Arguments;

namespace AssuranceApi.Test
{
    public class ProjectDeliveryPartnerControllerTests
    {
        private readonly ProjectDeliveryPartnerModelValidator _validator;

        private readonly ILogger<ProjectDeliveryPartnerController> _logger;

        public ProjectDeliveryPartnerControllerTests(ITestOutputHelper output)
        {
            _validator = new ProjectDeliveryPartnerModelValidator();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.TestOutput(output)
                .MinimumLevel.Verbose()
                .CreateLogger();

            _logger = new SerilogLoggerFactory(
                Log.Logger
            ).CreateLogger<ProjectDeliveryPartnerController>();
        }

        [Fact]
        public async Task Create_ReturnsCreatedResult_WhenAValidModelIsPassed()
        {
            var mockServiceStandardPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockServiceStandardPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.CreateProjectDeliveryPartner("1", GetValidProjectDeliveryPartnerModel());

            response.Should().BeOfType<CreatedResult>();
        }

        [Fact]
        public async Task Create_ReturnsBadRequestObjectResult_WhenAnInvalidProjectIdIsPassed()
        {
            var mockServiceStandardPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockServiceStandardPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.CreateProjectDeliveryPartner("INVALID", GetValidProjectDeliveryPartnerModel());

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_ReturnsBadRequestObjectResult_WhenAnInvalidDeliveryPartnerIdIsPassed()
        {
            var mockServiceStandardPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockServiceStandardPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();
            model.DeliveryPartnerId = "INVALID";

            var response = await controller.CreateProjectDeliveryPartner("1", model);

            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_ReturnsBadRequestObjectResult_WhenAnEmptyProjectIdIsPassed()
        {
            var mockServiceStandardPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockServiceStandardPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.CreateProjectDeliveryPartner(string.Empty, GetValidProjectDeliveryPartnerModel());

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_ReturnsBadRequestObjectResult_WhenAnEmptyProjectDeliveryPartnerModelIsPassed()
        {
            var mockServiceStandardPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockServiceStandardPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.CreateProjectDeliveryPartner("1", null);

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_ReturnsNotFoundObjectResult_WhenProjectIdDoesntExist()
        {
            var mockServiceStandardPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockServiceStandardPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();
            model.ProjectId = "2";

            var response = await controller.CreateProjectDeliveryPartner("2", model);

            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_ReturnsNotFoundObjectResult_WhenDeliveryPartnerIdDoesntExist()
        {
            var mockServiceStandardPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockServiceStandardPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();
            model.DeliveryPartnerId = "2";

            var response = await controller.CreateProjectDeliveryPartner("1", model);

            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingProjectDeliveryPartner()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                null,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();
            model.DeliveryPartnerId = "2";

            var response = await controller.CreateProjectDeliveryPartner("1", model);

            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetProjectDeliveryPartnersByProjectId_ReturnsOkResult_WhenProjectIdIsValid()
        {
            var mockProjectDeliveryPartnerPersistence = Substitute.For<IProjectDeliveryPartnerPersistence>();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var expectedList = new List<ProjectDeliveryPartnerModel> { GetValidProjectDeliveryPartnerModel() };
            mockProjectDeliveryPartnerPersistence.GetByProjectAsync("1").Returns(expectedList);

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectDeliveryPartnersByProjectId("1");

            response.Should().BeOfType<OkObjectResult>();
            var okResult = response as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedList);
        }

        [Fact]
        public async Task GetProjectDeliveryPartnersByProjectId_ReturnsBadRequest_WhenProjectIdIsEmpty()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectDeliveryPartnersByProjectId(string.Empty);

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetProjectDeliveryPartnersByProjectId_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                null,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectDeliveryPartnersByProjectId("1");

            response.Should().BeOfType<ObjectResult>();
            var objectResult = response as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetProjectsByDeliveryPartnerId_ReturnsOkResult_WhenDeliveryPartnerIdIsValid()
        {
            var mockProjectDeliveryPartnerPersistence = Substitute.For<IProjectDeliveryPartnerPersistence>();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var deliveryPartnerId = "2";
            var expectedList = GetValidProjectDeliveryPartnerModelList()
                .Where(x => x.DeliveryPartnerId == deliveryPartnerId)
                .ToList();
            mockProjectDeliveryPartnerPersistence.GetByDeliveryPartnerAsync(deliveryPartnerId).Returns(expectedList);

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectsByDeliveryPartnerId(deliveryPartnerId);

            response.Should().BeOfType<OkObjectResult>();
            var okResult = response as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(GetValidProjectModels());
        }

        [Fact]
        public async Task GetProjectsByDeliveryPartnerId_ReturnsBadRequest_WhenDeliveryPartnerIdIsEmpty()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectsByDeliveryPartnerId(string.Empty);

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetProjectsByDeliveryPartnerId_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                null,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectsByDeliveryPartnerId("1");

            response.Should().BeOfType<ObjectResult>();
            var objectResult = response as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetProjectDeliveryPartner_ReturnsOkResult_WhenProjectIdAndDeliveryPartnerIdAreValid()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var expected = GetValidProjectDeliveryPartnerModel();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectDeliveryPartner("1", "1");

            response.Should().BeOfType<OkObjectResult>();
            var okResult = response as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetProjectDeliveryPartner_ReturnsBadRequest_WhenProjectIdOrDeliveryPartnerIdIsEmpty()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectDeliveryPartner(string.Empty, "1");
            response.Should().BeOfType<BadRequestObjectResult>();

            response = await controller.GetProjectDeliveryPartner("1", string.Empty);
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetProjectDeliveryPartner_ReturnsNotFound_WhenNoProjectDeliveryPartnerFound()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectDeliveryPartner("1", "2");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetProjectDeliveryPartner_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                null,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectDeliveryPartner("1", "1");

            response.Should().BeOfType<ObjectResult>();
            var objectResult = response as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Update_ReturnsOkResult_WhenAValidModelIsPassed()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var model = GetValidProjectDeliveryPartnerModel();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.UpdateProjectDeliveryPartner("1", "1", model);

            response.Should().BeOfType<OkObjectResult>();
            var okResult = response as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(model);
        }

        [Fact]
        public async Task Update_ReturnsBadRequestObjectResult_WhenProjectIdOrDeliveryPartnerIdIsEmpty()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();

            var response = await controller.UpdateProjectDeliveryPartner(string.Empty, "1", model);
            response.Should().BeOfType<BadRequestObjectResult>();

            response = await controller.UpdateProjectDeliveryPartner("1", string.Empty, model);
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Update_ReturnsBadRequestObjectResult_WhenModelIsNull()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.UpdateProjectDeliveryPartner("1", "1", null);

            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Update_ReturnsNotFoundObjectResult_WhenProjectDeliveryPartnerDoesNotExist()
        {
            var mockProjectDeliveryPartnerPersistence = Substitute.For<IProjectDeliveryPartnerPersistence>();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            mockProjectDeliveryPartnerPersistence.GetAsync("1", "2").Returns((ProjectDeliveryPartnerModel?)null);

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();
            model.DeliveryPartnerId = "2";

            var response = await controller.UpdateProjectDeliveryPartner("1", "2", model);

            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Update_ReturnsNotFoundObjectResult_WhenProjectIdDoesNotExist()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = Substitute.For<IProjectPersistence>();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            mockProjectPersistence.GetByIdAsync("2").Returns((ProjectModel?)null);

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();
            model.ProjectId = "2";

            var response = await controller.UpdateProjectDeliveryPartner("2", "1", model);

            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Update_ReturnsNotFoundObjectResult_WhenDeliveryPartnerIdDoesNotExist()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();
            model.DeliveryPartnerId = "2";

            var response = await controller.UpdateProjectDeliveryPartner("1", "2", model);

            response.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Update_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                null,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var model = GetValidProjectDeliveryPartnerModel();

            var response = await controller.UpdateProjectDeliveryPartner("1", "1", model);

            response.Should().BeOfType<ObjectResult>();
            var objectResult = response as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenProjectDeliveryPartnerExists()
        {
            var mockProjectDeliveryPartnerPersistence = Substitute.For<IProjectDeliveryPartnerPersistence>();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            mockProjectDeliveryPartnerPersistence.DeleteAsync("1", "1").Returns(true);

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.DeleteProjectDeliveryPartner("1", "1");

            response.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenProjectIdOrDeliveryPartnerIdIsEmpty()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.DeleteProjectDeliveryPartner(string.Empty, "1");
            response.Should().BeOfType<BadRequestObjectResult>();

            response = await controller.DeleteProjectDeliveryPartner("1", string.Empty);
            response.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenProjectDeliveryPartnerDoesNotExist()
        {
            var mockProjectDeliveryPartnerPersistence = GetProjectDeliveryPartnerPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                mockProjectDeliveryPartnerPersistence,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.DeleteProjectDeliveryPartner("1", "2");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryPartnerPersistence = GetDeliveryPartnerPersistenceMock();

            var controller = new ProjectDeliveryPartnerController(
                null,
                mockProjectPersistence,
                mockDeliveryPartnerPersistence,
                _validator,
                _logger
            );

            var response = await controller.DeleteProjectDeliveryPartner("1", "1");

            response.Should().BeOfType<ObjectResult>();
            var objectResult = response as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        private static IProjectDeliveryPartnerPersistence GetProjectDeliveryPartnerPersistenceMock()
        {
            var mockProjectDeliveryPartnerPersistence = Substitute.For<IProjectDeliveryPartnerPersistence>();
            mockProjectDeliveryPartnerPersistence.GetAsync("1", "1").Returns(GetValidProjectDeliveryPartnerModel());
            mockProjectDeliveryPartnerPersistence.UpsertAsync(Arg.Any<ProjectDeliveryPartnerModel>()).Returns(true);

            return mockProjectDeliveryPartnerPersistence;
        }

        private static IProjectPersistence GetProjectPersistenceMock()
        {
            var mockProjectPersistence = Substitute.For<IProjectPersistence>();
            mockProjectPersistence.GetByIdAsync("1").Returns(GetValidProjectModel());
            mockProjectPersistence
                .GetByIdsAsync(Arg.Is<List<string>>(ids => ids != null && ids.Count == 3 && ids.Contains("2") && ids.Contains("3") && ids.Contains("4")))
                .Returns(GetValidProjectModels());

            return mockProjectPersistence;
        }

        private static IDeliveryPartnerPersistence GetDeliveryPartnerPersistenceMock()
        {
            var mockDeliveryPartnerPersistence = Substitute.For<IDeliveryPartnerPersistence>();
            mockDeliveryPartnerPersistence.GetByIdAsync("1").Returns(GetValidDeliveryPartnerModel());

            return mockDeliveryPartnerPersistence;
        }

        private static ProjectModel GetValidProjectModel()
        {
            return new()
            {
                Commentary = "This is Test Project 1",
                DefCode = "1234",
                Id = "1",
                LastUpdated = new DateTime(2024, 04, 21).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Name = "Test Project 1",
                Phase = "Discovery",
                StandardsSummary =
                [
                    new()
                    {
                        AggregatedCommentary = "",
                        AggregatedStatus = "",
                        LastUpdated = new DateTime(2024, 04, 21),
                        Professions =
                        [
                            new()
                            {
                                Commentary = "Profession Update",
                                LastUpdated = new DateTime(2024, 04, 21),
                                ProfessionId = "1",
                                Status = "Status",
                            },
                        ],
                        StandardId = "1",
                    },
                ],
                Status = "GREEN",
                Tags = ["TAG1", "TAG2"],
                UpdateDate = new DateTime(2024, 04, 21).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            };
        }



        private static DeliveryPartnerModel GetValidDeliveryPartnerModel()
        {
            return new()
            {
                Id = "1",
                Name = "Test Delivery Partner 1",
                CreatedAt = new DateTime(2024, 04, 21),
                IsActive = true,
                UpdatedAt = new DateTime(2024, 04, 21),
            };
        }

        private static ProjectDeliveryPartnerModel GetValidProjectDeliveryPartnerModel()
        {
            return new()
            {
                Id = "1",
                ProjectId = "1",
                DeliveryPartnerId = "1",
                EngagementManager = "Manager 1",
                EngagementStarted = new DateTime(2024, 04, 21, 0, 0, 0, 0),
                EngagementEnded = DateTime.MinValue,
            };
        }

        private static List<ProjectDeliveryPartnerModel> GetValidProjectDeliveryPartnerModelList()
        {
            return
            [
                new () {
                    Id = "2",
                    ProjectId = "2",
                    DeliveryPartnerId = "2",
                    EngagementManager = "Manager 2",
                    EngagementStarted = new DateTime(2024, 04, 22, 0, 0, 0, 0),
                    EngagementEnded = DateTime.MinValue,
                },
                new () {
                    Id = "3",
                    ProjectId = "3",
                    DeliveryPartnerId = "2",
                    EngagementManager = "Manager 2",
                    EngagementStarted = new DateTime(2024, 04, 23, 0, 0, 0, 0),
                    EngagementEnded = DateTime.MinValue,
                },
                new () {
                    Id = "4",
                    ProjectId = "4",
                    DeliveryPartnerId = "2",
                    EngagementManager = "Manager 2",
                    EngagementStarted = new DateTime(2024, 04, 24, 0, 0, 0, 0),
                    EngagementEnded = DateTime.MinValue,
                },
                new () {
                    Id = "5",
                    ProjectId = "4",
                    DeliveryPartnerId = "3",
                    EngagementManager = "Manager 5",
                    EngagementStarted = new DateTime(2024, 04, 25, 0, 0, 0, 0),
                    EngagementEnded = DateTime.MinValue,
                },
                new() {
                    Id = "6",
                    ProjectId = "5",
                    DeliveryPartnerId = "3",
                    EngagementManager = "Manager 1",
                    EngagementStarted = new DateTime(2024, 04, 21, 0, 0, 0, 0),
                    EngagementEnded = DateTime.MinValue,
                }
            ];
        }

        private static List<ProjectModel> GetValidProjectModels()
        {
            return
            [
                new() {
                    Commentary = "This is Test Project 2",
                    DefCode = "1234",
                    Id = "2",
                    LastUpdated = new DateTime(2024, 04, 21).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Name = "Test Project 2",
                    Phase = "Discovery",
                    StandardsSummary =
                    [
                        new StandardSummaryModel
                        {
                            AggregatedCommentary = "",
                            AggregatedStatus = "",
                            LastUpdated = new DateTime(2024, 04, 21),
                            Professions =
                            [
                                new StandardSummaryProfessionModel
                                {
                                    Commentary = "Profession Update",
                                    LastUpdated = new DateTime(2024, 04, 21),
                                    ProfessionId = "1",
                                    Status = "Status",
                                },
                            ],
                            StandardId = "1",
                        },
                    ],
                    Status = "GREEN",
                    Tags = ["TAG1", "TAG2"],
                    UpdateDate = new DateTime(2024, 04, 21).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                },
                new()
                {
                    Commentary = "This is Test Project 3",
                    DefCode = "5678",
                    Id = "3",
                    LastUpdated = new DateTime(2024, 04, 22).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Name = "Test Project 3",
                    Phase = "Alpha",
                    StandardsSummary =
                    [
                        new() {
                            AggregatedCommentary = "Commentary 3",
                            AggregatedStatus = "AMBER",
                            LastUpdated = new DateTime(2024, 04, 22),
                            Professions =
                            [
                                new StandardSummaryProfessionModel
                                {
                                    Commentary = "Profession Update 3",
                                    LastUpdated = new DateTime(2024, 04, 22),
                                    ProfessionId = "3",
                                    Status = "Status 3",
                                },
                            ],
                            StandardId = "3",
                        },
                    ],
                    Status = "AMBER",
                    Tags = ["TAG3", "TAG4"],
                    UpdateDate = new DateTime(2024, 04, 22).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                },
                new()
                {
                    Commentary = "This is Test Project 4",
                    DefCode = "9101",
                    Id = "4",
                    LastUpdated = new DateTime(2024, 04, 23).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Name = "Test Project 4",
                    Phase = "Beta",
                    StandardsSummary =
                    [
                        new StandardSummaryModel
                        {
                            AggregatedCommentary = "Commentary 4",
                            AggregatedStatus = "RED",
                            LastUpdated = new DateTime(2024, 04, 23),
                            Professions =
                            [
                                new StandardSummaryProfessionModel
                                {
                                    Commentary = "Profession Update 4",
                                    LastUpdated = new DateTime(2024, 04, 23),
                                    ProfessionId = "4",
                                    Status = "Status 4",
                                },
                            ],
                            StandardId = "4",
                        },
                    ],
                    Status = "RED",
                    Tags = ["TAG5", "TAG6"],
                    UpdateDate = new DateTime(2024, 04, 23).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                }
            ];
        }
    }
}
