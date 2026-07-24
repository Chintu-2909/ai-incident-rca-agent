using IncidentRca.Application.Models;
using IncidentRca.Domain.Enums;
using IncidentRca.Infrastructure.Persistence;
using IncidentRca.Infrastructure.Services;
using IncidentRca.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace IncidentRca.Tests.Services;

[TestFixture]
public sealed class IncidentAgentToolsTests
{
    private IncidentRcaDbContext _dbContext = null!;
    private IncidentAgentTools _agentTools = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        _dbContext =
            await TestDbContextFactory.CreateSeededAsync();

        _agentTools = new IncidentAgentTools(
            _dbContext,
            NullLogger<IncidentAgentTools>.Instance);
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Test]
    public async Task GetIncidentDetailsAsync_WhenIncidentExists_ReturnsIncident()
    {
        var result =
            await _agentTools.GetIncidentDetailsAsync(
                "INC-1001");

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(
                result!.IncidentNumber,
                Is.EqualTo("INC-1001"));

            Assert.That(
                result.ErrorCode,
                Is.EqualTo("INVALID_DEPARTMENT"));

            Assert.That(
                result.KnownRootCauseCategory,
                Is.EqualTo(
                    RootCauseCategory.MappingFailure));

            Assert.That(
                result.CorrelationId,
                Is.EqualTo("CORR-MAP-1001"));
        });
    }

    [Test]
    public async Task GetIncidentDetailsAsync_NormalizesIncidentNumber()
    {
        var result =
            await _agentTools.GetIncidentDetailsAsync(
                "  inc-1002  ");

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(
                result!.IncidentNumber,
                Is.EqualTo("INC-1002"));

            Assert.That(
                result.KnownRootCauseCategory,
                Is.EqualTo(
                    RootCauseCategory.AuthenticationFailure));
        });
    }

    [Test]
    public async Task GetIncidentDetailsAsync_WhenIncidentDoesNotExist_ReturnsNull()
    {
        var result =
            await _agentTools.GetIncidentDetailsAsync(
                "INC-9999");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetIncidentDetailsAsync_WhenIncidentNumberIsEmpty_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () =>
                await _agentTools.GetIncidentDetailsAsync(
                    " "));
    }

    [Test]
    public async Task FindSimilarIncidentsAsync_ReturnsDeterministicResults()
    {
        var results =
            await _agentTools.FindSimilarIncidentsAsync(
                "INC-1001");

        Assert.That(results, Has.Count.EqualTo(3));

        Assert.Multiple(() =>
        {
            Assert.That(
                results.All(
                    result =>
                        result.SimilarityScore == 35),
                Is.True);

            Assert.That(
                results.All(
                    result =>
                        result.MatchingSignals.Contains(
                            "Same integration")),
                Is.True);

            Assert.That(
                results.All(
                    result =>
                        result.MatchingSignals.Contains(
                            "Same affected system")),
                Is.True);
        });
    }

    [Test]
    public async Task FindSimilarIncidentsAsync_WhenIncidentDoesNotExist_ReturnsEmpty()
    {
        var results =
            await _agentTools.FindSimilarIncidentsAsync(
                "INC-9999");

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task GetIntegrationRunbookAsync_ReturnsCorrectMappingRunbook()
    {
        var result =
            await _agentTools.GetIntegrationRunbookAsync(
                "Employee Onboarding Integration",
                RootCauseCategory.MappingFailure);

        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(
                result!.RunbookCode,
                Is.EqualTo("RB-HCM-MAP-001"));

            Assert.That(
                result.Category,
                Is.EqualTo(
                    RootCauseCategory.MappingFailure));

            Assert.That(
                result.CorrectiveActions,
                Is.Not.Empty);

            Assert.That(
                result.ValidationSteps,
                Is.Not.Empty);

            Assert.That(
                result.EscalationTeam,
                Is.EqualTo("HCM Integration Support"));
        });
    }

    [Test]
    public async Task GetIntegrationRunbookAsync_WhenNoMatchExists_ReturnsNull()
    {
        var result =
            await _agentTools.GetIntegrationRunbookAsync(
                "Employee Onboarding Integration",
                RootCauseCategory.AuthorizationFailure);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ValidateIncidentEvidenceAsync_WithCompleteIncident_ReturnsSufficientEvidence()
    {
        var incident =
            await _agentTools.GetIncidentDetailsAsync(
                "INC-1001");

        Assert.That(incident, Is.Not.Null);

        var result =
            await _agentTools.ValidateIncidentEvidenceAsync(
                incident!);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.HasSufficientEvidence,
                Is.True);

            Assert.That(
                result.ConfirmedFacts,
                Has.Count.EqualTo(7));

            Assert.That(
                result.Assumptions,
                Is.Empty);

            Assert.That(
                result.MissingInformation,
                Is.Empty);

            Assert.That(
                result.ValidationSummary,
                Does.Contain("sufficient"));
        });
    }

    [Test]
    public async Task ValidateIncidentEvidenceAsync_WithMissingEvidence_ReportsMissingInformation()
    {
        var incident = new IncidentToolResult
        {
            IncidentId = 999,
            IncidentNumber = "INC-TEST",
            Title = "Incomplete incident",
            Description = "Test incident",
            IntegrationName =
                "Employee Onboarding Integration",
            AffectedSystem = "Mock HCM",
            Environment = "Production Simulation",
            Severity = IncidentSeverity.Medium,
            Status = IncidentStatus.Investigating,
            KnownRootCauseCategory =
                RootCauseCategory.Unknown
        };

        var result =
            await _agentTools.ValidateIncidentEvidenceAsync(
                incident);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.HasSufficientEvidence,
                Is.False);

            Assert.That(
                result.MissingInformation,
                Does.Contain(
                    "A structured error code is missing."));

            Assert.That(
                result.MissingInformation,
                Does.Contain(
                    "A correlation ID is missing."));

            Assert.That(
                result.MissingInformation,
                Does.Contain(
                    "Technical investigation evidence is missing."));

            Assert.That(
                result.MissingInformation,
                Does.Contain(
                    "The business impact is missing."));

            Assert.That(
                result.Assumptions,
                Does.Contain(
                    "The root-cause category has not been confirmed."));
        });
    }
}
