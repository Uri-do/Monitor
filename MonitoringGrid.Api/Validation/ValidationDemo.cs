using FluentValidation;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Validation;

namespace MonitoringGrid.Api.Validation;

/// <summary>
/// Demonstration of enhanced validation capabilities
/// </summary>
public static class ValidationDemo
{
    /// <summary>
    /// Demonstrates the enhanced validation system with various scenarios
    /// </summary>
    public static void DemonstrateValidation()
    {
        Console.WriteLine("=== Enhanced Validation System Demo ===\n");

        // Test 1: Valid KPI request
        Console.WriteLine("Test 1: Valid KPI Request");
        var validRequest = new CreateKpiRequest
        {
            Indicator = "Valid_KPI_Test",
            Owner = "test@example.com",
            Priority = 2, // Email only
            Frequency = 60, // 1 hour
            LastMinutes = 120, // 2 hours data window
            Deviation = 5.0m,
            SpName = "monitoring.test_procedure",
            SubjectTemplate = "Alert: {current} vs {historical} (deviation: {deviation}%)",
            DescriptionTemplate = "Current: {current}, Historical: {historical}, Deviation: {deviation}%",
            CooldownMinutes = 60,
            ContactIds = new List<int> { 1, 2 }
        };

        TestValidation("Valid Request", validRequest);

        // Test 2: Invalid frequency for priority
        Console.WriteLine("\nTest 2: Invalid Frequency for Priority (SMS alerts too frequent)");
        var invalidFrequencyRequest = new CreateKpiRequest
        {
            Indicator = validRequest.Indicator,
            Owner = validRequest.Owner,
            Priority = 1, // SMS + Email
            Frequency = 2, // Too frequent for SMS alerts
            LastMinutes = validRequest.LastMinutes,
            Deviation = validRequest.Deviation,
            SpName = validRequest.SpName,
            SubjectTemplate = validRequest.SubjectTemplate,
            DescriptionTemplate = validRequest.DescriptionTemplate,
            CooldownMinutes = validRequest.CooldownMinutes,
            ContactIds = validRequest.ContactIds
        };

        TestValidation("Invalid Frequency for Priority", invalidFrequencyRequest);

        // Test 3: Invalid stored procedure name
        Console.WriteLine("\nTest 3: Invalid Stored Procedure Name");
        var invalidSpRequest = new CreateKpiRequest
        {
            Indicator = validRequest.Indicator,
            Owner = validRequest.Owner,
            Priority = validRequest.Priority,
            Frequency = validRequest.Frequency,
            LastMinutes = validRequest.LastMinutes,
            Deviation = validRequest.Deviation,
            SpName = "DROP TABLE users; --", // Dangerous SQL
            SubjectTemplate = validRequest.SubjectTemplate,
            DescriptionTemplate = validRequest.DescriptionTemplate,
            CooldownMinutes = validRequest.CooldownMinutes,
            ContactIds = validRequest.ContactIds
        };

        TestValidation("Invalid Stored Procedure", invalidSpRequest);

        // Test 4: Invalid template (missing placeholders)
        Console.WriteLine("\nTest 4: Invalid Template (Missing Placeholders)");
        var invalidTemplateRequest = new CreateKpiRequest
        {
            Indicator = validRequest.Indicator,
            Owner = validRequest.Owner,
            Priority = validRequest.Priority,
            Frequency = validRequest.Frequency,
            LastMinutes = validRequest.LastMinutes,
            Deviation = validRequest.Deviation,
            SpName = validRequest.SpName,
            SubjectTemplate = "Simple alert message", // Missing required placeholders
            DescriptionTemplate = validRequest.DescriptionTemplate,
            CooldownMinutes = validRequest.CooldownMinutes,
            ContactIds = validRequest.ContactIds
        };

        TestValidation("Invalid Template", invalidTemplateRequest);

        // Test 5: Invalid cooldown period
        Console.WriteLine("\nTest 5: Invalid Cooldown Period");
        var invalidCooldownRequest = new CreateKpiRequest
        {
            Indicator = validRequest.Indicator,
            Owner = validRequest.Owner,
            Priority = validRequest.Priority,
            Frequency = 5, // 5 minutes
            LastMinutes = validRequest.LastMinutes,
            Deviation = validRequest.Deviation,
            SpName = validRequest.SpName,
            SubjectTemplate = validRequest.SubjectTemplate,
            DescriptionTemplate = validRequest.DescriptionTemplate,
            CooldownMinutes = 2, // Too short for high-frequency KPI
            ContactIds = validRequest.ContactIds
        };

        TestValidation("Invalid Cooldown", invalidCooldownRequest);

        // Test 6: Invalid data window
        Console.WriteLine("\nTest 6: Invalid Data Window");
        var invalidDataWindowRequest = new CreateKpiRequest
        {
            Indicator = validRequest.Indicator,
            Owner = validRequest.Owner,
            Priority = validRequest.Priority,
            Frequency = 5, // 5 minutes (high frequency)
            LastMinutes = 120, // Too large data window for high-frequency KPI
            Deviation = validRequest.Deviation,
            SpName = validRequest.SpName,
            SubjectTemplate = validRequest.SubjectTemplate,
            DescriptionTemplate = validRequest.DescriptionTemplate,
            CooldownMinutes = validRequest.CooldownMinutes,
            ContactIds = validRequest.ContactIds
        };

        TestValidation("Invalid Data Window", invalidDataWindowRequest);

        Console.WriteLine("\n=== Validation Demo Complete ===");
    }

