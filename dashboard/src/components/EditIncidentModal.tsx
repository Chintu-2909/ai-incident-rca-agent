"use client";

import { FormEvent, useState } from "react";
import { updateIncident } from "@/lib/api";
import { formatTechnicalLabel } from "@/lib/format";
import type {
  CreateIncidentRequest,
  IncidentDetails,
} from "@/types/incident";

type EditIncidentModalProps = {
  open: boolean;
  incident: IncidentDetails | null;
  onClose: () => void;
  onUpdated: (incident: IncidentDetails) => void;
};

const ROOT_CAUSE_CATEGORIES = [
  "Unknown",
  "MappingFailure",
  "AuthenticationFailure",
  "AuthorizationFailure",
  "ValidationFailure",
  "DownstreamTimeout",
  "DuplicateProcessing",
  "DatabaseConnectivity",
  "ConfigurationFailure",
  "DownstreamOutage",
];

function getAllowedStatuses(
  currentStatus: string,
) {
  switch (currentStatus) {
    case "New":
      return ["New", "Investigating"];

    case "Investigating":
      return ["Investigating", "Resolved"];

    case "Resolved":
      return ["Resolved", "Investigating"];

    case "Closed":
      return ["Closed"];

    default:
      return [currentStatus];
  }
}

function mapIncidentToForm(
  incident: IncidentDetails,
): CreateIncidentRequest {
  return {
    incidentNumber: incident.incidentNumber,
    title: incident.title,
    description: incident.description,
    integrationName: incident.integrationName,
    affectedSystem: incident.affectedSystem,
    environment: incident.environment,
    businessImpact: incident.businessImpact,
    technicalEvidence: incident.technicalEvidence,
    errorCode: incident.errorCode ?? "",
    errorMessage: incident.errorMessage ?? "",
    correlationId: incident.correlationId ?? "",
    severity: incident.severity,
    status: incident.status,
    rootCauseCategory:
      incident.knownRootCauseCategory,
    confirmedResolution:
      incident.confirmedResolution ?? "",
    detectedAtUtc: incident.detectedAtUtc,
    resolvedAtUtc: incident.resolvedAtUtc ?? null,
  };
}

