using Microsoft.AspNetCore.Mvc;
using Moq;
using WeatherApi.Services;
using FluentValidation;
using FluentValidation.Results;
using Xunit;
using WeatherApi.Dto;
using System.Text.Json;

public class UserPreferenceControllerTests
{
    private readonly Mock<IUserPreferenceService> _mockService;
    private readonly Mock<IValidator<UserPreferenceDto>> _mockValidator;
    private readonly UserPreferenceController _controller;

    public UserPreferenceControllerTests()
    {
        _mockService = new Mock<IUserPreferenceService>();
        _mockValidator = new Mock<IValidator<UserPreferenceDto>>();
        _controller = new UserPreferenceController(_mockService.Object, _mockValidator.Object);
    }

    [Fact]
    public async Task SetDefaultLocation_ValidInput_ReturnsOk()
    {
        // Arrange
        var dto = new UserPreferenceDto { UserId = "user1", City = "Paris" };
        _mockValidator.Setup(v => v.ValidateAsync(dto, default))
                      .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _controller.SetDefaultLocation(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Contains("successfully", okResult.Value!.ToString());
        _mockService.Verify(s => s.SetDefaultLocationAsync(dto.UserId, dto.City), Times.Once);
    }

    [Fact]
    public async Task SetDefaultLocation_InvalidInput_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UserPreferenceDto { UserId = "", City = "" };

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("UserId", "User ID is required"),
            new ValidationFailure("City", "City is required")
        };

        _mockValidator.Setup(v => v.ValidateAsync(dto, default))
                      .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.SetDefaultLocation(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var details = Assert.IsType<ValidationProblemDetails>(badRequest.Value);

        Assert.Equal(400, details.Status);
        Assert.Equal("Input validation errors occurred", details.Title);
        Assert.Contains("UserId", details.Errors.Keys);
        Assert.Contains("City", details.Errors.Keys);
        Assert.Contains("User ID is required", details.Errors["UserId"]);
        Assert.Contains("City is required", details.Errors["City"]);

        _mockService.Verify(s => s.SetDefaultLocationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetDefaultLocation_Found_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(s => s.GetDefaultLocationAsync("user1")).ReturnsAsync("Paris");

        // Act
        var result = await _controller.GetDefaultLocation("user1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;

        var cityProp = value.GetType().GetProperty("City")!;
        var cityValue = cityProp.GetValue(value, null);

        Assert.Equal("Paris", cityValue);
    }

    [Fact]
    public async Task GetDefaultLocation_NotFound_Returns404()
    {
        // Arrange
        _mockService.Setup(s => s.GetDefaultLocationAsync("unknown")).ReturnsAsync((string?)null);

        // Act
        var result = await _controller.GetDefaultLocation("unknown");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(404, details.Status);
    }
}
