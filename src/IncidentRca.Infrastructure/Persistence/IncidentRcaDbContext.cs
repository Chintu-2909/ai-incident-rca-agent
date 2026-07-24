using IncidentRca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncidentRca.Infrastructure.Persistence;

public sealed class IncidentRcaDbContext(
    DbContextOptions<IncidentRcaDbContext> options)
    : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<Runbook> Runbooks => Set<Runbook>();

    public DbSet<Investigation> Investigations => Set<Investigation>();

    public DbSet<ToolExecution> ToolExecutions => Set<ToolExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIncident(modelBuilder);
        ConfigureRunbook(modelBuilder);
        ConfigureInvestigation(modelBuilder);
        ConfigureToolExecution(modelBuilder);
    }

    private static void ConfigureIncident(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Incident>();

        entity.ToTable("Incidents");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.IncidentNumber)
            .HasMaxLength(30)
            .IsRequired();

        entity.HasIndex(x => x.IncidentNumber)
            .IsUnique();

        entity.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Description)
            .HasMaxLength(4000)
            .IsRequired();

        entity.Property(x => x.IntegrationName)
            .HasMaxLength(150)
            .IsRequired();

        entity.HasIndex(x => x.IntegrationName);

        entity.Property(x => x.AffectedSystem)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Environment)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.BusinessImpact)
            .HasMaxLength(2000)
            .IsRequired();

        entity.Property(x => x.TechnicalEvidence)
            .HasMaxLength(8000)
            .IsRequired();

        entity.Property(x => x.ErrorCode)
            .HasMaxLength(100);

        entity.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        entity.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        entity.HasIndex(x => x.CorrelationId);

        entity.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(30);

        entity.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        entity.Property(x => x.KnownRootCauseCategory)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.Property(x => x.ConfirmedResolution)
            .HasMaxLength(4000);

        entity.Property(x => x.ClosedBy)
            .HasMaxLength(200);

        entity.Property(x => x.ClosureSummary)
            .HasMaxLength(4000);

        entity.Property(x => x.FinalRcaInvestigationId);

        entity.Property(x => x.ClosureValidationConfirmed)
            .HasDefaultValue(false);

        entity.HasIndex(x => x.FinalRcaInvestigationId);
    }

    private static void ConfigureRunbook(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Runbook>();

        entity.ToTable("Runbooks");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.RunbookCode)
            .HasMaxLength(50)
            .IsRequired();

        entity.HasIndex(x => x.RunbookCode)
            .IsUnique();

        entity.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.IntegrationName)
            .HasMaxLength(150)
            .IsRequired();

        entity.HasIndex(x => new
        {
            x.IntegrationName,
            x.Category
        });

        entity.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.Property(x => x.Symptoms)
            .HasMaxLength(4000)
            .IsRequired();

        entity.Property(x => x.InvestigationSteps)
            .HasMaxLength(8000)
            .IsRequired();

        entity.Property(x => x.CorrectiveActions)
            .HasMaxLength(8000)
            .IsRequired();

        entity.Property(x => x.PreventiveActions)
            .HasMaxLength(8000)
            .IsRequired();

        entity.Property(x => x.ValidationSteps)
            .HasMaxLength(8000)
            .IsRequired();

        entity.Property(x => x.EscalationTeam)
            .HasMaxLength(150)
            .IsRequired();
    }

    private static void ConfigureInvestigation(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Investigation>();

        entity.ToTable("Investigations");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Objective)
            .HasMaxLength(2000)
            .IsRequired();

        entity.Property(x => x.AiProvider)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.ModelName)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        entity.Property(x => x.RootCauseCategory)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.Property(x => x.Confidence)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.Property(x => x.ResultJson)
            .HasColumnType("TEXT")
            .IsRequired();

        entity.Property(x => x.FailureReason)
            .HasMaxLength(4000);

        entity.HasIndex(x => x.IncidentId);

        entity.HasIndex(x => x.StartedAtUtc);

        entity.HasOne(x => x.Incident)
            .WithMany()
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureToolExecution(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ToolExecution>();

        entity.ToTable("ToolExecutions");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.ToolName)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.ArgumentsJson)
            .HasColumnType("TEXT")
            .IsRequired();

        entity.Property(x => x.ResultJson)
            .HasColumnType("TEXT");

        entity.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        entity.Property(x => x.ErrorMessage)
            .HasMaxLength(4000);

        entity.HasIndex(x => new
        {
            x.InvestigationId,
            x.StepNumber
        })
        .IsUnique();

        entity.HasOne(x => x.Investigation)
            .WithMany(x => x.ToolExecutions)
            .HasForeignKey(x => x.InvestigationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
