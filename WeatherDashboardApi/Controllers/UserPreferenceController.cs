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