export default function EditIncidentModal({
  open,
  incident,
  onClose,
  onUpdated,
}: EditIncidentModalProps) {
  const [form, setForm] =
    useState<CreateIncidentRequest | null>(() =>
      incident ? mapIncidentToForm(incident) : null,
    );

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  if (!open || !incident || !form) {
    return null;
  }

  const incidentNumber = incident.incidentNumber;

  function updateField(
    field: keyof CreateIncidentRequest,
    value: string,
  ) {
    setForm((current) => {
      if (!current) {
        return current;
      }

      return Object.assign(
        {},
        current,
        Object.fromEntries([[field, value]]),
      ) as CreateIncidentRequest;
    });
  }

  function closeModal() {
    if (saving) {
      return;
    }

    setError("");
    onClose();
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    try {
      setSaving(true);
      setError("");

      const currentForm = form;

      if (!currentForm) {
        throw new Error(
          "The incident form is not available.",
        );
      }

      const updateRequest: CreateIncidentRequest = {
        ...currentForm,
        incidentNumber:
          currentForm.incidentNumber.trim().toUpperCase(),
        title: currentForm.title.trim(),
        description: currentForm.description.trim(),
        integrationName:
          currentForm.integrationName.trim(),
        affectedSystem:
          currentForm.affectedSystem.trim(),
        environment: currentForm.environment.trim(),
        businessImpact:
          currentForm.businessImpact.trim(),
        technicalEvidence:
          currentForm.technicalEvidence.trim(),
        errorCode:
          currentForm.errorCode?.trim() || null,
        errorMessage:
          currentForm.errorMessage?.trim() || null,
        correlationId:
          currentForm.correlationId?.trim() || null,
        confirmedResolution:
          currentForm.confirmedResolution?.trim() || null,
      };

      const updated = await updateIncident(
        incidentNumber,
        updateRequest,
      );

      onUpdated(updated);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "The incident could not be updated.",
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modalBackdrop">
      <section
        className="incidentModal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="edit-incident-title"
      >
        <header className="modalHeader">
          <div>
            <div className="modalEyebrow">
              INCIDENT EVIDENCE UPDATE
            </div>

            <h2 id="edit-incident-title">
              Edit {incident.incidentNumber}
            </h2>

            <p>
              Add newly verified evidence, update the
              incident status, and record the resolution.
            </p>
          </div>

          <button
            type="button"
            className="modalCloseButton"
            disabled={saving}
            onClick={closeModal}
            aria-label="Close edit incident form"
          >
            ×
          </button>
        </header>

        {error ? (
          <div className="modalError" role="alert">
            {error}
          </div>
        ) : null}

        <form
          className="incidentForm"
          onSubmit={handleSubmit}
        >
          <div className="formSection">
            <h3>Incident identification</h3>

            <div className="modalFieldGrid">
              <label>
                <span>Incident number</span>

                <input
                  readOnly
                  value={form.incidentNumber}
                />
              </label>

              <label>
                <span>Severity *</span>

                <select
                  value={form.severity}
                  onChange={(event) =>
                    updateField(
                      "severity",
                      event.target.value,
                    )
                  }
                >
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">
                    Critical
                  </option>
                </select>
              </label>
            </div>

            <label>
              <span>Title *</span>

              <input
                required
                maxLength={200}
                value={form.title}
                onChange={(event) =>
                  updateField(
                    "title",
                    event.target.value,
                  )
                }
              />
            </label>

            <label>
              <span>Description *</span>

              <textarea
                required
                maxLength={4000}
                value={form.description}
                onChange={(event) =>
                  updateField(
                    "description",
                    event.target.value,
                  )
                }
              />
            </label>
          </div>

          <div className="formSection">
            <h3>Systems and business impact</h3>

            <div className="modalFieldGrid">
              <label>
                <span>Integration name *</span>

                <input
                  required
                  maxLength={150}
                  value={form.integrationName}
                  onChange={(event) =>
                    updateField(
                      "integrationName",
                      event.target.value,
                    )
                  }
                />
              </label>

              <label>
                <span>Affected system *</span>

                <input
                  required
                  maxLength={100}
                  value={form.affectedSystem}
                  onChange={(event) =>
                    updateField(
                      "affectedSystem",
                      event.target.value,
                    )
                  }
                />
              </label>

              <label>
                <span>Environment *</span>

                <input
                  required
                  maxLength={50}
                  value={form.environment}
                  onChange={(event) =>
                    updateField(
                      "environment",
                      event.target.value,
                    )
                  }
                />
              </label>

              <label>
                <span>Status *</span>

                <select
                  value={form.status}
                  onChange={(event) =>
                    updateField(
                      "status",
                      event.target.value,
                    )
                  }
                >
                  {getAllowedStatuses(
                    incident.status,
                  ).map((status) => (
                    <option
                      key={status}
                      value={status}
                    >
                      {status}
                    </option>
                  ))}
                </select>

                <small className="statusTransitionHint">
                  {incident.status === "New"
                    ? "Move the incident to Investigating when active analysis begins."
                    : incident.status === "Investigating"
                      ? "Move to Resolved only after the cause, recovery, and validation are confirmed."
                      : incident.status === "Resolved"
                        ? "Return to Investigating if validation fails. Use Controlled Closure to close the incident."
                        : "Closed incidents are permanently read-only."}
                </small>
              </label>
            </div>

            <label>
              <span>Business impact *</span>

              <textarea
                required
                maxLength={2000}
                value={form.businessImpact}
                onChange={(event) =>
                  updateField(
                    "businessImpact",
                    event.target.value,
                  )
                }
              />
            </label>
          </div>

          <div className="formSection">
            <h3>Technical evidence</h3>

            <div className="modalFieldGrid">
              <label>
                <span>Error code</span>

                <input
                  maxLength={100}
                  value={form.errorCode ?? ""}
                  onChange={(event) =>
                    updateField(
                      "errorCode",
                      event.target.value,
                    )
                  }
                />
              </label>

              <label>
                <span>Correlation ID</span>

                <input
                  maxLength={100}
                  value={form.correlationId ?? ""}
                  onChange={(event) =>
                    updateField(
                      "correlationId",
                      event.target.value,
                    )
                  }
                />
              </label>
            </div>

            <label>
              <span>Error message</span>

              <input
                maxLength={2000}
                value={form.errorMessage ?? ""}
                onChange={(event) =>
                  updateField(
                    "errorMessage",
                    event.target.value,
                  )
                }
              />
            </label>

            <label>
              <span>Technical evidence *</span>

              <textarea
                required
                className="largeEvidenceInput"
                maxLength={8000}
                value={form.technicalEvidence}
                onChange={(event) =>
                  updateField(
                    "technicalEvidence",
                    event.target.value,
                  )
                }
              />
            </label>
          </div>

          <div className="formSection">
            <h3>Investigation result</h3>

            <label>
              <span>Root-cause category</span>

              <select
                value={form.rootCauseCategory}
                onChange={(event) =>
                  updateField(
                    "rootCauseCategory",
                    event.target.value,
                  )
                }
              >
                {ROOT_CAUSE_CATEGORIES.map(
                  (category) => (
                    <option
                      key={category}
                      value={category}
                    >
                      {formatTechnicalLabel(
                        category,
                      )}
                    </option>
                  ),
                )}
              </select>
            </label>

            <label>
              <span>
                Confirmed resolution
                {form.status === "Resolved" ||
                form.status === "Closed"
                  ? " *"
                  : ""}
              </span>

              <textarea
                required={
                  form.status === "Resolved" ||
                  form.status === "Closed"
                }
                maxLength={4000}
                placeholder={
                  form.status === "Resolved" ||
                  form.status === "Closed"
                    ? "Describe the verified corrective action and successful validation."
                    : "Resolution can be added after the investigation is completed."
                }
                value={form.confirmedResolution ?? ""}
                onChange={(event) =>
                  updateField(
                    "confirmedResolution",
                    event.target.value,
                  )
                }
              />

              <small
                className={
                  form.status === "Resolved" ||
                  form.status === "Closed"
                    ? "resolutionHint resolutionHintRequired"
                    : "resolutionHint"
                }
              >
                {form.status === "Resolved" ||
                form.status === "Closed"
                  ? "Required before saving a resolved or closed incident."
                  : "Optional while the incident remains under investigation."}
              </small>
            </label>
          </div>

          <footer className="modalFooter">
            <button
              type="button"
              className="secondaryButton"
              disabled={saving}
              onClick={closeModal}
            >
              Cancel
            </button>

            <button
              type="submit"
              className="saveIncidentButton"
              disabled={saving}
            >
              {saving
                ? "Saving changes..."
                : "Save changes"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
