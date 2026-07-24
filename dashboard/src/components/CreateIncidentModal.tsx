"use client";

import { FormEvent, useState } from "react";
import { createIncident } from "@/lib/api";
import { formatTechnicalLabel } from "@/lib/format";
import type {
  CreateIncidentRequest,
  IncidentDetails,
} from "@/types/incident";

type CreateIncidentModalProps = {
  open: boolean;
  onClose: () => void;
  onCreated: (incident: IncidentDetails) => void;
};

const INITIAL_FORM: CreateIncidentRequest = {
  incidentNumber: "",
  title: "",
  description: "",
  integrationName: "Employee Onboarding Integration",
  affectedSystem: "Mock HCM",
  environment: "Production Simulation",
  businessImpact: "",
  technicalEvidence: "",
  errorCode: "",
  errorMessage: "",
  correlationId: "",
  severity: "High",
  status: "Investigating",
  rootCauseCategory: "Unknown",
  confirmedResolution: "",
  detectedAtUtc: null,
  resolvedAtUtc: null,
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

export default function CreateIncidentModal({
  open,
  onClose,
  onCreated,
}: CreateIncidentModalProps) {
  const [form, setForm] =
    useState<CreateIncidentRequest>(INITIAL_FORM);

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  if (!open) {
    return null;
  }

  function updateField(
    field: keyof CreateIncidentRequest,
    value: string,
  ) {
    setForm((current) => ({
      ...current,
      [field]: value,
    }));
  }

  function resetAndClose() {
    if (saving) {
      return;
    }

    setForm(INITIAL_FORM);
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

      const created = await createIncident({
        ...form,
        incidentNumber:
          form.incidentNumber.trim().toUpperCase(),
        title: form.title.trim(),
        description: form.description.trim(),
        integrationName: form.integrationName.trim(),
        affectedSystem: form.affectedSystem.trim(),
        environment: form.environment.trim(),
        businessImpact: form.businessImpact.trim(),
        technicalEvidence: form.technicalEvidence.trim(),
        errorCode: form.errorCode?.trim() || null,
        errorMessage: form.errorMessage?.trim() || null,
        correlationId: form.correlationId?.trim() || null,
        confirmedResolution:
          form.confirmedResolution?.trim() || null,
      });

      setForm(INITIAL_FORM);
      onCreated(created);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "The incident could not be created.",
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
        aria-labelledby="create-incident-title"
      >
        <header className="modalHeader">
          <div>
            <div className="modalEyebrow">
              INCIDENT INTAKE
            </div>

            <h2 id="create-incident-title">
              Create new incident
            </h2>

            <p>
              Enter trusted incident information before
              starting the AI investigation.
            </p>
          </div>

          <button
            type="button"
            className="modalCloseButton"
            disabled={saving}
            onClick={resetAndClose}
            aria-label="Close incident form"
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
                <span>Incident number *</span>

                <input
                  required
                  maxLength={30}
                  placeholder="INC-1008"
                  value={form.incidentNumber}
                  onChange={(event) =>
                    updateField(
                      "incidentNumber",
                      event.target.value,
                    )
                  }
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
                placeholder="Brief incident title"
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
                placeholder="Describe what happened."
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
                  <option value="New">New</option>
                  <option value="Investigating">
                    Investigating
                  </option>
                </select>

                <small className="statusTransitionHint">
                  New incidents are awaiting active analysis.
                  Choose Investigating when evidence collection
                  has already started.
                </small>
              </label>
            </div>

            <label>
              <span>Business impact *</span>

              <textarea
                required
                maxLength={2000}
                placeholder="Explain the impact on users or business processing."
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
                  placeholder="DEPENDENCY_TIMEOUT"
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
                  placeholder="CORR-1008"
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
                placeholder="Exact application or dependency error"
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
                placeholder={
                  "Enter one evidence item per line.\n" +
                  "Requests succeeded before deployment.\n" +
                  "All later calls returned HTTP 500.\n" +
                  "No authentication failures were recorded."
                }
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
              onClick={resetAndClose}
            >
              Cancel
            </button>

            <button
              type="submit"
              className="saveIncidentButton"
              disabled={saving}
            >
              {saving
                ? "Saving incident..."
                : "Save incident"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