    private static void TestValidation(string testName, CreateKpiRequest request)
    {
        try
        {
            // Test frequency validation
            var frequencyValid = EnhancedValidationExtensions.ValidateFrequencyForPriority(request.Frequency, request.Priority);
            Console.WriteLine($"  Frequency validation: {(frequencyValid ? "✓ PASS" : "✗ FAIL")}");
            if (!frequencyValid)
            {
                Console.WriteLine($"    Error: {EnhancedValidationExtensions.GetFrequencyValidationMessage(request.Frequency, request.Priority)}");
            }

            // Test stored procedure validation
            var spValid = EnhancedValidationExtensions.ValidateStoredProcedureName(request.SpName);
            Console.WriteLine($"  Stored Procedure validation: {(spValid ? "✓ PASS" : "✗ FAIL")}");
            if (!spValid)
            {
                Console.WriteLine($"    Error: Stored procedure name is invalid or contains dangerous patterns");
            }

            // Test template validation
            var templateValid = EnhancedValidationExtensions.ValidateTemplate(request.SubjectTemplate);
            Console.WriteLine($"  Template validation: {(templateValid ? "✓ PASS" : "✗ FAIL")}");
            if (!templateValid)
            {
                Console.WriteLine($"    Error: Template missing required placeholders or contains unsafe content");
            }

            // Test cooldown validation
            var cooldownValid = EnhancedValidationExtensions.ValidateCooldownPeriod(request.CooldownMinutes, request.Frequency);
            Console.WriteLine($"  Cooldown validation: {(cooldownValid ? "✓ PASS" : "✗ FAIL")}");
            if (!cooldownValid)
            {
                Console.WriteLine($"    Error: {EnhancedValidationExtensions.GetCooldownValidationMessage(request.CooldownMinutes, request.Frequency)}");
            }

            // Test data window validation
            var dataWindowValid = EnhancedValidationExtensions.ValidateDataWindow(request.LastMinutes, request.Frequency);
            Console.WriteLine($"  Data Window validation: {(dataWindowValid ? "✓ PASS" : "✗ FAIL")}");
            if (!dataWindowValid)
            {
                Console.WriteLine($"    Error: {EnhancedValidationExtensions.GetDataWindowValidationMessage(request.LastMinutes, request.Frequency)}");
            }

            var overallValid = frequencyValid && spValid && templateValid && cooldownValid && dataWindowValid;
            Console.WriteLine($"  Overall Result: {(overallValid ? "✓ VALID" : "✗ INVALID")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error during validation: {ex.Message}");
        }
    }

