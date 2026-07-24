import type { IncidentDetails } from "@/types/incident";

type IncidentLifecycleProps = {
  incident: IncidentDetails;
};

const LIFECYCLE_STAGES = [
  "New",
  "Investigating",
  "Resolved",
  "Closed",
] as const;

function getLifecycleGuidance(status: string) {
  switch (status) {
    case "New":
      return "Start active analysis and move the incident to Investigating.";

    case "Investigating":
      return "Continue evidence collection until the cause, recovery, and validation are confirmed.";

    case "Resolved":
      return "Generate or review the final RCA, then complete Controlled Closure.";

    case "Closed":
      return "The incident is permanently closed and available as a read-only operational record.";

    default:
      return "Review the current incident status.";
  }
}

export default function IncidentLifecycle({
  incident,
}: IncidentLifecycleProps) {
  const currentIndex = LIFECYCLE_STAGES.indexOf(
    incident.status as (typeof LIFECYCLE_STAGES)[number],
  );

  return (
    <section className="incidentLifecycleCard">
      <div className="incidentLifecycleHeader">
        <div>
          <span className="incidentLifecycleEyebrow">
            INCIDENT LIFECYCLE
          </span>

          <h3>{incident.status}</h3>
        </div>

        <span className="incidentLifecycleStatus">
          {incident.status === "Closed"
            ? "Lifecycle complete"
            : "Active record"}
        </span>
      </div>

      <div
        className="incidentLifecycleTrack"
        aria-label={`Current incident status: ${incident.status}`}
      >
        {LIFECYCLE_STAGES.map((stage, index) => {
          const isCompleted =
            currentIndex >= 0 && index < currentIndex;

          const isCurrent = index === currentIndex;

          return (
            <div
              className="incidentLifecycleStage"
              key={stage}
            >
              <div className="incidentLifecycleStageRow">
                <span
                  className={[
                    "incidentLifecycleNode",
                    isCompleted
                      ? "incidentLifecycleNodeCompleted"
                      : "",
                    isCurrent
                      ? "incidentLifecycleNodeCurrent"
                      : "",
                  ]
                    .filter(Boolean)
                    .join(" ")}
                >
                  {isCompleted ? "✓" : index + 1}
                </span>

                {index < LIFECYCLE_STAGES.length - 1 ? (
                  <span
                    className={
                      index < currentIndex
                        ? "incidentLifecycleLine incidentLifecycleLineCompleted"
                        : "incidentLifecycleLine"
                    }
                  />
                ) : null}
              </div>

              <strong>{stage}</strong>
            </div>
          );
        })}
      </div>

      <p className="incidentLifecycleGuidance">
        {getLifecycleGuidance(incident.status)}
      </p>
    </section>
  );
}
