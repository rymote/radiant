using System.ComponentModel.DataAnnotations;

namespace Rymote.Radiant.Validation;

public static class ModelValidator
{
    public static ValidationResult ValidateModel<T>(T model) where T : class
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new ValidationContext(model);
        
        bool isValid = Validator.TryValidateObject(model, context, validationResults, true);
        
        return new ValidationResult
        {
            IsValid = isValid,
            Errors = validationResults.Select(r => r.ErrorMessage ?? "").ToList()
        };
    }

    public static async Task<T> ValidateAndSaveAsync<T>(this T model) where T : class
    {
        ValidationResult validation = ValidateModel(model);
        if (!validation.IsValid)
        {
            throw new ValidationException($"Validation failed: {string.Join(", ", validation.Errors)}");
        }

        if (model is Models.Model<object> baseModel)
        {
            return await baseModel.SaveAsync<T>();
        }

        throw new InvalidOperationException("Model must inherit from Model<TKey>");
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
