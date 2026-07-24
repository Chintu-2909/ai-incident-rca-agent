using IncidentRca.Domain.Entities;
using IncidentRca.Domain.Enums;

namespace IncidentRca.Infrastructure.Seed;

public static class SyntheticDataFactory
{
    public static IReadOnlyList<Incident> CreateIncidents()
    {
        var now = DateTimeOffset.UtcNow;

        return
        [
            new Incident
            {
                IncidentNumber = "INC-1001",
                Title = "Employee creation failed due to missing department mapping",
                Description =
                    "Approved employee onboarding requests were not created in the Mock HCM system.",
                IntegrationName = "Employee Onboarding Integration",
                AffectedSystem = "Mock HCM",
                Environment = "Production Simulation",
                BusinessImpact =
                    "Five approved employees are waiting for creation in the target HCM system.",
                TechnicalEvidence =
                    """
                    The source request was approved successfully.
                    The integration API submitted the employee payload to Mock HCM.
                    Mock HCM returned HTTP 400 with error code INVALID_DEPARTMENT.
                    All failed records contain source department code FIN-OPS.
                    No FIN-OPS mapping exists in the target department mapping table.
                    No employee records were created for the affected requests.
                    """,
                ErrorCode = "INVALID_DEPARTMENT",
                ErrorMessage =
                    "Department code FIN-OPS is not recognized by the target system.",
                CorrelationId = "CORR-MAP-1001",
                Severity = IncidentSeverity.High,
                Status = IncidentStatus.Resolved,
                KnownRootCauseCategory = RootCauseCategory.MappingFailure,
                ConfirmedResolution =
                    "Added the FIN-OPS to department 104 mapping and successfully reprocessed the affected requests.",
                DetectedAtUtc = now.AddHours(-12),
                CreatedAtUtc = now.AddHours(-12),
                ResolvedAtUtc = now.AddHours(-10)
            },
            new Incident
            {
                IncidentNumber = "INC-1002",
                Title = "HCM authentication token expired",
                Description =
                    "Employee onboarding transactions failed while calling the Mock HCM employee endpoint.",
                IntegrationName = "Employee Onboarding Integration",
                AffectedSystem = "Mock HCM",
                Environment = "Production Simulation",
                BusinessImpact =
                    "New employee records cannot be created until authentication is restored.",
                TechnicalEvidence =
                    """
                    Twelve requests failed within a twenty-minute period.
                    Every failed dependency call returned HTTP 401.
                    The response body contains TOKEN_EXPIRED.
                    The configured token expiry time occurred before the first failed request.
                    Network connectivity checks completed successfully.
                    No payload validation failures were recorded.
                    """,
                ErrorCode = "TOKEN_EXPIRED",
                ErrorMessage =
                    "The access token used for the HCM API has expired.",
                CorrelationId = "CORR-AUTH-1002",
                Severity = IncidentSeverity.Critical,
                Status = IncidentStatus.Resolved,
                KnownRootCauseCategory = RootCauseCategory.AuthenticationFailure,
                ConfirmedResolution =
                    "Renewed the Mock HCM access token, validated connectivity, and reprocessed one transaction successfully.",
                DetectedAtUtc = now.AddDays(-3),
                CreatedAtUtc = now.AddDays(-3),
                ResolvedAtUtc = now.AddDays(-3).AddHours(1)
            },
            new Incident
            {
                IncidentNumber = "INC-1003",
                Title = "Downstream HCM requests timing out",
                Description =
                    "The onboarding API is timing out while waiting for responses from Mock HCM.",
                IntegrationName = "Employee Onboarding Integration",
                AffectedSystem = "Mock HCM",
                Environment = "Production Simulation",
                BusinessImpact =
                    "Employee onboarding is delayed and retry queues are increasing.",
                TechnicalEvidence =
                    """
                    Application requests started timing out after 09:40 UTC.
                    Mock HCM dependency duration increased from 900 milliseconds to 18 seconds.
                    The integration timeout is configured for 15 seconds.
                    Six requests returned HTTP 504.
                    No authentication, validation, or mapping errors were recorded.
                    Mock HCM health checks showed degraded performance during the same period.
                    """,
                ErrorCode = "DEPENDENCY_TIMEOUT",
                ErrorMessage =
                    "The downstream HCM service did not respond within the configured timeout.",
                CorrelationId = "CORR-TIMEOUT-1003",
                Severity = IncidentSeverity.High,
                Status = IncidentStatus.Resolved,
                KnownRootCauseCategory = RootCauseCategory.DownstreamTimeout,
                ConfirmedResolution =
                    "Mock HCM latency returned to normal and failed requests were reprocessed after health validation.",
                DetectedAtUtc = now.AddDays(-6),
                CreatedAtUtc = now.AddDays(-6),
                ResolvedAtUtc = now.AddDays(-6).AddHours(2)
            },
            new Incident
            {
                IncidentNumber = "INC-1004",
                Title = "Duplicate employee onboarding request rejected",
                Description =
                    "A repeated onboarding request attempted to create an employee who already exists.",
                IntegrationName = "Employee Onboarding Integration",
                AffectedSystem = "Mock HCM",
                Environment = "Production Simulation",
                BusinessImpact =
                    "One onboarding request remains in failed status although the employee already exists.",
                TechnicalEvidence =
                    """
                    Two requests were submitted with the same employee identifier EMP-2048.
                    The first request completed successfully.
                    The second request returned HTTP 409 with error code DUPLICATE_EMPLOYEE.
                    The idempotency key was missing from the second source request.
                    The target employee record is active and contains the expected information.
                    """,
                ErrorCode = "DUPLICATE_EMPLOYEE",
                ErrorMessage =
                    "An employee with identifier EMP-2048 already exists.",
                CorrelationId = "CORR-DUP-1004",
                Severity = IncidentSeverity.Medium,
                Status = IncidentStatus.Resolved,
                KnownRootCauseCategory = RootCauseCategory.DuplicateProcessing,
                ConfirmedResolution =
                    "Marked the duplicate transaction as completed after validating the existing employee record.",
                DetectedAtUtc = now.AddDays(-9),
                CreatedAtUtc = now.AddDays(-9),
                ResolvedAtUtc = now.AddDays(-9).AddMinutes(45)
            },
            new Incident
            {
                IncidentNumber = "INC-1005",
                Title = "Integration database connection failure",
                Description =
                    "The onboarding API cannot persist new integration transactions.",
                IntegrationName = "Employee Onboarding Integration",
                AffectedSystem = "Integration Database",
                Environment = "Production Simulation",
                BusinessImpact =
                    "New onboarding requests cannot be stored or processed.",
                TechnicalEvidence =
                    """
                    The API returned HTTP 500 when saving new transactions.
                    Application logs contain a database connection exception.
                    The configured database endpoint is unreachable.
                    Three connection retries failed.
                    Mock HCM health checks remained healthy.
                    No request validation or authentication failures were identified.
                    """,
                ErrorCode = "DB_CONNECTION_FAILED",
                ErrorMessage =
                    "A connection could not be established with the integration database.",
                CorrelationId = "CORR-DB-1005",
                Severity = IncidentSeverity.Critical,
                Status = IncidentStatus.Resolved,
                KnownRootCauseCategory = RootCauseCategory.DatabaseConnectivity,
                ConfirmedResolution =
                    "Restored database connectivity, validated transaction persistence, and resumed processing.",
                DetectedAtUtc = now.AddDays(-14),
                CreatedAtUtc = now.AddDays(-14),
                ResolvedAtUtc = now.AddDays(-14).AddHours(1)
            }
        ];
    }

