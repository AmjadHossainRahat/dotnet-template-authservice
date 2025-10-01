using System.ComponentModel.DataAnnotations;

namespace AuthService.API.Validations
{
    public class CustomAgeValidationAttribute : ValidationAttribute
    {
        private readonly int _min;
        private readonly int _max;

        public CustomAgeValidationAttribute(int min, int max)
        {
            _min = min;
            _max = max;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is int age)
            {
                if (age < _min || age > _max)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}
