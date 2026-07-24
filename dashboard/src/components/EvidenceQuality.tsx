import type { IncidentDetails } from "@/types/incident";

type EvidenceQualityProps = {
  incident: IncidentDetails;
};

type EvidenceCheck = {
  label: string;
  available: boolean;
  requiredNow: boolean;
};

function hasValue(value?: string | null) {
  return Boolean(value?.trim());
}

export default function EvidenceQuality({
  incident,
}: EvidenceQualityProps) {
  const isResolved = incident.status === "Resolved";
  const isClosed = incident.status === "Closed";
  const requiresResolution = isResolved || isClosed;

  const checks: EvidenceCheck[] = [
    {
      label: "Error code",
      available: hasValue(incident.errorCode),
      requiredNow: true,
    },
    {
      label: "Correlation ID",
      available: hasValue(incident.correlationId),
      requiredNow: true,
    },
    {
      label: "Technical evidence",
      available: hasValue(incident.technicalEvidence),
      requiredNow: true,
    },
    {
      label: "Business impact",
      available: hasValue(incident.businessImpact),
      requiredNow: true,
    },
    {
      label: "Confirmed resolution",
      available: hasValue(incident.confirmedResolution),
      requiredNow: requiresResolution,
    },
  ];

  const completedCount = checks.filter(
    (check) => check.available,
  ).length;

  const percentage = Math.round(
    (completedCount / checks.length) * 100,
  );

  const requiredEvidenceComplete = checks
    .filter((check) => check.requiredNow)
    .every((check) => check.available);

  const closureComplete =
    isClosed &&
    requiredEvidenceComplete &&
    hasValue(incident.closedBy) &&
    hasValue(incident.closedAtUtc) &&
    hasValue(incident.finalRcaInvestigationId) &&
    incident.closureValidationConfirmed;

  const quality = (() => {
    if (isClosed) {
      return closureComplete
        ? {
            label: "Closure evidence complete",
            stage: "Final RCA approved",
            className: "evidenceQualityClosed",
            description:
              "The incident is closed, validation is confirmed, and the approved final RCA is preserved.",
          }
        : {
            label: "Closure record needs attention",
            stage: "Closure verification required",
            className: "evidenceQualityAttention",
            description:
              "The incident is closed, but one or more closure records are incomplete.",
          };
    }

    if (isResolved) {
      return requiredEvidenceComplete
        ? {
            label: "Ready for final RCA",
            stage: "Closure review eligible",
            className: "evidenceQualityReady",
            description:
              "The resolution is recorded and the evidence is ready for final RCA review and controlled closure.",
          }
        : {
            label: "Resolution evidence incomplete",
            stage: "Final RCA not ready",
            className: "evidenceQualityAttention",
            description:
              "Complete the missing evidence before approving the final RCA or closing the incident.",
          };
    }

    if (incident.status === "Investigating") {
      return requiredEvidenceComplete
        ? {
            label: "Evidence ready for review",
            stage: "RCA remains preliminary",
            className: "evidenceQualityInvestigating",
            description:
              "Core evidence is captured, but findings remain preliminary while the incident is Investigating.",
          }
        : {
            label: "Evidence under investigation",
            stage: "RCA in progress",
            className: "evidenceQualityInvestigating",
            description:
              "Evidence collection is still in progress. Missing identifiers should be added before resolution.",
          };
    }

    return {
      label: "Initial evidence",
      stage: "Investigation not started",
      className: "evidenceQualityInitial",
      description:
        "The incident is New. Capture the initial technical evidence and move it to Investigating when active analysis begins.",
    };
  })();

  return (
    <section
      className={`evidenceQualityCard ${quality.className}`}
    >
      <div className="evidenceQualityHeader">
        <div>
          <span className="evidenceQualityEyebrow">
            EVIDENCE READINESS
          </span>

          <h3>{quality.label}</h3>
          <p>{quality.description}</p>
        </div>

        <div className="evidenceQualityScore">
          <strong>{percentage}%</strong>
          <span>captured</span>
        </div>
      </div>

      <div className="evidenceReadinessMeta">
        <span>
          <strong>Lifecycle</strong>
          {incident.status}
        </span>
        <span>
          <strong>RCA stage</strong>
          {quality.stage}
        </span>
      </div>

      <div
        className="evidenceQualityProgress"
        aria-label={`${percentage} percent evidence captured`}
      >
        <span style={{ width: `${percentage}%` }} />
      </div>

      <div className="evidenceChecklist">
        {checks.map((check) => {
          const optionalForNow =
            !check.requiredNow && !check.available;

          return (
            <div
              className={
                check.available
                  ? isClosed
                    ? "evidenceCheck evidenceCheckAvailable"
                    : isResolved
                      ? "evidenceCheck evidenceCheckResolved"
                      : "evidenceCheck evidenceCheckCaptured"
                  : optionalForNow
                    ? "evidenceCheck evidenceCheckOptional"
                    : "evidenceCheck evidenceCheckMissing"
              }
              key={check.label}
            >
              <span
                className="evidenceCheckIcon"
                aria-hidden="true"
              >
                {check.available
                  ? "✓"
                  : optionalForNow
                    ? "○"
                    : "!"}
              </span>

              <span>{check.label}</span>
            </div>
          );
        })}
      </div>
    </section>
  );
}