    public static IReadOnlyList<Runbook> CreateRunbooks()
    {
        var now = DateTimeOffset.UtcNow;

        return
        [
            new Runbook
            {
                RunbookCode = "RB-HCM-MAP-001",
                Title = "Resolve missing HCM department mappings",
                IntegrationName = "Employee Onboarding Integration",
                Category = RootCauseCategory.MappingFailure,
                Symptoms =
                    "HTTP 400 responses containing INVALID_DEPARTMENT or UNKNOWN_DEPARTMENT.",
                InvestigationSteps =
                    """
                    1. Retrieve the failed transaction using its correlation ID.
                    2. Confirm the source department code.
                    3. Check whether a target HCM department mapping exists.
                    4. Verify that no employee record was created.
                    5. Confirm that other mandatory fields passed validation.
                    """,
                CorrectiveActions =
                    """
                    Add or correct the approved department mapping.
                    Validate the mapping in a non-production environment.
                    Retry one failed transaction after approval.
                    """,
                PreventiveActions =
                    """
                    Validate department mappings before submitting employee requests.
                    Alert when an unknown department code is received.
                    Review source-to-target mappings after organizational changes.
                    """,
                ValidationSteps =
                    """
                    Confirm that the employee exists in Mock HCM.
                    Confirm that the department identifier is correct.
                    Confirm that the integration transaction completed successfully.
                    Monitor subsequent requests for fifteen minutes.
                    """,
                EscalationTeam = "HCM Integration Support",
                UpdatedAtUtc = now
            },
            new Runbook
            {
                RunbookCode = "RB-HCM-AUTH-002",
                Title = "Restore HCM API authentication",
                IntegrationName = "Employee Onboarding Integration",
                Category = RootCauseCategory.AuthenticationFailure,
                Symptoms =
                    "HTTP 401 responses containing TOKEN_EXPIRED or INVALID_TOKEN.",
                InvestigationSteps =
                    """
                    1. Confirm the dependency returned HTTP 401.
                    2. Check token expiry without exposing the token value.
                    3. Verify network connectivity separately.
                    4. Confirm that the configured identity has not been disabled.
                    """,
                CorrectiveActions =
                    """
                    Renew or rotate the approved authentication credential.
                    Update the secure configuration store.
                    Validate one read-only HCM request before reprocessing.
                    """,
                PreventiveActions =
                    """
                    Add token-expiry monitoring.
                    Rotate credentials before expiration.
                    Use managed identity or short-lived credentials where supported.
                    """,
                ValidationSteps =
                    """
                    Confirm that authentication succeeds.
                    Submit one controlled test request.
                    Verify that no new HTTP 401 responses occur.
                    Monitor the integration for fifteen minutes.
                    """,
                EscalationTeam = "Identity and HCM Integration Support",
                UpdatedAtUtc = now
            },
            new Runbook
            {
                RunbookCode = "RB-HCM-TIMEOUT-003",
                Title = "Investigate downstream HCM timeouts",
                IntegrationName = "Employee Onboarding Integration",
                Category = RootCauseCategory.DownstreamTimeout,
                Symptoms =
                    "HTTP 504 responses, task cancellations, or increased HCM dependency duration.",
                InvestigationSteps =
                    """
                    1. Compare application request duration with dependency duration.
                    2. Check Mock HCM health and availability.
                    3. Review timeout and retry configuration.
                    4. Determine whether failures are transient or continuous.
                    5. Check for a recent downstream deployment.
                    """,
                CorrectiveActions =
                    """
                    Wait for downstream health recovery when the dependency is degraded.
                    Reprocess failed requests in controlled batches.
                    Change timeout configuration only when supported by evidence.
                    """,
                PreventiveActions =
                    """
                    Add dependency latency alerts.
                    Use backoff for transient retries.
                    Add circuit-breaker protection.
                    Maintain a downstream availability dashboard.
                    """,
                ValidationSteps =
                    """
                    Confirm normal downstream response duration.
                    Execute one controlled transaction.
                    Monitor error rate and latency.
                    Confirm that retry queues are decreasing.
                    """,
                EscalationTeam = "HCM Platform Operations",
                UpdatedAtUtc = now
            },
            new Runbook
            {
                RunbookCode = "RB-HCM-DUP-004",
                Title = "Handle duplicate employee processing",
                IntegrationName = "Employee Onboarding Integration",
                Category = RootCauseCategory.DuplicateProcessing,
                Symptoms =
                    "HTTP 409 responses containing DUPLICATE_EMPLOYEE.",
                InvestigationSteps =
                    """
                    1. Confirm whether the target employee record already exists.
                    2. Compare the source and target employee identifiers.
                    3. Check previous transaction history.
                    4. Verify the idempotency key.
                    """,
                CorrectiveActions =
                    """
                    Do not create a second employee record.
                    Reconcile the failed transaction with the existing target record.
                    Mark the duplicate transaction appropriately after validation.
                    """,
                PreventiveActions =
                    """
                    Enforce idempotency keys.
                    Check employee existence before creation.
                    Add duplicate-request monitoring.
                    """,
                ValidationSteps =
                    """
                    Confirm that exactly one employee record exists.
                    Confirm that employee details match the approved request.
                    Confirm that the transaction is no longer retrying.
                    """,
                EscalationTeam = "Integration Support",
                UpdatedAtUtc = now
            },
            new Runbook
            {
                RunbookCode = "RB-DB-CONN-005",
                Title = "Restore integration database connectivity",
                IntegrationName = "Employee Onboarding Integration",
                Category = RootCauseCategory.DatabaseConnectivity,
                Symptoms =
                    "Database connection exceptions, failed connection retries, or HTTP 500 persistence failures.",
                InvestigationSteps =
                    """
                    1. Confirm the database exception type.
                    2. Check endpoint reachability.
                    3. Validate configuration without exposing secrets.
                    4. Check database availability and recent changes.
                    5. Determine whether all application instances are affected.
                    """,
                CorrectiveActions =
                    """
                    Restore the approved database endpoint or configuration.
                    Validate database connectivity.
                    Confirm that transactions can be persisted before resuming processing.
                    """,
                PreventiveActions =
                    """
                    Add database availability checks.
                    Monitor connection-pool usage.
                    Alert on repeated connection failures.
                    Maintain validated recovery procedures.
                    """,
                ValidationSteps =
                    """
                    Confirm that the health check passes.
                    Save and retrieve one synthetic transaction.
                    Confirm that failed requests can be processed.
                    Monitor database errors for fifteen minutes.
                    """,
                EscalationTeam = "Database and Integration Operations",
                UpdatedAtUtc = now
            },
            new Runbook
            {
                RunbookCode = "RB-HCM-CONFIG-006",
                Title =
                    "Resolve HCM certificate and endpoint configuration failures",
                IntegrationName =
                    "Employee Onboarding Integration",
                Category =
                    RootCauseCategory.ConfigurationFailure,
                Symptoms =
                    """
                    CERTIFICATE_VALIDATION_FAILED errors.
                    TLS negotiation or certificate-chain failures.
                    Dependency failures after certificate rotation.
                    Target endpoint or trusted-certificate configuration mismatch.
                    Requests fail while network connectivity remains available.
                    """,
                InvestigationSteps =
                    """
                    1. Retrieve the failed transaction using its correlation ID.
                    2. Confirm when the failures started.
                    3. Check for recent certificate, endpoint, or TLS configuration changes.
                    4. Compare successful calls before the change with failed calls after it.
                    5. Verify certificate trust, expiry, hostname, and the full certificate chain.
                    6. Confirm network connectivity separately from TLS validation.
                    7. Verify whether all application instances use the same configuration.
                    8. Confirm that payload validation completed successfully.
                    """,
                CorrectiveActions =
                    """
                    Update the approved trusted-certificate or endpoint configuration.
                    Validate the complete certificate chain and target hostname.
                    Reload or restart only the affected component when required.
                    Execute one controlled onboarding transaction after the correction.
                    Reprocess affected transactions only after successful validation.
                    """,
                PreventiveActions =
                    """
                    Monitor certificate expiry and alert before renewal deadlines.
                    Test certificate changes in a non-production environment.
                    Perform a connectivity test immediately after certificate rotation.
                    Maintain documented certificate ownership and renewal responsibilities.
                    Use controlled configuration management for endpoint and certificate changes.
                    Add automated TLS dependency health checks.
                    Review certificate rotation procedures after every related incident.
                    """,
                ValidationSteps =
                    """
                    Confirm that TLS negotiation succeeds.
                    Confirm that the HCM health check passes.
                    Execute one controlled employee onboarding transaction.
                    Confirm that the employee record is created correctly.
                    Confirm that no new certificate-validation errors are logged.
                    Monitor the integration for at least fifteen minutes.
                    """,
                EscalationTeam =
                    "HCM Platform and Integration Support",
                UpdatedAtUtc = now
            }

        ];
    }
}
