using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherApi.Controllers;
using WeatherApi.Dto;
using WeatherApi.Models;
using WeatherApi.Services;
using FluentValidation;
using FluentValidation.Results;
using Xunit;

public class WeatherControllerTests
{
    private readonly Mock<IWeatherService> _mockService;
    private readonly Mock<IValidator<WeatherQueryDto>> _mockValidator;
    private readonly Mock<ILogger<WeatherController>> _mockLogger;
    private readonly WeatherController _controller;

    public WeatherControllerTests()
    {
        _mockService = new Mock<IWeatherService>();
        _mockValidator = new Mock<IValidator<WeatherQueryDto>>();
        _mockLogger = new Mock<ILogger<WeatherController>>();

        _controller = new WeatherController(
            _mockService.Object,
            _mockValidator.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetByCity_ReturnsOk_WhenServiceReturnsData()
    {
        // Arrange
        var query = new WeatherQueryDto { City = "London" };
        var expectedWeather = new Weather("London", "GB", 15.0, "Cloudy", 80, 4.2);

        _mockValidator
            .Setup(v => v.ValidateAsync(query, default))
            .ReturnsAsync(new ValidationResult());

        _mockService
            .Setup(s => s.GetWeatherByCityAsync(query.City!))
            .ReturnsAsync(expectedWeather);

        // Act
        var result = await _controller.GetByCity(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedWeather = Assert.IsType<Weather>(okResult.Value);
        Assert.Equal(expectedWeather, returnedWeather);
    }

    [Fact]
    public async Task GetByCity_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var query = new WeatherQueryDto { City = "" };

        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("City", "City is required")
        };

        _mockValidator
            .Setup(v => v.ValidateAsync(query, default))
            .ReturnsAsync(new ValidationResult(failures));

        // Act
        var result = await _controller.GetByCity(query);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal("Input validation errors occurred", problemDetails.Title);
        Assert.Equal(400, problemDetails.Status);
        Assert.True(problemDetails.Errors.ContainsKey("City"));
    }

    [Fact]
    public async Task GetByCity_Returns502_WhenProviderFails()
    {
        // Arrange
        var query = new WeatherQueryDto { City = "InvalidCity" };

        _mockValidator
            .Setup(v => v.ValidateAsync(query, default))
            .ReturnsAsync(new ValidationResult());

        _mockService
            .Setup(s => s.GetWeatherByCityAsync(query.City!))
            .ThrowsAsync(new InvalidOperationException("provider error"));

        // Act
        var result = await _controller.GetByCity(query);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("Weather provider error", problem.Title);
        Assert.Equal(502, problem.Status);
    }

    [Fact]
    public async Task GetByCity_Returns500_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var query = new WeatherQueryDto { City = "TestCity" };

        _mockValidator
            .Setup(v => v.ValidateAsync(query, default))
            .ReturnsAsync(new ValidationResult());

        _mockService
            .Setup(s => s.GetWeatherByCityAsync(query.City!))
            .ThrowsAsync(new Exception("Unexpected failure"));

        // Act
        var result = await _controller.GetByCity(query);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal(500, problem.Status);
    }
}
