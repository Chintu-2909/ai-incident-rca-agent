using IncidentRca.Domain.Enums;
using IncidentRca.Domain.Models;
using IncidentRca.Infrastructure.Persistence;
using IncidentRca.Infrastructure.Services;
using IncidentRca.Tests.Fakes;
using IncidentRca.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IncidentRca.Tests.Services;

[TestFixture]
public sealed class IncidentInvestigationServiceTests
{
    private IncidentRcaDbContext _dbContext = null!;
    private IncidentAgentTools _agentTools = null!;
    private FakeAiAgentClient _aiClient = null!;
    private IncidentInvestigationService _service = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        _dbContext =
            await TestDbContextFactory.CreateSeededAsync();

        _agentTools = new IncidentAgentTools(
            _dbContext,
            NullLogger<IncidentAgentTools>.Instance);

        _aiClient = new FakeAiAgentClient();

        _service = new IncidentInvestigationService(
            _dbContext,
            _agentTools,
            _aiClient,
            NullLogger<IncidentInvestigationService>.Instance);
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Test]
    public async Task InvestigateAsync_WithValidIncident_ReturnsCompletedReport()
    {
        var request = CreateValidRequest();

        var result =
            await _service.InvestigateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.InvestigationId,
                Is.Not.EqualTo(Guid.Empty));

            Assert.That(
                result.IncidentNumber,
                Is.EqualTo("INC-1001"));

            Assert.That(
                result.RootCauseCategory,
                Is.EqualTo(
                    RootCauseCategory.MappingFailure));

            Assert.That(
                result.Confidence,
                Is.EqualTo(ConfidenceLevel.High));

            Assert.That(
                result.ToolsExecuted,
                Has.Count.EqualTo(4));

            Assert.That(
                result.ToolsExecuted.All(
                    tool => tool.Successful),
                Is.True);

            Assert.That(
                _aiClient.CallCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public async Task InvestigateAsync_PersistsCompletedInvestigation()
    {
        var result =
            await _service.InvestigateAsync(
                CreateValidRequest());

        var investigation =
            await _dbContext.Investigations
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id ==
                        result.InvestigationId);

        Assert.Multiple(() =>
        {
            Assert.That(
                investigation.Status,
                Is.EqualTo(
                    InvestigationStatus.Completed));

            Assert.That(
                investigation.AiProvider,
                Is.EqualTo("FakeAI"));

            Assert.That(
                investigation.ModelName,
                Is.EqualTo("fake-incident-model"));

            Assert.That(
                investigation.RootCauseCategory,
                Is.EqualTo(
                    RootCauseCategory.MappingFailure));

            Assert.That(
                investigation.Confidence,
                Is.EqualTo(ConfidenceLevel.High));

            Assert.That(
                investigation.CompletedAtUtc,
                Is.Not.Null);

            Assert.That(
                investigation.ResultJson,
                Does.Contain("INC-1001"));
        });
    }

    [Test]
    public async Task InvestigateAsync_PersistsFourToolExecutions()
    {
        var result =
            await _service.InvestigateAsync(
                CreateValidRequest());

        var executions =
            await _dbContext.ToolExecutions
                .AsNoTracking()
                .Where(
                    item =>
                        item.InvestigationId ==
                        result.InvestigationId)
                .OrderBy(item => item.StepNumber)
                .ToListAsync();

        Assert.That(executions, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(
                executions.Select(
                    item => item.ToolName).ToList(),
                Is.EqualTo(
                    new[]
                    {
                        "GetIncidentDetails",
                        "FindSimilarIncidents",
                        "GetIntegrationRunbook",
                        "ValidateIncidentEvidence"
                    }));

            Assert.That(
                executions.All(
                    item =>
                        item.Status ==
                        ToolExecutionStatus.Completed),
                Is.True);

            Assert.That(
                executions.All(
                    item =>
                        !string.IsNullOrWhiteSpace(
                            item.ResultJson)),
                Is.True);
        });
    }

    [Test]
    public async Task GetInvestigationAsync_WhenCompleted_ReturnsPersistedReport()
    {
        var created =
            await _service.InvestigateAsync(
                CreateValidRequest());

        var retrieved =
            await _service.GetInvestigationAsync(
                created.InvestigationId);

        Assert.That(retrieved, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(
                retrieved!.InvestigationId,
                Is.EqualTo(
                    created.InvestigationId));

            Assert.That(
                retrieved.IncidentNumber,
                Is.EqualTo("INC-1001"));

            Assert.That(
                retrieved.RootCauseCategory,
                Is.EqualTo(
                    RootCauseCategory.MappingFailure));

            Assert.That(
                retrieved.ToolsExecuted,
                Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void InvestigateAsync_WhenIncidentDoesNotExist_ThrowsKeyNotFoundException()
    {
        var request =
            new StartInvestigationRequest(
                "INC-9999",
                "Prepare an incident report.");

        var exception =
            Assert.ThrowsAsync<KeyNotFoundException>(
                async () =>
                    await _service.InvestigateAsync(
                        request));

        Assert.That(
            exception!.Message,
            Does.Contain("INC-9999"));
    }

    [Test]
    public void InvestigateAsync_WhenIncidentNumberIsBlank_ThrowsArgumentException()
    {
        var request =
            new StartInvestigationRequest(
                " ",
                "Prepare an incident report.");

        var exception =
            Assert.ThrowsAsync<ArgumentException>(
                async () =>
                    await _service.InvestigateAsync(
                        request));

        Assert.That(
            exception!.Message,
            Does.Contain(
                "Incident number is required"));
    }

    [Test]
    public void InvestigateAsync_WhenObjectiveIsBlank_ThrowsArgumentException()
    {
        var request =
            new StartInvestigationRequest(
                "INC-1001",
                " ");

        var exception =
            Assert.ThrowsAsync<ArgumentException>(
                async () =>
                    await _service.InvestigateAsync(
                        request));

        Assert.That(
            exception!.Message,
            Does.Contain(
                "Investigation objective is required"));
    }

    [Test]
    public async Task InvestigateAsync_WhenAiProviderFails_MarksInvestigationAsFailed()
    {
        _aiClient.ExceptionToThrow =
            new InvalidOperationException(
                "Synthetic AI provider failure.");

        var exception =
            Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await _service.InvestigateAsync(
                        CreateValidRequest()));

        Assert.That(
            exception!.Message,
            Is.EqualTo(
                "Synthetic AI provider failure."));

        var investigation =
            await _dbContext.Investigations
                .AsNoTracking()
                .SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                investigation.Status,
                Is.EqualTo(
                    InvestigationStatus.Failed));

            Assert.That(
                investigation.FailureReason,
                Is.EqualTo(
                    "Synthetic AI provider failure."));

            Assert.That(
                investigation.CompletedAtUtc,
                Is.Not.Null);

            Assert.That(
                investigation.ResultJson,
                Is.EqualTo("{}"));
        });

        var toolExecutionCount =
            await _dbContext.ToolExecutions.CountAsync();

        Assert.That(
            toolExecutionCount,
            Is.EqualTo(4));
    }

    private static StartInvestigationRequest
        CreateValidRequest()
    {
        return new StartInvestigationRequest(
            "INC-1001",
            "Prepare an evidence-based RCA, ServiceNow work note, stakeholder update, and validation checklist.");
    }
}
