// -----------------------------------------------------------------------------------------------------
//  Summary:
//      This controller provides endpoints to retrieve current weather data based on city names.
//      It validates incoming query parameters, interacts with the IWeatherService to fetch data
//      from weather providers, and handles both expected and unexpected errors gracefully.
//
//  Endpoint:
//      GET /api/weather
//          - Accepts a WeatherQueryDto containing the city name.
//          - Returns current weather information for the requested city.
//
//  Error Handling:
//      - Returns 400 (Bad Request) for validation errors.
//      - Returns 502 (Bad Gateway) for upstream or provider-related issues.
//      - Returns 500 (Internal Server Error) for unexpected exceptions.
//
//  Dependencies:
//      - IWeatherService: Fetches weather data from external or internal sources.
//      - IValidator<WeatherQueryDto>: Validates query parameters before processing.
//      - ILogger<WeatherController>: Logs warnings and errors for observability.
// -----------------------------------------------------------------------------------------------------

using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using WeatherApi.Dto;
using WeatherApi.Models;
using WeatherApi.Services;

namespace WeatherApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly IValidator<WeatherQueryDto> _validator;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(
            IWeatherService weatherService,
            IValidator<WeatherQueryDto> validator,
            ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _validator = validator;
            _logger = logger;
        }

        /// <summary>
        /// Get weather by city name
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetByCity([FromQuery] WeatherQueryDto query)
        {
            ValidationResult validationResult = await _validator.ValidateAsync(query);
            if (!validationResult.IsValid)
            {
                var problemDetails = new ValidationProblemDetails(validationResult.ToDictionary())
                {
                    Title = "Input validation errors occurred",
                    Status = StatusCodes.Status400BadRequest
                };
                return BadRequest(problemDetails);
            }

            try
            {
                var result = await _weatherService.GetWeatherByCityAsync(query.City!);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Weather provider error for city {city}", query.City);
                var problem = new ProblemDetails
                {
                    Title = "Weather provider error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status502BadGateway
                };
                return StatusCode(StatusCodes.Status502BadGateway, problem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching weather for city {city}", query.City);
                var problem = new ProblemDetails
                {
                    Title = "An unexpected error occurred",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                };
                return StatusCode(StatusCodes.Status500InternalServerError, problem);
            }
        }
    }
}
