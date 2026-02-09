using System.ComponentModel.DataAnnotations;

namespace MediaAssetManager.API.Validation
{
    /// <summary>
    /// Validation attribute to ensure expand parameter values are from an allowed set.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AllowedExpandValuesAttribute : ValidationAttribute
    {
        private readonly HashSet<string> _allowedValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowedExpandValuesAttribute"/> class.
        /// </summary>
        /// <param name="allowedValues">Array of allowed expand values (case-insensitive).</param>
        public AllowedExpandValuesAttribute(params string[] allowedValues)
        {
            _allowedValues = new HashSet<string>(allowedValues, StringComparer.OrdinalIgnoreCase);
            ErrorMessage = $"Invalid expand value(s). Allowed values are: {string.Join(", ", allowedValues)}";
        }

        /// <inheritdoc/>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success; // Null is valid (optional parameter)

            if (value is not string[] expandArray)
                return new ValidationResult("Expand parameter must be a string array.");

            var invalidValues = expandArray
                .Where(v => !_allowedValues.Contains(v))
                .ToList();

            if (invalidValues.Any())
            {
                return new ValidationResult(
                    $"Invalid expand value(s): {string.Join(", ", invalidValues)}. {ErrorMessage}");
            }

            return ValidationResult.Success;
        }
    }
}
