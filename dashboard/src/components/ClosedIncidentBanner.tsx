"use client";

import type { IncidentDetails } from "@/types/incident";

type ClosedIncidentBannerProps = {
  incident: IncidentDetails;
};

function formatClosureTime(value?: string | null) {
  if (!value) {
    return "Not available";
  }

  return new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Kolkata",
  }).format(new Date(value)) + " IST";
}

export default function ClosedIncidentBanner({
  incident,
}: ClosedIncidentBannerProps) {
  if (incident.status !== "Closed") {
    return null;
  }

  return (
    <section className="closedIncidentBanner">
      <div className="closedBannerHeader">
        <div className="closedBannerStatus">
          <span aria-hidden="true">✓</span>
        </div>

        <div>
          <span className="closedBannerEyebrow">
            INCIDENT CLOSURE
          </span>

          <h3>Closed and locked</h3>

          <p>
            This incident is a read-only operational
            record. View the approved final RCA and
            investigation history for further reference.
          </p>
        </div>
      </div>

      <div className="closedBannerMetadata">
        <div>
          <span>Closed by</span>
          <strong>{incident.closedBy ?? "Not available"}</strong>
        </div>

        <div>
          <span>Closed at</span>
          <strong>{formatClosureTime(incident.closedAtUtc)}</strong>
        </div>

        <div>
          <span>Validation</span>
          <strong>
            {incident.closureValidationConfirmed
              ? "Confirmed"
              : "Not confirmed"}
          </strong>
        </div>
      </div>

      <div className="closedBannerFinalRca">
        <span>Approved final RCA</span>

        <code>
          {incident.finalRcaInvestigationId ??
            "Not available"}
        </code>
      </div>

      <div className="closedBannerSummary">
        <span>Closure summary</span>

        <p>
          {incident.closureSummary ??
            "No closure summary was recorded."}
        </p>
      </div>
    </section>
  );
}
