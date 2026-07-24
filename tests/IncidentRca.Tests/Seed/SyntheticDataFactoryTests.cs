using IncidentRca.Domain.Enums;
using IncidentRca.Infrastructure.Seed;

namespace IncidentRca.Tests.Seed;

[TestFixture]
public sealed class SyntheticDataFactoryTests
{
    [Test]
    public void CreateIncidents_ReturnsFiveUniqueSyntheticIncidents()
    {
        var incidents =
            SyntheticDataFactory.CreateIncidents();

        Assert.Multiple(() =>
        {
            Assert.That(
                incidents,
                Has.Count.EqualTo(5));

            Assert.That(
                incidents
                    .Select(x => x.IncidentNumber)
                    .Distinct()
                    .ToList(),
                Has.Count.EqualTo(5));

            Assert.That(
                incidents.All(
                    x => x.Environment ==
                         "Production Simulation"),
                Is.True);

            Assert.That(
                incidents.All(
                    x => !string.IsNullOrWhiteSpace(
                        x.TechnicalEvidence)),
                Is.True);
        });
    }

    [Test]
    public void CreateRunbooks_ReturnsSixUniqueRunbooks()
    {
        var runbooks =
            SyntheticDataFactory.CreateRunbooks();

        Assert.Multiple(() =>
        {
            Assert.That(
                runbooks,
                Has.Count.EqualTo(6));

            Assert.That(
                runbooks
                    .Select(x => x.RunbookCode)
                    .Distinct()
                    .ToList(),
                Has.Count.EqualTo(6));

            Assert.That(
                runbooks.Any(
                    x => x.Category ==
                         RootCauseCategory.MappingFailure),
                Is.True);

            Assert.That(
                runbooks.All(
                    x => !string.IsNullOrWhiteSpace(
                        x.EscalationTeam)),
                Is.True);
        });
    }
}