    // Helper methods to access private validation methods
    private static class EnhancedValidationExtensions
    {
        public static bool ValidateFrequencyForPriority(int frequency, byte priority)
        {
            return priority switch
            {
                1 when frequency < 5 => false, // SMS alerts shouldn't run more than every 5 minutes
                1 when frequency > 1440 => false, // High priority should run at least daily
                2 when frequency < 1 => false, // Email-only shouldn't run more than every minute
                2 when frequency > 10080 => false, // Email-only should run at least weekly
                _ => true
            };
        }

        public static string GetFrequencyValidationMessage(int frequency, byte priority)
        {
            return priority switch
            {
                1 when frequency < 5 => "High priority KPIs (SMS alerts) should not run more frequently than every 5 minutes to avoid spam",
                1 when frequency > 1440 => "High priority KPIs should run at least once per day",
                2 when frequency < 1 => "Email-only KPIs should not run more frequently than every minute",
                2 when frequency > 10080 => "Email-only KPIs should run at least once per week",
                _ => "Frequency is valid for this priority level"
            };
        }

        public static bool ValidateStoredProcedureName(string spName)
        {
            if (string.IsNullOrWhiteSpace(spName)) return false;
            
            // Check for dangerous patterns
            var dangerousPatterns = new[] { ";", "--", "/*", "*/", "xp_", "DROP", "DELETE", "TRUNCATE", "ALTER" };
            if (dangerousPatterns.Any(pattern => spName.ToUpper().Contains(pattern.ToUpper()))) return false;
            
            // Validate naming convention
            if (!spName.StartsWith("monitoring.") && !spName.StartsWith("stats.")) return false;
            
            // Check length
            if (spName.Length > 255) return false;
            
            return true;
        }

        public static bool ValidateTemplate(string template)
        {
            if (string.IsNullOrWhiteSpace(template)) return false;
            
            // Check for required placeholders
            var requiredPlaceholders = new[] { "{current}", "{historical}", "{deviation}" };
            if (requiredPlaceholders.Any(p => !template.Contains(p))) return false;
            
            // Check for dangerous content
            var dangerousPatterns = new[] { "<script", "javascript:", "onclick=", "onerror=" };
            if (dangerousPatterns.Any(pattern => template.ToLower().Contains(pattern))) return false;
            
            return true;
        }

        public static bool ValidateCooldownPeriod(int cooldownMinutes, int frequency)
        {
            if (cooldownMinutes < 0) return false;
            if (cooldownMinutes > frequency * 10) return false; // Cooldown shouldn't exceed 10x frequency
            if (frequency <= 5 && cooldownMinutes < frequency) return false; // High-frequency needs adequate cooldown
            return true;
        }

        public static string GetCooldownValidationMessage(int cooldownMinutes, int frequency)
        {
            if (cooldownMinutes < 0) return "Cooldown period cannot be negative";
            if (cooldownMinutes > frequency * 10) return "Cooldown period should not exceed 10 times the execution frequency";
            if (frequency <= 5 && cooldownMinutes < frequency) return "High-frequency KPIs should have cooldown periods at least equal to their frequency";
            return "Cooldown period is valid";
        }

        public static bool ValidateDataWindow(int lastMinutes, int frequency)
        {
            if (lastMinutes <= 0) return false;
            if (lastMinutes < frequency) return false; // Data window should be at least equal to frequency
            if (lastMinutes > frequency * 100) return false; // Data window shouldn't be excessive
            if (frequency <= 5 && lastMinutes > 60) return false; // High-frequency should use smaller windows
            if (frequency >= 1440 && lastMinutes < 1440) return false; // Daily KPIs should use at least 24h windows
            return true;
        }

        public static string GetDataWindowValidationMessage(int lastMinutes, int frequency)
        {
            if (lastMinutes <= 0) return "Data window must be greater than 0";
            if (lastMinutes < frequency) return "Data window should be at least equal to the execution frequency";
            if (lastMinutes > frequency * 100) return "Data window should not exceed 100 times the execution frequency";
            if (frequency <= 5 && lastMinutes > 60) return "High-frequency KPIs (≤5 min) should use data windows ≤60 minutes";
            if (frequency >= 1440 && lastMinutes < 1440) return "Daily KPIs should use data windows of at least 24 hours";
            return "Data window is valid";
        }
    }
}
