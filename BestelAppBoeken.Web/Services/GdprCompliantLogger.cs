using Microsoft.Extensions.Logging;
using System;

namespace BestelAppBoeken.Web.Services
{
    public class GdprCompliantLogger
    {
        private readonly ILogger<GdprCompliantLogger> _logger;

        public GdprCompliantLogger(ILogger<GdprCompliantLogger> logger)
        {
            _logger = logger;
        }

        public void LogSecurityEvent(string eventType, string action, string userIdHash, bool success)
        {
            // Log zonder persoonsgegevens - alleen gehashte user ID
            _logger.LogInformation("Security Event: {EventType}, Action: {Action}, UserHash: {UserIdHash}, Success: {Success}, Timestamp: {Timestamp}",
                eventType, action, userIdHash, success, DateTime.UtcNow);
        }

        public void LogDataProcessing(string processor, string dataType, string purpose, bool anonymized)
        {
            _logger.LogInformation("GDPR Processing: Processor: {Processor}, DataType: {DataType}, Purpose: {Purpose}, Anonymized: {Anonymized}",
                processor, dataType, purpose, anonymized);
        }

        public void LogValidationError(string validator, string errorType, bool containsPii)
        {
            _logger.LogWarning("Validation Error: Validator: {Validator}, ErrorType: {ErrorType}, ContainsPII: {ContainsPii}",
                validator, errorType, containsPii);
        }
    }
}