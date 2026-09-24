using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WayPoint.API.Controllers;
using WayPoint.Application.Common.Interfaces;
using WayPoint.Application.Common.Interfaces.Disruption;
using WayPoint.Application.DTOs.Common;
using WayPoint.Application.Features.AiWorkflows.DTOs;
using WayPoint.Application.Features.DisruptionManagement.DTOs;
using WayPoint.Domain.Enums;
using Xunit;

namespace WayPoint.Tests.Controllers;

public class AiWorkflowControllerTests
{
    private readonly Mock<IAiWorkflowService> _mockAiWorkflowService;
    private readonly Mock<IAiWorkflowQueryService> _mockWorkflowQueryService;
    private readonly AiWorkflowController _controller;

    public AiWorkflowControllerTests()
    {
        _mockAiWorkflowService = new Mock<IAiWorkflowService>();
        _mockWorkflowQueryService = new Mock<IAiWorkflowQueryService>();
        _controller = new AiWorkflowController(
            _mockAiWorkflowService.Object,
            _mockWorkflowQueryService.Object
        );
    }

    [Fact]
    public async Task CreateWorkflow_Success_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateAiWorkflowDto("Test Objective");
        var expectedId = Guid.NewGuid();
        var resultDto = new AiWorkflowResponseDto(expectedId, "Test Objective", AiWorkflowStatus.Running, DateTime.UtcNow, null, new List<AiWorkflowStepResponseDto>());

        _mockAiWorkflowService
            .Setup(x => x.CreateWorkflowAsync(dto))
            .ReturnsAsync(resultDto);

        // Act
        var response = await _controller.CreateWorkflow(dto);

