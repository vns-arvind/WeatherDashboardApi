using FluentValidation;
using WeatherApi.Dto;

namespace WeatherApi.Validators
{
    public class UserPreferenceValidator : AbstractValidator<UserPreferenceDto>
    {
        public UserPreferenceValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required");
        }
    }
}
