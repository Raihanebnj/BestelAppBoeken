using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BestelAppBoeken.Web.Services
{
    public class JsonSchemaValidator
    {
        private readonly Dictionary<string, JsonSchema> _schemas = new();

        public JsonSchemaValidator()
        {
            // Define GDPR-compliant schemas
            InitializeSchemas();
        }

        private void InitializeSchemas()
        {
            // Order schema 
            var orderSchema = new JsonSchema
            {
                RequiredFields = new List<string> { "orderId", "timestamp" },
                OptionalFields = new List<string> { "customerHash", "productId", "quantity" },
                FieldConstraints = new Dictionary<string, FieldConstraint>
                {
                    { "customerHash", new FieldConstraint { MaxLength = 64, Pattern = @"^[a-fA-F0-9]{64}$" } },
                    { "orderId", new FieldConstraint { MaxLength = 36, Pattern = @"^[a-fA-F0-9\-]{36}$" } }
                },
                ForbiddenFields = new List<string> { "email", "phone", "address", "birthDate", "bsn" }
            };

            _schemas.Add("order", orderSchema);
        }

        public ValidationResult Validate(string schemaName, JObject data)
        {
            if (!_schemas.ContainsKey(schemaName))
                return ValidationResult.Error($"Schema '{schemaName}' niet gevonden");

            var schema = _schemas[schemaName];
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            // Check required fields
            foreach (var field in schema.RequiredFields)
            {
                if (!data.ContainsKey(field))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Vereist veld ontbreekt: {field}");
                }
            }

            // Check forbidden fields (GDPR compliance)
            foreach (var field in schema.ForbiddenFields)
            {
                if (data.ContainsKey(field))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Verboden veld gedetecteerd: {field}");
                }
            }

            return result;
        }

        public class JsonSchema
        {
            public List<string> RequiredFields { get; set; }
            public List<string> OptionalFields { get; set; }
            public List<string> ForbiddenFields { get; set; }
            public Dictionary<string, FieldConstraint> FieldConstraints { get; set; }
        }

        public class FieldConstraint
        {
            public int? MaxLength { get; set; }
            public string Pattern { get; set; }
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; }

            public static ValidationResult Error(string message) => new()
            {
                IsValid = false,
                Errors = new List<string> { message }
            };
        }
    }
}