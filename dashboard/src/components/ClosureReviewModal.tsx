"use client";

import { FormEvent, useState } from "react";
import { closeIncident } from "@/lib/api";
import { formatTechnicalLabel } from "@/lib/format";
import type {
  IncidentDetails,
  InvestigationHistoryItem,
} from "@/types/incident";

type ClosureReviewModalProps = {
  incident: IncidentDetails;
  investigations: InvestigationHistoryItem[];
  onClose: () => void;
  onClosed: (incident: IncidentDetails) => void;
};

function formatInvestigationDate(value?: string | null) {
  if (!value) {
    return "Completion time unavailable";
  }

  return new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Kolkata",
  }).format(new Date(value));
}

export default function ClosureReviewModal({
  incident,
  investigations,
  onClose,
  onClosed,
}: ClosureReviewModalProps) {
  const eligibleInvestigations = investigations.filter(
    (item) =>
      item.status === "Completed" &&
      item.confidence !== "InsufficientEvidence",
  );

  const [closedBy, setClosedBy] = useState("");
  const [closureSummary, setClosureSummary] = useState("");
  const [finalRcaInvestigationId, setFinalRcaInvestigationId] =
    useState(eligibleInvestigations[0]?.investigationId ?? "");
  const [validationConfirmed, setValidationConfirmed] =
    useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const selectedFinalRca = eligibleInvestigations.find(
    (item) => item.investigationId === finalRcaInvestigationId,
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (incident.status !== "Resolved") {
      setError("Only a resolved incident can be permanently closed.");
      return;
    }

    if (!finalRcaInvestigationId) {
      setError(
        "Select an eligible completed investigation as the final RCA.",
      );
      return;
    }

    if (!validationConfirmed) {
      setError(
        "Confirm that resolution and recovery validation are complete.",
      );
      return;
    }

    try {
      setSaving(true);
      setError("");

      const closedIncident = await closeIncident(
        incident.incidentNumber,
        {
          closedBy: closedBy.trim(),
          closureSummary: closureSummary.trim(),
          finalRcaInvestigationId,
          validationConfirmed,
        },
      );

      onClosed(closedIncident);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "The incident could not be closed.",
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modalBackdrop">
      <section
        className="incidentModal closureReviewModal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="closure-review-title"
      >
        <header className="modalHeader closureModalHeader">
          <div>
            <div className="modalEyebrow">CONTROLLED CLOSURE</div>
            <h2 id="closure-review-title">
              Close {incident.incidentNumber}
            </h2>
            <p>
              Review the final RCA and validation evidence before
              permanently locking this incident.
            </p>
          </div>

          <button
            type="button"
            className="modalCloseButton"
            disabled={saving}
            onClick={onClose}
            aria-label="Close closure review"
          >
            x
          </button>
        </header>

        <div className="closureWarning">
          <strong>This action is permanent.</strong>
          <p>
            After closure, incident editing and additional AI
            investigations will be blocked. History and the approved
            final RCA will remain available.
          </p>
        </div>

        {error ? (
          <div className="modalError" role="alert">
            {error}
          </div>
        ) : null}

        <form className="incidentForm" onSubmit={handleSubmit}>
          <div className="formSection">
            <h3>Closure readiness</h3>
            <div className="closureReadinessGrid">
              <div>
                <span>Current status</span>
                <strong>{incident.status}</strong>
              </div>
              <div>
                <span>Root-cause category</span>
                <strong>
                  {formatTechnicalLabel(
                    incident.knownRootCauseCategory,
                  )}
                </strong>
              </div>
              <div>
                <span>Resolution</span>
                <strong>
                  {incident.confirmedResolution ? "Recorded" : "Missing"}
                </strong>
              </div>
              <div>
                <span>Eligible final RCAs</span>
                <strong>{eligibleInvestigations.length}</strong>
              </div>
            </div>
          </div>

          <div className="formSection">
            <h3>Approved final RCA</h3>

            {eligibleInvestigations.length > 0 ? (
              <>
                <label>
                  <span>Final RCA investigation *</span>
                  <select
                    required
                    value={finalRcaInvestigationId}
                    onChange={(event) =>
                      setFinalRcaInvestigationId(event.target.value)
                    }
                  >
                    {eligibleInvestigations.map((investigation) => (
                      <option
                        key={investigation.investigationId}
                        value={investigation.investigationId}
                      >
                        {formatTechnicalLabel(investigation.confidence)}
                        {" | "}
                        {formatTechnicalLabel(
                          investigation.rootCauseCategory,
                        )}
                        {" | "}
                        {formatInvestigationDate(
                          investigation.completedAtUtc,
                        )}
                      </option>
                    ))}
                  </select>
                </label>

                {selectedFinalRca ? (
                  <div className="selectedFinalRca">
                    <span>Selected investigation</span>
                    <code>{selectedFinalRca.investigationId}</code>
                    <div>
                      <strong>
                        {formatTechnicalLabel(
                          selectedFinalRca.confidence,
                        )}
                      </strong>
                      <strong>
                        {selectedFinalRca.toolExecutionCount} tools
                      </strong>
                      <strong>
                        {formatInvestigationDate(
                          selectedFinalRca.completedAtUtc,
                        )}
                      </strong>
                    </div>
                  </div>
                ) : null}
              </>
            ) : (
              <div className="closureBlockedMessage">
                No completed investigation with sufficient confidence
                is available. Generate and review a final RCA before
                closing the incident.
              </div>
            )}
          </div>

          <div className="formSection">
            <h3>Closure ownership and summary</h3>

            <label>
              <span>Closed by *</span>
              <input
                required
                maxLength={200}
                autoComplete="name"
                placeholder="Enter the closure owner"
                value={closedBy}
                onChange={(event) => setClosedBy(event.target.value)}
              />
              <small className="resolutionHint">
                In an enterprise deployment, this value should come
                from the authenticated user identity.
              </small>
            </label>

            <label>
              <span>Closure summary *</span>
              <textarea
                required
                maxLength={4000}
                className="closureSummaryInput"
                placeholder="Summarize the confirmed cause, corrective action, validation results, monitoring outcome, and remaining follow-up."
                value={closureSummary}
                onChange={(event) =>
                  setClosureSummary(event.target.value)
                }
              />
              <small className="resolutionHint">
                Include what failed, what was changed, and how recovery
                was validated.
              </small>
            </label>
          </div>

          <div className="formSection">
            <label className="closureConfirmation">
              <input
                type="checkbox"
                checked={validationConfirmed}
                onChange={(event) =>
                  setValidationConfirmed(event.target.checked)
                }
              />
              <span>
                I confirm that the incident is resolved, recovery
                validation is complete, and the selected RCA is accurate
                and ready for permanent closure.
              </span>
            </label>
          </div>

          <footer className="modalFooter">
            <button
              type="button"
              className="secondaryButton"
              disabled={saving}
              onClick={onClose}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="closeIncidentConfirmButton"
              disabled={
                saving || eligibleInvestigations.length === 0
              }
            >
              {saving
                ? "Closing incident..."
                : "Close incident permanently"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
