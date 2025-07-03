using AssuranceApi.Controllers;
using AssuranceApi.Profession.Models;
using AssuranceApi.Profession.Services;
using AssuranceApi.Project.Handlers;
using AssuranceApi.Project.Helpers;
using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using AssuranceApi.Project.Validators;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.ServiceStandard.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog;
using Serilog.Extensions.Logging;
using System.Security.Cryptography.Xml;
using Xunit.Abstractions;

namespace AssuranceApi.Test
{
    public class ProjectsControllerTests
    {
        private readonly ProjectValidator _validator;
        private readonly ILogger<ProjectsController> _logger;

        private static readonly List<ProjectModel> _activeProjects =
        [
            new()
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
            },
            new()
            {
                Commentary = "This is Test Project 2",
                DefCode = "2345",
                Id = "2",
                LastUpdated = new DateTime(2024, 04, 22).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                Name = "Test Project 2",
                Phase = "Alpha",
                StandardsSummary =
                [
                    new()
                    {
                        AggregatedCommentary = "",
                        AggregatedStatus = "",
                        LastUpdated = new DateTime(2024, 04, 22),
                        Professions =
                        [
                            new()
                            {
                                Commentary = "Profession Update",
                                LastUpdated = new DateTime(2024, 04, 22),
                                ProfessionId = "1",
                                Status = "Status",
                            },
                        ],
                        StandardId = "2",
                    },
                ],
                Status = "AMBER",
                Tags = ["TAG2", "TAG3"],
                UpdateDate = new DateTime(2024, 04, 22).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            },
        ];

        private static readonly List<ProjectHistory> _projectHistory =
        [
            new()
            {
                Id = "1-a",
                Changes = new Changes()
                {
                    Commentary = new CommentaryChange()
                    {
                        From = "Before commentary update 1",
                        To = "After commentary update 1",
                    },
                    Name = new AssuranceApi.Project.Models.NameChange()
                    {
                        From = "Before name update 1",
                        To = "After name update 1",
                    },
                    Phase = new PhaseChange()
                    {
                        From = "Before phase update 1",
                        To = "After phase update 1",
                    },
                    Status = new StatusChange() { From = "Discovery", To = "Discovery" },
                    Tags = new TagsChange() { From = ["TAG2", "TAG3"], To = ["TAG2", "TAG4"] },
                },
                ChangedBy = "System",
                IsArchived = false,
                ProjectId = "1",
                Timestamp = new DateTime(2024, 04, 21),
            },
            new()
            {
                Id = "1-b",
                Changes = new Changes()
                {
                    Commentary = new CommentaryChange()
                    {
                        From = "Before commentary update 2",
                        To = "After commentary update 2",
                    },
                    Name = new AssuranceApi.Project.Models.NameChange()
                    {
                        From = "Before name update 2",
                        To = "After name update 2",
                    },
                    Phase = new PhaseChange()
                    {
                        From = "Before phase update 2",
                        To = "After phase update 2",
                    },
                    Status = new StatusChange() { From = "Discovery", To = "Alpha" },
                    Tags = new TagsChange()
                    {
                        From = ["TAG1", "TAG2"],
                        To = ["TAG1", "TAG2", "TAG4"],
                    },
                },
                ChangedBy = "",
                IsArchived = false,
                ProjectId = "1",
                Timestamp = new DateTime(2024, 04, 21),
            },
        ];

        private static readonly ProjectStandards _projectStandards = new()
            {
                ChangedBy = "System",
                Commentary = "Commentary 1",
                Id = "1",
                LastUpdated = new DateTime(2024, 04, 21),
                ProfessionId = "1",
                ProjectId = "1",
                StandardId = "1",
                Status = "GREEN"
            };



        private static readonly ServiceStandardModel _serviceStandardModel = new()
        {
            CreatedAt = new DateTime(2024, 04, 21),
            DeletedAt = null,
            DeletedBy = null,
            Description = "Service Standard 1",
            Id = "1",
            IsActive = true,
            Name = "1",
            Number = 1,
            UpdatedAt = new DateTime(2024, 04, 21)
        };

        private static readonly ProfessionModel _professionModel = new()
        {
            CreatedAt = new DateTime(2024, 04, 21),
            DeletedAt = null,
            DeletedBy = null,
            Description = "Profession 1",
            Id = "1",
            IsActive = true,
            Name = "1",
            UpdatedAt = new DateTime(2024, 04, 21)
        };

