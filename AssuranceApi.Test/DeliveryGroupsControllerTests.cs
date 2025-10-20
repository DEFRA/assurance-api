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
using AssuranceApi.Project.Models;
using AssuranceApi.Data.ChangeHistory;

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
                Outcome = "Outcome 1",
                RoadmapName = "Roadmap 1",
                RoadmapLink = "https://roadmaplink1.com",
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
                Outcome = "Outcome 2",
                RoadmapName = "Roadmap 2",
                RoadmapLink = "https://roadmaplink2.com",
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
                Outcome = "Outcome 3",
                RoadmapName = "Roadmap 3",
                RoadmapLink = "https://roadmaplink3.com",
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
                Outcome = "Outcome 4",
                RoadmapName = "Roadmap 4",
                RoadmapLink = "https://roadmaplink4.com",
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
                Outcome = "Outcome 5",
                RoadmapName = "Roadmap 5",
                RoadmapLink = "https://roadmaplink5.com",
                CreatedAt = new DateTime(2024, 04, 25).ToUniversalTime(),
                UpdatedAt = new DateTime(2024, 04, 25).ToUniversalTime()
            }
        ];

        private static readonly List<ProjectModel> _activeProjects =
        [
            new()
            {
                Commentary = "This is Test Project 1",
                DefCode = "1234",
                DeliveryGroupId = "ID-1",
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
                DeliveryGroupId = "ID-2",
                StandardsSummary =
                [
                    new()
                    {
                        AggregatedCommentary = "",
                        AggregatedStatus = "GREEN",
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

        private static readonly List<History<DeliveryGroupChanges>> _deliveryGroupHistory =
        [
            new()
            {
                Id = "1-a",
                Changes = new DeliveryGroupChanges()
                {
                    Name = new Change<string>()
                    {
                        From = "Before name update 1",
                        To = "After name update 1",
                    },
                    Status = new Change<string>()
                    {
                        From = "Before status update 1",
                        To = "After status update 1",
                    },
                    Lead = new Change<string>()
                    {
                        From = "Before lead update 1",
                        To = "After lead update 1",
                    },
                    Outcome = new Change<string>()
                    {
                        From = "Before outcome update 1",
                        To = "After outcome update 1",
                    },
                    RoadmapName = new Change<string>()
                    {
                        From = "Before roadmap name update 1",
                        To = "After roadmap name update 1",
                    },
                    RoadmapLink = new Change<string>()
                    {
                        From = "Before roadmap link update 1",
                        To = "After roadmap link update 1",
                    }
                },
                ChangedBy = "System",
                IsArchived = false,
                ItemId = "1",
                Timestamp = new DateTime(2024, 04, 21),
            },
            new()
            {
                Id = "1-b",
                Changes = new DeliveryGroupChanges()
                {
                    Name = new Change<string>()
                    {
                        From = "Before name update 2",
                        To = "After name update 2",
                    },
                    Status = new Change<string>()
                    {
                        From = "Before status update 2",
                        To = "After status update 2",
                    },
                    Lead = new Change<string>()
                    {
                        From = "Before lead update 2",
                        To = "After lead update 2",
                    },
                    Outcome = new Change<string>()
                    {
                        From = "Before outcome update 2",
                        To = "After outcome update 2",
                    },
                    RoadmapName = new Change<string>()
                    {
                        From = "Before roadmap name update 2",
                        To = "After roadmap name update 2",
                    },
                    RoadmapLink = new Change<string>()
                    {
                        From = "Before roadmap link update 2",
                        To = "After roadmap link update 2",
                    }
                },
                ChangedBy = "System",
                IsArchived = false,
                ItemId = "1",
                Timestamp = new DateTime(2024, 04, 21),
            },
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
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence, 
                mockDeliveryGroupHistoryPersistence,
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
                null!,
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
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence, 
                mockDeliveryGroupHistoryPersistence,
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
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence,
                mockDeliveryGroupHistoryPersistence,
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
                null!, 
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
        public async Task GetDeliveryGroupProjects_ReturnsOkResult_WithListOfDeliveryGroups()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence,
                mockDeliveryGroupHistoryPersistence,
                _validator,
                _logger
            );

            // Act
            var response = await controller.GetDeliveryGroupProjects("ID-1");

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            response.As<OkObjectResult>().Value.As<List<ProjectModel>>().Count.Should().Be(1);
            response.As<OkObjectResult>().Value.As<List<ProjectModel>>()[0].Should().BeEquivalentTo(_activeProjects[0]);
        }

        [Fact]
        public async Task Create_InvalidDeliveryGroup_ReturnsBadRequestObjectResult()
        {
            // Arrange
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence, 
                mockDeliveryGroupHistoryPersistence,
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
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence, 
                mockDeliveryGroupHistoryPersistence,
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
                null!, 
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
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence, 
                mockDeliveryGroupHistoryPersistence,
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
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence, 
                mockDeliveryGroupHistoryPersistence,
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
                null!, 
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
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence, 
                mockDeliveryGroupHistoryPersistence,
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
                null!, 
                null!,
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
                null!, 
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
                null!, 
                null!,
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
                null!, 
                null!,
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
                null!, 
                null!,
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
                null!, 
                GetDeliveryGroupHistoryPersistenceMock(),
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
                null!, 
                null!,
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
                null!, 
                null!,
                _validator,
                _logger
            );

            // Act
            var response = await controller.Delete("   "); // Whitespace-only ID

            // Assert
            response.Should().BeOfType<NotFoundResult>();
        }
        [Fact]
        public async Task GetHistory_ReturnsOkResult_WithMatchingProjectHistory_WhenAValidIdIsPassed()
        {
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence,
                mockDeliveryGroupHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetHistory("1");

            response
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should()
                .BeEquivalentTo(_deliveryGroupHistory);
        }

        [Fact]
        public async Task GetHistory_ReturnsOkResult__WithEmptyCollection_WhenThereIsNoMatchingProjectHistory()
        {
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence,
                mockDeliveryGroupHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.GetHistory("2");

            response.Should().BeOfType<OkObjectResult>();
            response
                .As<OkObjectResult>()
                .Value.As<IEnumerable<History<DeliveryGroupChanges>>>()
                .Count()
                .Should()
                .Be(0);
        }

        [Fact]
        public async Task GetHistory_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new DeliveryGroupsController(
                null,
                null,
                null,
                _validator,
                _logger
            );
            var response = await controller.GetHistory("1");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteHistory_ReturnsOkResult_WhenThereIsAValidProjectHistory()
        {
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence,
                mockDeliveryGroupHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.DeleteHistory("1", "1");

            response.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task DeleteHistory_ReturnsNotFoundResult_WhenThereIsAnInvalidProjectHistory()
        {
            var mockDeliveryGroupPersistence = GetDeliveryGroupPersistenceMock();
            var mockProjectPersistence = GetProjectPersistenceMock();
            var mockDeliveryGroupHistoryPersistence = GetDeliveryGroupHistoryPersistenceMock();

            var controller = new DeliveryGroupsController(
                mockDeliveryGroupPersistence,
                mockProjectPersistence,
                mockDeliveryGroupHistoryPersistence,
                _validator,
                _logger
            );
            var response = await controller.DeleteHistory("INVALID", "INVALID");

            response.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteHistory_ReturnsObjectResult_With500Result_WhenAnExceptionOccurs()
        {
            var controller = new DeliveryGroupsController(
                null!,
                null!,
                null!,
                _validator,
                _logger
            );
            var response = await controller.DeleteHistory("INVALID", "INVALID");

            response.Should().BeOfType<ObjectResult>();
            response.As<ObjectResult>().StatusCode.Should().Be(500);
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

        private static IHistoryPersistence<DeliveryGroupChanges> GetDeliveryGroupHistoryPersistenceMock()
        {
            var mockDeliveryGroupPersistence = Substitute.For<IHistoryPersistence<DeliveryGroupChanges>>();

            mockDeliveryGroupPersistence.GetHistoryAsync("1").Returns(_deliveryGroupHistory);
            mockDeliveryGroupPersistence.GetHistoryAsync("2").Returns([]);
            mockDeliveryGroupPersistence.ArchiveHistoryEntryAsync("1", "1").Returns(true);

            return mockDeliveryGroupPersistence;
        }

        private static IProjectPersistence GetProjectPersistenceMock()
        {
            var mockProjectPersistence = Substitute.For<IProjectPersistence>();

            mockProjectPersistence.GetAllAsync(Arg.Is<ProjectQueryParameters>(x =>
                x.Tags == null &&
                x.StartDate == null &&
                x.EndDate == null &&
                x.DeliveryGroupId == "ID-1")).Returns([_activeProjects[0]]);

            return mockProjectPersistence;
        }
    }
}
