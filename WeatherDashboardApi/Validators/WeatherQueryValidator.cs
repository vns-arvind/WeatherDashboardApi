using FluentValidation;
using WeatherApi.Dto;

namespace WeatherApi.Validators
{
    public class WeatherQueryValidator : AbstractValidator<WeatherQueryDto>
    {
        public WeatherQueryValidator()
        {
            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("City query parameter is required");
        }
    }
}