        // Assert
        var createdResult = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AiWorkflowController.GetWorkflow));
        createdResult.RouteValues?["id"].Should().Be(expectedId);
        createdResult.Value.Should().BeEquivalentTo(resultDto);
    }

    [Fact]
    public async Task CreateWorkflow_Failure_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateAiWorkflowDto("Test Objective");
        _mockAiWorkflowService
            .Setup(x => x.CreateWorkflowAsync(dto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var response = await _controller.CreateWorkflow(dto);

        // Assert
        var badRequestResult = response.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Be("Database error");
    }

    [Fact]
    public async Task GetWorkflow_Success_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultDto = new AiWorkflowResponseDto(id, "Test Objective", AiWorkflowStatus.Running, DateTime.UtcNow, null, new List<AiWorkflowStepResponseDto>());

        _mockAiWorkflowService
            .Setup(x => x.GetWorkflowAsync(id))
            .ReturnsAsync(resultDto);

        // Act
        var response = await _controller.GetWorkflow(id);

        // Assert
        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(resultDto);
    }

    [Fact]
    public async Task GetWorkflow_NotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mockAiWorkflowService
            .Setup(x => x.GetWorkflowAsync(id))
            .ThrowsAsync(new KeyNotFoundException("Workflow not found"));

        // Act
        var response = await _controller.GetWorkflow(id);

        // Assert
        var notFoundResult = response.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Be("Workflow not found");
    }

    [Fact]
    public async Task UpdateWorkflowStatus_Success_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateAiWorkflowStatusDto(AiWorkflowStatus.Completed);
        var resultDto = new AiWorkflowResponseDto(id, "Test Objective", AiWorkflowStatus.Completed, DateTime.UtcNow, DateTime.UtcNow, new List<AiWorkflowStepResponseDto>());

        _mockAiWorkflowService
            .Setup(x => x.UpdateWorkflowStatusAsync(id, dto))
            .ReturnsAsync(resultDto);

        // Act
        var response = await _controller.UpdateWorkflowStatus(id, dto);

        // Assert
        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(resultDto);
    }

    [Fact]
    public async Task UpdateWorkflowStatus_NotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateAiWorkflowStatusDto(AiWorkflowStatus.Completed);

        _mockAiWorkflowService
            .Setup(x => x.UpdateWorkflowStatusAsync(id, dto))
            .ThrowsAsync(new KeyNotFoundException("Workflow not found"));

        // Act
        var response = await _controller.UpdateWorkflowStatus(id, dto);

        // Assert
        var notFoundResult = response.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task AddStep_Success_ReturnsCreated()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new CreateAiWorkflowStepDto("Test Agent", 1, "Test Prompt");
        var resultId = Guid.NewGuid();
        var resultDto = new AiWorkflowStepResponseDto(resultId, id, "Test Agent", 1, "Test Prompt", DateTime.UtcNow, new List<AiToolCallResponseDto>(), new List<AiValidationResultResponseDto>());

        _mockAiWorkflowService
            .Setup(x => x.AddStepAsync(id, dto))
            .ReturnsAsync(resultDto);

        // Act
        var response = await _controller.AddStep(id, dto);

        // Assert
        var createdResult = response.Should().BeOfType<CreatedResult>().Subject;
        createdResult.Location.Should().Be($"api/v1/ai/workflows/{id}/steps/{resultId}");
        createdResult.Value.Should().BeEquivalentTo(resultDto);
    }

    [Fact]
    public async Task AddStep_NotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new CreateAiWorkflowStepDto("Test Agent", 1, "Test Prompt");

        _mockAiWorkflowService
            .Setup(x => x.AddStepAsync(id, dto))
            .ThrowsAsync(new KeyNotFoundException("Workflow not found"));

        // Act
        var response = await _controller.AddStep(id, dto);

        // Assert
        var notFoundResult = response.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task AddToolCall_Success_ReturnsCreated()
    {
        // Arrange
        var stepId = Guid.NewGuid();
        var dto = new CreateAiToolCallDto("SearchRoutes", "{}", "{}", 100);
        var resultId = Guid.NewGuid();
        var resultDto = new AiToolCallResponseDto(resultId, stepId, "SearchRoutes", "{}", "{}", 100, DateTime.UtcNow);

        _mockAiWorkflowService
            .Setup(x => x.AddToolCallAsync(stepId, dto))
            .ReturnsAsync(resultDto);

        // Act
        var response = await _controller.AddToolCall(stepId, dto);

        // Assert
        var createdResult = response.Should().BeOfType<CreatedResult>().Subject;
        createdResult.Location.Should().Be($"api/v1/ai/workflows/steps/{stepId}/tool-calls/{resultId}");
        createdResult.Value.Should().BeEquivalentTo(resultDto);
    }

    [Fact]
    public async Task AddToolCall_NotFound_ReturnsNotFound()
    {
        // Arrange
        var stepId = Guid.NewGuid();
        var dto = new CreateAiToolCallDto("SearchRoutes", "{}", "{}", 100);

        _mockAiWorkflowService
            .Setup(x => x.AddToolCallAsync(stepId, dto))
            .ThrowsAsync(new KeyNotFoundException("Step not found"));

        // Act
        var response = await _controller.AddToolCall(stepId, dto);

        // Assert
        var notFoundResult = response.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task AddValidationResult_Success_ReturnsCreated()
    {
        // Arrange
        var stepId = Guid.NewGuid();
        var dto = new CreateAiValidationResultDto("Rule1", true, null);
        var resultId = Guid.NewGuid();
        var resultDto = new AiValidationResultResponseDto(resultId, stepId, "Rule1", true, null);

        _mockAiWorkflowService
            .Setup(x => x.AddValidationResultAsync(stepId, dto))
            .ReturnsAsync(resultDto);

        // Act
        var response = await _controller.AddValidationResult(stepId, dto);

        // Assert
        var createdResult = response.Should().BeOfType<CreatedResult>().Subject;
        createdResult.Location.Should().Be($"api/v1/ai/workflows/steps/{stepId}/validations/{resultId}");
        createdResult.Value.Should().BeEquivalentTo(resultDto);
    }

    [Fact]
    public async Task AddValidationResult_NotFound_ReturnsNotFound()
    {
        // Arrange
        var stepId = Guid.NewGuid();
        var dto = new CreateAiValidationResultDto("Rule1", true, null);

        _mockAiWorkflowService
            .Setup(x => x.AddValidationResultAsync(stepId, dto))
            .ThrowsAsync(new KeyNotFoundException("Step not found"));

        // Act
        var response = await _controller.AddValidationResult(stepId, dto);

        // Assert
        var notFoundResult = response.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetWorkflows_Success_ReturnsOk()
    {
        // Arrange
        var filter = new WorkflowFilterParams();
        var pagedResponse = new PaginatedResponseDto<AiWorkflowDto>(new List<AiWorkflowDto>(), 0, 1, 10);
        
        _mockWorkflowQueryService
            .Setup(x => x.GetWorkflowsAsync(filter))
            .ReturnsAsync(pagedResponse);

        // Act
        var response = await _controller.GetWorkflows(filter);

        // Assert
        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pagedResponse);
    }
}
