/*
Example of usages:
using System.ComponentModel.DataAnnotations;

    public class SampleTenantDto
    {
        // Required
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        // String length / min/max
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Description must be between 3 and 50 characters.")]
        public string Description { get; set; } = string.Empty;

        // Range for numeric values
        [Range(1, 1000, ErrorMessage = "MaxUsers must be between 1 and 1000.")]
        public int MaxUsers { get; set; }

        // Email
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string AdminEmail { get; set; } = string.Empty;

        // Regex pattern (e.g., alphanumeric only)
        [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Code must be alphanumeric.")]
        public string Code { get; set; } = string.Empty;

        // Compare fields (e.g., password confirmation)
        [Required]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "ConfirmPassword must match Password.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Custom validation example
        [CustomAgeValidation(18, 100, ErrorMessage = "Age must be between 18 and 100.")]
        public int Age { get; set; }
    }
*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AuthService.API.Models;

namespace AuthService.API.Validations
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var response = ApiResponse<string>.Fail(
                    message: "Validation failed",
                    code: "VALIDATION_ERROR",
                    statusCode: 400
                );
                response.Error!.ValidationErrors = errors;

                context.Result = new BadRequestObjectResult(response);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}