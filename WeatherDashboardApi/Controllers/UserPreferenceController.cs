// -----------------------------------------------------------------------------------------------------
//  Summary:
//      This controller manages user preference operations related to default weather locations.
//      It exposes API endpoints to set and retrieve a user's default city for weather data.
//      Input validation is performed using FluentValidation to ensure request integrity.
//      The controller interacts with IUserPreferenceService for persistence and business logic.
//
//  Endpoints:
//      POST /api/user-preferences/set-default-location
//          - Validates input and sets the user's default location.
//      GET /api/user-preferences/{userId}
//          - Retrieves the user's saved default location.
//
//  Error Handling:
//      - Returns 400 (Bad Request) for validation or argument errors.
//      - Returns 404 (Not Found) when no location exists for a given user.
//      - Returns 500 (Internal Server Error) for unexpected exceptions.
//
//  Dependencies:
//      - IUserPreferenceService: Handles storage and retrieval of user preferences.
//      - IValidator<UserPreferenceDto>: Validates incoming request payloads.
// -----------------------------------------------------------------------------------------------------
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using WeatherApi.Dto;
using WeatherApi.Services;

[ApiController]
[Route("api/user-preferences")]
public class UserPreferenceController : ControllerBase
{
    private readonly IUserPreferenceService _service;
    private readonly IValidator<UserPreferenceDto> _validator;

    public UserPreferenceController(IUserPreferenceService service, IValidator<UserPreferenceDto> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpPost("set-default-location")]
    public async Task<IActionResult> SetDefaultLocation([FromBody] UserPreferenceDto dto)
    {
        ValidationResult result = await _validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            var problemDetails = new ValidationProblemDetails(result.ToDictionary())
            {
                Title = "Input validation errors occurred",
                Detail = "Invalid input",
                Status = StatusCodes.Status400BadRequest
            };
            return BadRequest(problemDetails);
        }

        try
        {
            await _service.SetDefaultLocationAsync(dto.UserId, dto.City);
            return Ok(new { Message = "Default location set successfully" });
        }
        catch (ArgumentException ex)
        {

            var problemDetails = new ProblemDetails
            {
                Title = "Invalid input",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            };
            return BadRequest(problemDetails);
        }
        catch (Exception ex)
        {
            // Unexpected errors
            var problemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            };
            return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
        }

    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetDefaultLocation(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            var problem = new ProblemDetails
            {
                Title = "UserId is required",
                Detail = "UserId is missing",
                Status = StatusCodes.Status400BadRequest
            };
            return BadRequest(problem);
        }

        try
        {
            var city = await _service.GetDefaultLocationAsync(userId);
            if (city == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = $"No default location found for user '{userId}'",
                    Detail = "City is required",
                    Status = StatusCodes.Status404NotFound
                });
            }
            return Ok(new { City = city });
        }
        catch (Exception ex)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            };
            return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
        }
    }
}