        private static readonly List<ProjectStandardsHistory> _projectStandardsHistory =
        [
            new()
            {
                Archived = false,
                ChangedBy = "SYSTEM",
                Changes = new AssessmentChanges() {
                    Commentary = new CommentaryChange() {
                        From = "From Commentary",
                        To = "To Commentary"
                    },
                    Status = new StatusChange() {
                        From = "GREEN",
                        To = "RED"
                    }
                },
                Id = "1",
                ProfessionId = "1",
                ProjectId = "1",
                StandardId = "1",
                Timestamp = new DateTime(2024, 04, 21)
            },
        ];

        public ProjectsControllerTests(ITestOutputHelper output)
        {
            _validator = new ProjectValidator();
            Log.Logger = new LoggerConfiguration()
                .WriteTo.TestOutput(output)
                .MinimumLevel.Verbose()
                .CreateLogger();

            _logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<ProjectsController>();
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfAllProjects_WhenIncludeInactiveIsFalse()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.GetAll(string.Empty);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_activeProjects);
        }

        [Fact]
        public async Task GetAllWithTag_ReturnsOkResult_WithListOfTaggedProjects_WhenTagIsSpecified()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.GetAll("TAG3");

            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.As<List<ProjectModel>>().Count.Should().Be(1);
        }

        [Fact]
        public async Task GetAll_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProjectsController(null, null, null, _validator, _logger);
            var response = await controller.GetAll(string.Empty);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithMatchingProjects_WhenAValidIdIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.GetById("1");

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_activeProjects[0]);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenThereIsNoMatchingProject()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.GetById("2");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetById_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProjectsController(null, null, null, _validator, _logger);
            var response = await controller.GetById("1");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetHistory_ReturnsOkResult_WithMatchingProjectHistory_WhenAValidIdIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.GetHistory("1");

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_projectHistory);
        }

        [Fact]
        public async Task GetHistory_ReturnsOkResult__WithEmptyCollection_WhenThereIsNoMatchingProjectHistory()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.GetHistory("2");

            response.Should().BeOfType<OkObjectResult>();
            response
                .As<OkObjectResult>()
                .Value.As<IEnumerable<ProjectHistory>>()
                .Count<ProjectHistory>()
                .Should()
                .Be(0);
        }

        [Fact]
        public async Task GetHistory_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProjectsController(null, null, null, _validator, _logger);
            var response = await controller.GetHistory("1");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteHistory_ReturnsOkResult_WhenThereIsAValidProjectHistory()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.DeleteHistory("1", "1");

            response.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task DeleteHistory_ReturnsNotFoundResult_WhenThereIsAnInvalidProjectHistory()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.DeleteHistory("INVALID", "INVALID");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteHistory_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProjectsController(null, null, null, _validator, _logger);
            var response = await controller.DeleteHistory("INVALID", "INVALID");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Delete_ReturnsOkResult_WhenAValidIdIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Delete("1");

            response.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Delete_ReturnsNotFoundResult_WhenAnInvalidIdIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Delete("2");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Delete_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProjectsController(null, null, null, _validator, _logger);
            var response = await controller.Delete("1");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetTagsSummary_ReturnsOkResult_WithMatchingListOfTags_WhenAValidIdIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.GetTagsSummary();

            // NEED TO LOOK AT THIS
            // Loks like it is returning a dictionary where the values are dictionaries
            // The logic works, just the response type weems weird

            response.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetTagsSummary_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new ProjectsController(null, null, null, _validator, _logger);
            var response = await controller.GetTagsSummary();

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Create_ReturnsCreatedResult_WithSuccessMessage_WhenAValidProjectIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Create(_activeProjects[0]);

            response
                .Should()
                .BeOfType<CreatedResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_activeProjects[0]);
        }

        [Fact]
        public async Task Create_ReturnsCreatedResult_WhenAnEmptyNameIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var validModel = GetNewInstanceOfProjectModelToDiscardChanges();
            validModel.Name = string.Empty; // Empty name is now allowed

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Create(validModel);

            // Should succeed because empty names are now allowed for partial updates
            response
                .Should()
                .BeOfType<CreatedResult>();
        }

        [Fact]
        public async Task Create_ReturnsBadRequestObjectResult_WithErrorMessage_WhenAnInvalidProjectIsPassedWithInvalidStatus()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var invalidModel = GetNewInstanceOfProjectModelToDiscardChanges();
            invalidModel.Status = "Invalid";

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Create(invalidModel);

            var errorMessage = "Validation failed for project status 'Invalid'";

            response
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(errorMessage);
        }

        [Fact]
        public async Task Create_ReturnsBadRequestObjectResult_WithErrorMessage_WhenAnInvalidProjectIsPassedWithInvalidPhase()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var invalidModel = GetNewInstanceOfProjectModelToDiscardChanges();
            invalidModel.Phase = "Invalid"; // Invalid phase value

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Create(invalidModel);

            var errorMessage =
                "Validation errors occurred whilst creating the project:\n  The specified condition was not met for 'Phase'.";

            response
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(errorMessage);
        }

        [Fact]
        public async Task Create_ReturnsBadRequestObjectResult_WithErrorMessage_WhenAnInvalidProjectIsPassedWithNullCommentary()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var invalidModel = GetNewInstanceOfProjectModelToDiscardChanges();
            invalidModel.Commentary = null; // null is not allowed, but empty string is

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Create(invalidModel);

            var errorMessage =
                "Validation errors occurred whilst creating the project:\n  'Commentary' must not be empty.";

            response
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(errorMessage);
        }

        [Fact]
        public async Task Create_ReturnsCreatedResult_WhenAnEmptyPhaseIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var validModel = GetNewInstanceOfProjectModelToDiscardChanges();
            validModel.Phase = string.Empty; // Empty phase is now allowed

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Create(validModel);

            // Should succeed because empty phases are now allowed for partial updates
            response
                .Should()
                .BeOfType<CreatedResult>();
        }

        [Fact]
        public async Task Create_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingProject()
        {
            var controller = new ProjectsController(null, null, null, _validator, _logger);
            var response = await controller.Create(_activeProjects[0]);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Create_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingProjectHistory()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                null,
                null, _validator,
                _logger
            );
            var response = await controller.Create(_activeProjects[0]);

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Update_ReturnsOkObjectResult_WithSuccessMessage_WhenAValidProjectIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Update(_activeProjects[0].Id, _activeProjects[0]);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_activeProjects[0]);
        }

        [Fact]
        public async Task Update_ReturnsOkObjectResult_WithSuccessMessage_WhenAValidProjectIsPassedThatHasChangedName()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var changedModel = GetNewInstanceOfProjectModelToDiscardChanges();
            changedModel.Name = "Changed";

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Update(_activeProjects[0].Id, changedModel);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(changedModel);
        }

        [Fact]
        public async Task Update_ReturnsOkObjectResult_WithSuccessMessage_WhenAValidProjectIsPassedThatHasChangedPhase()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var changedModel = GetNewInstanceOfProjectModelToDiscardChanges();
            changedModel.Phase = "Private Beta";

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Update(_activeProjects[0].Id, changedModel);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(changedModel);
        }

        [Fact]
        public async Task Update_ReturnsOkObjectResult_WithSuccessMessage_WhenAValidProjectIsPassedThatHasChangedStatus()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var changedModel = GetNewInstanceOfProjectModelToDiscardChanges();
            changedModel.Status = "RED";

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Update(_activeProjects[0].Id, changedModel);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(changedModel);
        }

        [Fact]
        public async Task Update_ReturnsOkObjectResult_WithSuccessMessage_WhenAValidProjectIsPassedThatHasChangedCommentary()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var changedModel = GetNewInstanceOfProjectModelToDiscardChanges();
            changedModel.Commentary = "Changed!!!";

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Update(_activeProjects[0].Id, changedModel);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(changedModel);
        }

        [Fact]
        public async Task Update_ReturnsOkObjectResult_WithSuccessMessage_WhenAValidProjectIsPassedWithInvalidUpdateDate()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var invalidModel = GetNewInstanceOfProjectModelToDiscardChanges();
            invalidModel.UpdateDate = string.Empty;

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Update(invalidModel.Id, invalidModel);

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(invalidModel);
        }

        [Fact]
        public async Task Update_ReturnsNotFoundResult_WithErrorMessage_WhenAValidProjectIsPassedWithAnInvalidId()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                null, _validator,
                _logger
            );
            var response = await controller.Update(
                "99",
                GetNewInstanceOfProjectModelToDiscardChanges()
            );

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Update_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingProject()
        {
            var controller = new ProjectsController(null, null, null, _validator, _logger);
            var response = await controller.Update(
                "3",
                GetNewInstanceOfProjectModelToDiscardChanges()
            );

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Update_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingProjectHistory()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                null,
                null, _validator,
                _logger
            );
            var response = await controller.Update(
                "3",
                GetNewInstanceOfProjectModelToDiscardChanges()
            );

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetProjectStandardProfessionAssessment_ReturnsOkResult_WithMatchingProjectStandards_WhenValidIdsArePassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence, 
                _validator,
                _logger
            );
            var response = await controller.GetProjectStandardProfessionAssessment("1", "1", "1");

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_projectStandards);
        }

        [Fact]
        public async Task GetProjectStandardProfessionAssessment_ReturnsNotFoundResult_WithMatchingProjectStandards_WhenValidIdsArePassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetProjectStandardProfessionAssessment("INVALID", "INVALID", "INVALID");

            response
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetProjectStandardProfessionAssessment_ReturnsObjectResult_With500Result_WhenAnExceptionOccursPersistingProjectHistory()
        {
            var controller = new ProjectsController(
                null,
                null,
                null, 
                _validator,
                _logger
            );
            var response = await controller.GetProjectStandardProfessionAssessment("INVALID", "INVALID", "INVALID");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreateProjectStandardProfessionAssessment_ReturnsOkResult_WhenValidIdsArePassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectStandardHistoryPersistence = GetProjectStandardHistoryPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockProfessionPersistence = GetProfessionPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );
            var response = await controller.CreateProjectStandardProfessionAssessment(
                "1",
                "1",
                "1",
                _projectStandards,
                new CreateAssessmentHandler(
                    mockProjectStandardsPersistence,
                    mockProjectStandardHistoryPersistence,
                    mockProjectPersistence,
                    mockServiceStandardPersistence,
                    mockProfessionPersistence,
                    new SerilogLoggerFactory(Log.Logger).CreateLogger<CreateAssessmentHandler>()
                ),
                new StandardsSummaryHelper(
                    mockProjectPersistence,
                    mockProjectStandardsPersistence                   
                    )
                );

            response
                .Should()
                .BeOfType<OkResult>();
        }

        [Fact]
        public async Task CreateProjectStandardProfessionAssessment_ReturnsBadRequestObjectResult_WhenInvalidProjectIdIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectStandardHistoryPersistence = GetProjectStandardHistoryPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockProfessionPersistence = GetProfessionPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );
            var response = await controller.CreateProjectStandardProfessionAssessment(
                "2",
                "1",
                "1",
                _projectStandards,
                new CreateAssessmentHandler(
                    mockProjectStandardsPersistence,
                    mockProjectStandardHistoryPersistence,
                    mockProjectPersistence,
                    mockServiceStandardPersistence,
                    mockProfessionPersistence,
                    new SerilogLoggerFactory(Log.Logger).CreateLogger<CreateAssessmentHandler>()
                ),
                new StandardsSummaryHelper(
                    mockProjectPersistence,
                    mockProjectStandardsPersistence
                    )
                );

            response
                .Should()
                .BeOfType<BadRequestObjectResult>();
            response.As<BadRequestObjectResult>().StatusCode.Should().Be(400);
            response.As<BadRequestObjectResult>().Value.Should().Be("Referenced project does not exist"); 
        }

        [Fact]
        public async Task CreateProjectStandardProfessionAssessment_ReturnsObjectResult_With500Result_WhenInvalidProjectStandardPersistenceIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectStandardHistoryPersistence = GetProjectStandardHistoryPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockProfessionPersistence = GetProfessionPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );

            var response = await controller.CreateProjectStandardProfessionAssessment(
                "1",
                "1",
                "1",
                _projectStandards,
                new CreateAssessmentHandler(
                    mockProjectStandardsPersistence,
                    mockProjectStandardHistoryPersistence,
                    mockProjectPersistence,
                    null,
                    mockProfessionPersistence,
                    new SerilogLoggerFactory(Log.Logger).CreateLogger<CreateAssessmentHandler>()
                ),
                new StandardsSummaryHelper(
                    mockProjectPersistence,
                    mockProjectStandardsPersistence
                    )
                );

            response
                .Should()
                .BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreateProjectStandardProfessionAssessment_ReturnsObjectResult_With500Result_WhenInvalidCreateAssessmentHandlerIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectStandardHistoryPersistence = GetProjectStandardHistoryPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();
            var mockServiceStandardPersistence = GetServiceStandardPersistenceMock();
            var mockProfessionPersistence = GetProfessionPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );

            var response = await controller.CreateProjectStandardProfessionAssessment(
                "1",
                "1",
                "1",
                _projectStandards,
                null,
                new StandardsSummaryHelper(
                    mockProjectPersistence,
                    mockProjectStandardsPersistence
                    )
                );

            response
                .Should()
                .BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetProjectStandardProfessionHistory_ReturnsNotFoundResult_WithValidHistory_WhenAnInvalidProjectIdIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectStandardHistoryPersistence = GetProjectStandardHistoryPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectStandardProfessionHistory(
                "2",
                "1",
                "1",
                mockProjectStandardHistoryPersistence);

            response
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetProjectStandardProfessionHistory_ReturnsOkObjectResult_WithValidHistory_WhenValidIdsArePassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectStandardHistoryPersistence = GetProjectStandardHistoryPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectStandardProfessionHistory(
                "1",
                "1",
                "1",
                mockProjectStandardHistoryPersistence);

            response
                .Should()
                .BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.Should().Be(_projectStandardsHistory);
        }

        [Fact]
        public async Task GetProjectStandardProfessionHistory_ReturnsObjectResult_With500Result_WhenAnInvalidProjectStandardHistoryPersistenceIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );

            var response = await controller.GetProjectStandardProfessionHistory(
                "1",
                "1",
                "1",
                null);

            response
                .Should()
                .BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteProjectStandardProfessionHistory_ReturnsOkResult_WhenValidIdsArePassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectStandardHistoryPersistence = GetProjectStandardHistoryPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );

            var response = await controller.DeleteProjectStandardProfessionHistory(
                "1",
                "1",
                "1",
                "1",
                mockProjectStandardHistoryPersistence,
                mockProjectStandardsPersistence,
                mockProjectPersistence
                );

            response
                .Should()
                .BeOfType<OkResult>();
        }

        [Fact]
        public async Task DeleteProjectStandardProfessionHistory_ReturnsNotFoundResult_WhenAnInvalidHistoryIdIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectStandardHistoryPersistence = GetProjectStandardHistoryPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );

            var response = await controller.DeleteProjectStandardProfessionHistory(
                "1",
                "1",
                "1",
                "2",
                mockProjectStandardHistoryPersistence,
                mockProjectStandardsPersistence,
                mockProjectPersistence
                );

            response
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteProjectStandardProfessionHistory_ReturnsObjectResult_With500Result_WhenAnInvalidProjectStandardsHistoryPersistenceIsPassed()
        {
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockProjectHistoryPersistence = GetProjectHistoryPersistenceMock();
            var mockProjectStandardsPersistence = GetProjectStandardsPersistenceMock();

            var controller = new ProjectsController(
                mockProjectPersistence,
                mockProjectHistoryPersistence,
                mockProjectStandardsPersistence,
                _validator,
                _logger
            );

            var response = await controller.DeleteProjectStandardProfessionHistory(
                "1",
                "1",
                "1",
                "2",
                null,
                mockProjectStandardsPersistence,
                mockProjectPersistence
                );

            response
                .Should()
                .BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        private static IProjectStandardsHistoryPersistence GetProjectStandardHistoryPersistenceMock()
        {
            var mockProjectStandardsHistoryPersistence = Substitute.For<IProjectStandardsHistoryPersistence>();

            mockProjectStandardsHistoryPersistence.GetHistoryAsync("1", "1", "1").Returns(_projectStandardsHistory);
            mockProjectStandardsHistoryPersistence.ArchiveAsync("1", "1", "1", "1").Returns(true);

            return mockProjectStandardsHistoryPersistence;
        }

        private static IServiceStandardPersistence GetServiceStandardPersistenceMock()
        {
            var mockServiceStandardPersistence = Substitute.For<IServiceStandardPersistence>();
            
            mockServiceStandardPersistence.GetActiveByIdAsync("1").Returns(_serviceStandardModel);

            return mockServiceStandardPersistence;
        }

        private static IProfessionPersistence GetProfessionPersistenceMock()
        {
            var mockProfessionPersistence = Substitute.For<IProfessionPersistence>();

            mockProfessionPersistence.GetActiveByIdAsync("1").Returns(_professionModel);

            return mockProfessionPersistence;
        }

        private static IProjectPersistence GetProjectPersistenceMock()
        {
            var mockProjectPersistence = Substitute.For<IProjectPersistence>();

            mockProjectPersistence.GetAllAsync().Returns(_activeProjects);
            mockProjectPersistence.GetAllAsync(string.Empty).Returns(_activeProjects);
            mockProjectPersistence.GetAllAsync("TAG3").Returns([_activeProjects[1]]);
            mockProjectPersistence.GetByIdAsync("1").Returns(_activeProjects[0]);
            mockProjectPersistence
                .GetByIdAsync("3")
                .Returns(GetNewInstanceOfProjectModelToDiscardChanges());
            mockProjectPersistence.DeleteAsync("1").Returns(true);
            mockProjectPersistence.CreateAsync(Arg.Any<ProjectModel>()).Returns(true);
            mockProjectPersistence
                .UpdateAsync(Arg.Any<string>(), Arg.Any<ProjectModel>())
                .Returns(true);

            return mockProjectPersistence;
        }

        private static IProjectHistoryPersistence GetProjectHistoryPersistenceMock()
        {
            var mockProjectHistoryPersistence = Substitute.For<IProjectHistoryPersistence>();

            mockProjectHistoryPersistence.GetHistoryAsync("1").Returns(_projectHistory);
            mockProjectHistoryPersistence.GetHistoryAsync("2").Returns([]);
            mockProjectHistoryPersistence.ArchiveHistoryEntryAsync("1", "1").Returns(true);

            return mockProjectHistoryPersistence;
        }

        private static IProjectStandardsPersistence GetProjectStandardsPersistenceMock()
        {
            var mockProjectStandardsPersistence = Substitute.For<IProjectStandardsPersistence>();

            mockProjectStandardsPersistence.GetAsync("1", "1", "1").Returns(_projectStandards);
            mockProjectStandardsPersistence.GetByProjectAsync("1").Returns([_projectStandards]);

            return mockProjectStandardsPersistence;
        }

        private static ProjectModel GetNewInstanceOfProjectModelToDiscardChanges()
        {
            return new ProjectModel()
            {
                Commentary = _activeProjects[0].Commentary,
                DefCode = _activeProjects[0].DefCode,
                Id = "3",
                LastUpdated = _activeProjects[0].LastUpdated,
                Name = _activeProjects[0].Name,
                Phase = _activeProjects[0].Phase,
                StandardsSummary =
                [
                    new()
                    {
                        AggregatedCommentary = _activeProjects[0]
                            .StandardsSummary[0]
                            .AggregatedCommentary,
                        AggregatedStatus = _activeProjects[0].StandardsSummary[0].AggregatedStatus,
                        LastUpdated = _activeProjects[0].StandardsSummary[0].LastUpdated,
                        Professions =
                        [
                            new()
                            {
                                Commentary = _activeProjects[0]
                                    .StandardsSummary[0]
                                    .Professions[0]
                                    .Commentary,
                                LastUpdated = _activeProjects[0]
                                    .StandardsSummary[0]
                                    .Professions[0]
                                    .LastUpdated,
                                ProfessionId = _activeProjects[0]
                                    .StandardsSummary[0]
                                    .Professions[0]
                                    .ProfessionId,
                                Status = _activeProjects[0]
                                    .StandardsSummary[0]
                                    .Professions[0]
                                    .Status,
                            },
                        ],
                        StandardId = _activeProjects[0].StandardsSummary[0].StandardId,
                    },
                ],
                Status = _activeProjects[0].Status,
                Tags = new List<string>(),
                UpdateDate = _activeProjects[0].UpdateDate,
            };
        }
    }
}
