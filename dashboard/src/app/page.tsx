"use client";

import { useEffect, useState } from "react";
import CreateIncidentModal from "@/components/CreateIncidentModal";
import EditIncidentModal from "@/components/EditIncidentModal";
import ClosureReviewModal from "@/components/ClosureReviewModal";
import ClosedIncidentBanner from "@/components/ClosedIncidentBanner";
import IncidentLifecycle from "@/components/IncidentLifecycle";
import InvestigationHistory from "@/components/InvestigationHistory";
import EvidenceQuality from "@/components/EvidenceQuality";
import {
  getIncident,
  getIncidents,
  getInvestigation,
  getInvestigationHistory,
  startInvestigation,
} from "@/lib/api";
import { formatTechnicalLabel } from "@/lib/format";
import type {
  IncidentDetails,
  IncidentSummary,
  InvestigationHistoryItem,
  InvestigationResult,
} from "@/types/incident";

const DEFAULT_OBJECTIVE =
  "Prepare an evidence-based RCA, ServiceNow work note, business stakeholder update, corrective actions, preventive actions, validation checklist, and shift-handover note.";

function formatDate(value?: string | null) {
  if (!value) {
    return "Not available";
  }

  return new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function getStatusClass(status?: string) {
  switch (status) {
    case "New":
      return "badge badgeStatusNew";

    case "Investigating":
      return "badge badgeStatusInvestigating";

    case "Resolved":
      return "badge badgeStatusResolved";

    case "Closed":
      return "badge badgeStatusClosed";

    default:
      return "badge badgeStatusNeutral";
  }
}

function getSeverityClass(severity?: string) {
  switch (severity) {
    case "Critical":
      return "badge badgeCritical";
    case "High":
      return "badge badgeHigh";
    case "Medium":
      return "badge badgeMedium";
    default:
      return "badge badgeNeutral";
  }
}

function TextList({
  items,
  emptyText,
}: {
  items: string[];
  emptyText: string;
}) {
  if (items.length === 0) {
    return <p className="emptyText">{emptyText}</p>;
  }

  return (
    <ul className="itemList">
      {items.map((item, index) => (
        <li key={`${index}-${item}`}>{item}</li>
      ))}
    </ul>
  );
}

function ContentCard({
  title,
  subtitle,
  content,
  onCopy,
}: {
  title: string;
  subtitle?: string;
  content: string;
  onCopy: (content: string, label: string) => void;
}) {
  return (
    <article className="contentCard">
      <div className="contentCardHeader">
        <div>
          <h3>{title}</h3>
          {subtitle ? <p>{subtitle}</p> : null}
        </div>

        <button
          className="copyButton"
          type="button"
          onClick={() => onCopy(content, title)}
        >
          Copy
        </button>
      </div>

      <div className="contentText">{content}</div>
    </article>
  );
}

export default function Home() {
  const [incidents, setIncidents] = useState<IncidentSummary[]>([]);
  const [selectedNumber, setSelectedNumber] = useState("");
  const [selectedIncident, setSelectedIncident] =
    useState<IncidentDetails | null>(null);
  const [objective, setObjective] = useState(DEFAULT_OBJECTIVE);
  const [result, setResult] = useState<InvestigationResult | null>(null);
  const [pendingGeneratedReport, setPendingGeneratedReport] =
    useState<InvestigationResult | null>(null);
  const [loadingIncidents, setLoadingIncidents] = useState(true);
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [investigating, setInvestigating] = useState(false);
  const [error, setError] = useState("");
  const [copiedLabel, setCopiedLabel] = useState("");
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [closureModalOpen, setClosureModalOpen] = useState(false);
  const [history, setHistory] =
    useState<InvestigationHistoryItem[]>([]);
  const [loadingHistory, setLoadingHistory] =
    useState(false);
  const [loadingSavedReport, setLoadingSavedReport] =
    useState(false);
  const [viewedSavedReportId, setViewedSavedReportId] =
    useState<string | null>(null);
  const [historyOpen, setHistoryOpen] = useState(false);

  useEffect(() => {
    async function loadIncidents() {
      try {
        setLoadingIncidents(true);
        setError("");

        const data = await getIncidents();
        setIncidents(data);

        if (data.length > 0) {
          setSelectedNumber(data[0].incidentNumber);
        }
      } catch (requestError) {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Unable to load incidents.",
        );
      } finally {
        setLoadingIncidents(false);
      }
    }

    void loadIncidents();
  }, []);

  useEffect(() => {
    if (!selectedNumber) {
      return;
    }

    async function loadIncidentDetails() {
      try {
        setLoadingDetails(true);
        setError("");
        setResult(null);

        const incident = await getIncident(selectedNumber);
        setSelectedIncident(incident);
        await loadHistory(selectedNumber);
      } catch (requestError) {
        setSelectedIncident(null);
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Unable to load incident details.",
        );
      } finally {
        setLoadingDetails(false);
      }
    }

    void loadIncidentDetails();
  }, [selectedNumber]);

  function handleIncidentCreated(
    createdIncident: IncidentDetails,
  ) {
    const createdSummary: IncidentSummary = {
      id: createdIncident.id,
      incidentNumber: createdIncident.incidentNumber,
      title: createdIncident.title,
      integrationName: createdIncident.integrationName,
      affectedSystem: createdIncident.affectedSystem,
      environment: createdIncident.environment,
      severity: createdIncident.severity,
      status: createdIncident.status,
      knownRootCauseCategory:
        createdIncident.knownRootCauseCategory,
      detectedAtUtc: createdIncident.detectedAtUtc,
    };

    setIncidents((current) => [
      createdSummary,
      ...current.filter(
        (incident) =>
          incident.incidentNumber !==
          createdIncident.incidentNumber,
      ),
    ]);

    setSelectedIncident(createdIncident);
    setSelectedNumber(createdIncident.incidentNumber);
    setResult(null);
    setError("");
    setCreateModalOpen(false);
    setHistoryOpen(false);
  }

  function handleIncidentUpdated(
    updatedIncident: IncidentDetails,
  ) {
    const updatedSummary: IncidentSummary = {
      id: updatedIncident.id,
      incidentNumber: updatedIncident.incidentNumber,
      title: updatedIncident.title,
      integrationName: updatedIncident.integrationName,
      affectedSystem: updatedIncident.affectedSystem,
      environment: updatedIncident.environment,
      severity: updatedIncident.severity,
      status: updatedIncident.status,
      knownRootCauseCategory:
        updatedIncident.knownRootCauseCategory,
      detectedAtUtc: updatedIncident.detectedAtUtc,
    };

    setIncidents((current) =>
      current.map((incident) =>
        incident.incidentNumber ===
        updatedIncident.incidentNumber
          ? updatedSummary
          : incident,
      ),
    );

    setSelectedIncident(updatedIncident);
    setSelectedNumber(updatedIncident.incidentNumber);
    setResult(null);
    setError("");
    setEditModalOpen(false);
    setHistoryOpen(false);
  }

  function handleIncidentClosed(
    closedIncident: IncidentDetails,
  ) {
    const closedSummary: IncidentSummary = {
      id: closedIncident.id,
      incidentNumber: closedIncident.incidentNumber,
      title: closedIncident.title,
      integrationName: closedIncident.integrationName,
      affectedSystem: closedIncident.affectedSystem,
      environment: closedIncident.environment,
      severity: closedIncident.severity,
      status: closedIncident.status,
      knownRootCauseCategory:
        closedIncident.knownRootCauseCategory,
      detectedAtUtc: closedIncident.detectedAtUtc,
    };

    setIncidents((current) =>
      current.map((incident) =>
        incident.incidentNumber === closedIncident.incidentNumber
          ? closedSummary
          : incident,
      ),
    );

    setSelectedIncident(closedIncident);
    setSelectedNumber(closedIncident.incidentNumber);
    setClosureModalOpen(false);
    setEditModalOpen(false);
    setResult(null);
    setPendingGeneratedReport(null);
    setViewedSavedReportId(null);
    setHistoryOpen(true);
    void loadHistory(closedIncident.incidentNumber);
  }

  async function loadHistory(
    incidentNumber: string,
  ) {
    try {
      setLoadingHistory(true);

      const items =
        await getInvestigationHistory(
          incidentNumber,
        );

      setHistory(items);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to load investigation history.",
      );
    } finally {
      setLoadingHistory(false);
    }
  }

  async function handleViewSavedReport(
    investigationId: string,
  ) {
    try {
      setLoadingSavedReport(true);
      setError("");

      const savedReport =
        await getInvestigation(investigationId);

      setResult(savedReport);
      setViewedSavedReportId(investigationId);

      window.setTimeout(() => {
        document
          .querySelector(".resultsSection")
          ?.scrollIntoView({
            behavior: "smooth",
            block: "start",
          });
      }, 50);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to load the saved report.",
      );
    } finally {
      setLoadingSavedReport(false);
    }
  }

  function handleViewGeneratedReport() {
    if (!pendingGeneratedReport) {
      return;
    }

    setResult(pendingGeneratedReport);
    setViewedSavedReportId(
      pendingGeneratedReport.investigationId,
    );

    window.setTimeout(() => {
      scrollToSection("rca-report");
    }, 100);
  }

  function handleOpenGeneratedHistory() {
    setHistoryOpen(true);

    window.setTimeout(() => {
      scrollToSection("history-control");
    }, 100);
  }

  function handleDismissGeneratedReport() {
    setPendingGeneratedReport(null);
  }

  function handleCloseSavedReport() {
    setResult(null);
    setViewedSavedReportId(null);

    window.setTimeout(() => {
      document
        .querySelector(".historyToggleSection")
        ?.scrollIntoView({
          behavior: "smooth",
          block: "start",
        });
    }, 50);
  }

  function scrollToSection(sectionId: string) {
    document
      .getElementById(sectionId)
      ?.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
  }

  function handleOpenHistoryFromNavigation() {
    setHistoryOpen(true);

    window.setTimeout(() => {
      scrollToSection("history-control");
    }, 50);
  }

  async function handleReportNavigation(
    targetSection: "rca-report" | "report-actions",
  ) {
    const approvedFinalRcaId =
      selectedIncident?.status === "Closed"
        ? selectedIncident.finalRcaInvestigationId
        : null;

    const approvedFinalRcaIsOpen =
      Boolean(approvedFinalRcaId) &&
      viewedSavedReportId === approvedFinalRcaId;

    if (
      result &&
      (!approvedFinalRcaId || approvedFinalRcaIsOpen)
    ) {
      scrollToSection(targetSection);
      return;
    }

    const latestCompletedInvestigation =
      history.find(
        (item) => item.status === "Completed",
      );

    const investigationId =
      approvedFinalRcaId ??
      latestCompletedInvestigation?.investigationId;

    if (!investigationId) {
      setError(
        selectedIncident?.status === "Closed"
          ? "The approved final RCA is not available."
          : "No completed investigation report is available.",
      );
      return;
    }

    try {
      setLoadingSavedReport(true);
      setError("");

      const savedReport =
        await getInvestigation(investigationId);

      setResult(savedReport);
      setViewedSavedReportId(investigationId);

      window.setTimeout(() => {
        scrollToSection(targetSection);
      }, 100);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to load the investigation report.",
      );
    } finally {
      setLoadingSavedReport(false);
    }
  }

  async function handleInvestigation() {
    if (!selectedNumber || !objective.trim()) {
      setError("Select an incident and enter an investigation objective.");
      return;
    }

    try {
      setInvestigating(true);
      setError("");
      setResult(null);
      setViewedSavedReportId(null);
      setPendingGeneratedReport(null);

      const investigation = await startInvestigation({
        incidentNumber: selectedNumber,
        objective: objective.trim(),
      });

      setPendingGeneratedReport(investigation);
      setResult(null);
      setViewedSavedReportId(null);

      await loadHistory(selectedNumber);

      window.setTimeout(() => {
        document
          .querySelector(".investigationSuccessPanel")
          ?.scrollIntoView({
            behavior: "smooth",
            block: "center",
          });
      }, 100);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "The investigation could not be completed.",
      );
    } finally {
      setInvestigating(false);
    }
  }

  function buildCompleteReport(
    investigation: InvestigationResult,
  ) {
    function formatList(
      title: string,
      items: string[],
      emptyText: string,
    ) {
      const values =
        items.length > 0
          ? items.map(
              (item, index) =>
                `${index + 1}. ${item}`,
            )
          : [emptyText];

      return [
        title,
        ...values,
      ].join("\n");
    }

    return [
      "AI INCIDENT HANDOFF & RCA REPORT",
      "================================",
      "",
      `Incident: ${investigation.incidentNumber}`,
      `Investigation ID: ${investigation.investigationId}`,
      `Classification: ${formatTechnicalLabel(
        investigation.classification,
      )}`,
      `Root-cause category: ${formatTechnicalLabel(
        investigation.rootCauseCategory,
      )}`,
      `Confidence: ${formatTechnicalLabel(
        investigation.confidence,
      )}`,
      `Generated: ${formatDate(
        investigation.generatedAtUtc,
      )}`,
      "",
      "PROBABLE ROOT CAUSE",
      investigation.probableRootCause,
      "",
      formatList(
        "CONFIRMED FACTS",
        investigation.confirmedFacts,
        "No confirmed facts were recorded.",
      ),
      "",
      formatList(
        "ASSUMPTIONS",
        investigation.assumptions,
        "No unsupported assumptions were identified.",
      ),
      "",
      formatList(
        "MISSING INFORMATION",
        investigation.missingInformation,
        "No critical information is missing.",
      ),
      "",
      "TECHNICAL SUMMARY",
      investigation.technicalSummary,
      "",
      "SERVICENOW WORK NOTE",
      investigation.serviceNowWorkNote,
      "",
      "STAKEHOLDER UPDATE",
      investigation.stakeholderUpdate,
      "",
      "ROOT-CAUSE ANALYSIS",
      investigation.rootCauseAnalysis,
      "",
      formatList(
        "CORRECTIVE ACTIONS",
        investigation.correctiveActions,
        "No corrective actions were recorded.",
      ),
      "",
      formatList(
        "PREVENTIVE ACTIONS",
        investigation.preventiveActions,
        "No preventive actions were recorded.",
      ),
      "",
      formatList(
        "VALIDATION CHECKLIST",
        investigation.validationChecklist,
        "No validation steps were recorded.",
      ),
      "",
      "SHIFT-HANDOVER NOTE",
      investigation.shiftHandoverNote,
      "",
      formatList(
        "TRUSTED TECHNICAL EVIDENCE",
        investigation.evidence,
        "No trusted technical evidence was recorded.",
      ),
      "",
      formatList(
        "CONTROLLED TOOL EXECUTION",
        investigation.toolsExecuted.map(
          (tool) =>
            `${tool.toolName}: ${tool.summary}`,
        ),
        "No controlled tool executions were recorded.",
      ),
    ].join("\n");
  }

  async function handleCopy(content: string, label: string) {
    try {
      await navigator.clipboard.writeText(content);
      setCopiedLabel(label);

      window.setTimeout(() => {
        setCopiedLabel("");
      }, 2000);
    } catch {
      setError("The content could not be copied to the clipboard.");
    }
  }

  return (
    <main className="appShell">
      <header className="hero">
        <div className="heroGlow heroGlowOne" />
        <div className="heroGlow heroGlowTwo" />

        <div className="heroContent">
          <div>
            <div className="eyebrow">
              LOCAL-FIRST AGENTIC OPERATIONS
            </div>

            <h1>AI Incident Handoff &amp; RCA Agent</h1>

            <p className="heroDescription">
              Transform trusted incident evidence into structured RCA
              reports, ServiceNow work notes, stakeholder updates, and
              operational handover documentation.
            </p>

            <div className="technologyRow">
              <span>ASP.NET Core</span>
              <span>Next.js</span>
              <span>SQLite</span>
              <span>Ollama</span>
              <span>Phi-4 Mini</span>
            </div>
          </div>

          <div className="systemStatus">
            <span className="statusDot" />
            <div>
              <strong>Local AI ready</strong>
              <small>Controlled tools and evidence grounding</small>
            </div>
          </div>
        </div>
      </header>

      <section className="statsGrid">
        <article className="statCard">
          <span>Total incidents</span>
          <strong>{incidents.length}</strong>
          <small>Safe portfolio demonstration data</small>
        </article>

        <article className="statCard">
          <span>Controlled tools</span>
          <strong>4</strong>
          <small>Backend-governed agent actions</small>
        </article>

        <article className="statCard">
          <span>Automated tests</span>
          <strong>20</strong>
          <small>All currently passing</small>
        </article>

        <article className="statCard">
          <span>AI provider</span>
          <strong className="providerValue">Ollama</strong>
          <small>Amazon Bedrock ready architecture</small>
        </article>
      </section>

      <nav
        className="dashboardNavigation"
        aria-label="Dashboard sections"
      >
        <div className="dashboardNavigationInner">
          <button
            type="button"
            onClick={() =>
              scrollToSection("incident-evidence")
            }
          >
            <span>01</span>
            Incident evidence
          </button>

          <button
            type="button"
            disabled={
              !selectedNumber ||
              loadingHistory ||
              history.length === 0
            }
            onClick={handleOpenHistoryFromNavigation}
          >
            <span>02</span>
            History
            {history.length > 0 ? (
              <small>{history.length}</small>
            ) : null}
          </button>

          <button
            type="button"
            disabled={
              !result &&
              !history.some(
                (item) => item.status === "Completed",
              )
            }
            onClick={() =>
              void handleReportNavigation(
                "rca-report",
              )
            }
          >
            <span>03</span>
            RCA report
          </button>

          <button
            type="button"
            disabled={
              !result &&
              !history.some(
                (item) => item.status === "Completed",
              )
            }
            onClick={() =>
              void handleReportNavigation(
                "report-actions",
              )
            }
          >
            <span>04</span>
            Actions
          </button>
        </div>
      </nav>

      {error ? (
        <section className="errorBanner" role="alert">
          <strong>Unable to complete the request</strong>
          <span>{error}</span>
        </section>
      ) : null}

      <section className="workspaceGrid">
        <aside className="controlPanel">
          <div className="sectionHeading">
            <span className="sectionNumber">01</span>
            <div>
              <h2>Investigation setup</h2>
              <p>Select an incident and define the agent objective.</p>
            </div>
          </div>

          <button
            type="button"
            className="createIncidentButton"
            disabled={investigating}
            onClick={() => setCreateModalOpen(true)}
          >
            <span>＋</span>
            Create new incident
          </button>

          <label className="fieldLabel" htmlFor="incident">
            Incident
          </label>

          <select
            id="incident"
            className="fieldControl"
            value={selectedNumber}
            disabled={loadingIncidents || investigating}
            onChange={(event) => {
              setSelectedNumber(event.target.value);
              setHistoryOpen(false);
              setViewedSavedReportId(null);
              setResult(null);
              setPendingGeneratedReport(null);
            }}
          >
            {loadingIncidents ? (
              <option>Loading incidents...</option>
            ) : null}

            {incidents.map((incident) => (
              <option
                key={incident.incidentNumber}
                value={incident.incidentNumber}
              >
                {incident.incidentNumber} | {incident.title}
              </option>
            ))}
          </select>

          <label className="fieldLabel" htmlFor="objective">
            Investigation objective
          </label>

          <textarea
            id="objective"
            className="objectiveInput"
            value={objective}
            maxLength={2000}
            disabled={investigating}
            onChange={(event) => setObjective(event.target.value)}
          />

          <div className="characterCount">
            {objective.length} / 2000 characters
          </div>

          <button
            type="button"
            className="investigateButton"
            disabled={
              investigating ||
              loadingIncidents ||
              !selectedNumber ||
              !objective.trim() ||
              selectedIncident?.status === "Closed"
            }
            onClick={handleInvestigation}
          >
            {investigating ? (
              <>
                <span className="spinner" />
                Agent is investigating...
              </>
            ) : (
              <>
                <span className="buttonIcon">✦</span>
                {selectedIncident?.status === "Closed"
                  ? "Incident closed"
                  : selectedIncident?.status === "Resolved"
                    ? "Generate final RCA"
                    : selectedIncident?.status === "Investigating"
                      ? "Continue AI investigation"
                      : "Start AI investigation"}
              </>
            )}
          </button>

          <div className="safetyNote">
            <strong>Evidence-grounded workflow</strong>
            <p>
              The AI model cannot access SQLite, files, or shell commands.
              ASP.NET Core executes only four approved read-only tools.
            </p>
          </div>
        </aside>

        <section id="incident-evidence" className="incidentPanel">
          <div className="sectionHeading">
            <span className="sectionNumber">02</span>
            <div>
              <h2>Incident evidence</h2>
              <p>Trusted data retrieved from the synthetic incident store.</p>
            </div>
          </div>

          {loadingDetails ? (
            <div className="loadingPanel">
              <span className="spinner darkSpinner" />
              Loading incident evidence...
            </div>
          ) : selectedIncident ? (
            <>
              <div className="incidentTitleRow">
                <div>
                  <div className="incidentNumber">
                    {selectedIncident.incidentNumber}
                  </div>
                  <h3>{selectedIncident.title}</h3>
                </div>

                <div className="incidentHeaderActions">
                  {selectedIncident.status === "Resolved" ? (
                    <button
                      type="button"
                      className="closeIncidentHeaderButton"
                      disabled={investigating}
                      onClick={() => setClosureModalOpen(true)}
                    >
                      Close incident
                      <span aria-hidden="true">→</span>
                    </button>
                  ) : null}

                  {selectedIncident.status === "Closed" ? (
                    <button
                      type="button"
                      className="closedLockedButton"
                      disabled
                    >
                      Closed and locked
                    </button>
                  ) : null}

                  <button
                    type="button"
                    className="viewReportButton headerEditButton"
                    disabled={
                      investigating ||
                      selectedIncident.status === "Closed"
                    }
                    onClick={() => setEditModalOpen(true)}
                    aria-label={`Update ${selectedIncident.incidentNumber}`}
                  >
                    <span>Edit incident</span>
                    <span
                      className="editButtonArrow"
                      aria-hidden="true"
                    >
                      →
                    </span>
                  </button>

                  <div className="badgeRow">
                    <span
                      className={getSeverityClass(
                        selectedIncident.severity,
                      )}
                    >
                    {selectedIncident.severity}
                  </span>
                    <span
                      className={getStatusClass(
                        selectedIncident.status,
                      )}
                    >
                      {selectedIncident.status}
                    </span>
                  </div>
                </div>
              </div>

              <IncidentLifecycle
                incident={selectedIncident}
              />

              <ClosedIncidentBanner
                incident={selectedIncident}
              />

              <div className="detailGrid">
                <div>
                  <span>Integration</span>
                  <strong>{selectedIncident.integrationName}</strong>
                </div>
                <div>
                  <span>Affected system</span>
                  <strong>{selectedIncident.affectedSystem}</strong>
                </div>
                <div>
                  <span>Environment</span>
                  <strong>{selectedIncident.environment}</strong>
                </div>
                <div>
                  <span>Correlation ID</span>
                  <strong>
                    {selectedIncident.correlationId ?? "Not available"}
                  </strong>
                </div>
                <div>
                  <span>Error code</span>
                  <strong>
                    {selectedIncident.errorCode ?? "Not available"}
                  </strong>
                </div>
                <div>
                  <span>Detected</span>
                  <strong>
                    {formatDate(selectedIncident.detectedAtUtc)}
                  </strong>
                </div>
              </div>

              <EvidenceQuality
                incident={selectedIncident}
              />

              <div className="evidenceBlock">
                <span>Business impact</span>
                <p>{selectedIncident.businessImpact}</p>
              </div>

              <div className="evidenceBlock">
                <span>Technical evidence</span>
                <div className="technicalEvidence">
                  {selectedIncident.technicalEvidence}
                </div>
              </div>

              <div className="evidenceBlock resolutionBlock">
                <span>Recorded resolution</span>
                <p>
                  {selectedIncident.confirmedResolution ??
                    "Resolution has not been confirmed."}
                </p>
              </div>
            </>
          ) : (
            <div className="emptyPanel">
              Select an incident to view the available evidence.
            </div>
          )}
        </section>
      </section>

      {selectedNumber ? (
        <section id="history-control" className="historyToggleSection">
          <button
            type="button"
            className={
              historyOpen
                ? "historyToggleButton historyToggleButtonOpen"
                : "historyToggleButton"
            }
            disabled={loadingHistory || history.length === 0}
            aria-expanded={historyOpen}
            onClick={() =>
              setHistoryOpen((current) => !current)
            }
          >
            <span className="historyToggleContent">
              <span className="historyToggleEyebrow">
                AUDIT TRAIL
              </span>

              <strong>
                {history.length === 0
                  ? "No investigation history"
                  : historyOpen
                    ? "Hide investigation history"
                    : `View investigation history (${history.length})`}
              </strong>
            </span>

            <span
              className="historyToggleArrow"
              aria-hidden="true"
            >
              {historyOpen ? "↑" : "→"}
            </span>
          </button>
        </section>
      ) : null}

      {historyOpen && selectedNumber ? (
        <InvestigationHistory
          incidentNumber={selectedNumber}
          history={history}
          loading={
            loadingHistory ||
            loadingSavedReport
          }
          viewedReportId={viewedSavedReportId}
          finalRcaInvestigationId={
            selectedIncident?.finalRcaInvestigationId
          }
          onRefresh={() =>
            void loadHistory(selectedNumber)
          }
          onViewReport={(investigationId) =>
            void handleViewSavedReport(
              investigationId,
            )
          }
        />
      ) : null}

      {investigating ? (
        <section className="agentProgress">
          <div className="progressAnimation">
            <span />
            <span />
            <span />
          </div>
          <div>
            <h2>Agent investigation in progress</h2>
            <p>
              Retrieving incident context, finding related incidents,
              selecting a runbook, validating evidence, and generating
              the structured RCA with Phi-4 Mini.
            </p>
          </div>
        </section>
      ) : null}

      {pendingGeneratedReport ? (
        <section
          className="investigationSuccessPanel"
          role="status"
        >
          <div className="investigationSuccessIcon">
            <span aria-hidden="true">✓</span>
          </div>

          <div className="investigationSuccessContent">
            <span className="investigationSuccessEyebrow">
              INVESTIGATION COMPLETED
            </span>

            <h2>Report generated and saved</h2>

            <p>
              A{" "}
              <strong>
                {formatTechnicalLabel(
                  pendingGeneratedReport.confidence,
                )}
              </strong>{" "}
              confidence{" "}
              <strong>
                {formatTechnicalLabel(
                  pendingGeneratedReport.classification,
                )}
              </strong>{" "}
              report was generated for{" "}
              <strong>
                {pendingGeneratedReport.incidentNumber}
              </strong>{" "}
              and saved to Investigation History.
            </p>

            <div className="investigationSuccessMetadata">
              <span>
                Investigation ID
                <code>
                  {pendingGeneratedReport.investigationId}
                </code>
              </span>

              <span>
                Controlled tools
                <strong>
                  {pendingGeneratedReport.toolsExecuted.length}
                </strong>
              </span>
            </div>
          </div>

          <div className="investigationSuccessActions">
            <button
              type="button"
              className="viewGeneratedReportButton"
              onClick={handleViewGeneratedReport}
            >
              View generated report
              <span aria-hidden="true">→</span>
            </button>

            <button
              type="button"
              className="openGeneratedHistoryButton"
              onClick={handleOpenGeneratedHistory}
            >
              Open history
            </button>

            <button
              type="button"
              className="dismissGeneratedReportButton"
              onClick={handleDismissGeneratedReport}
            >
              Dismiss
            </button>
          </div>
        </section>
      ) : null}

      {result ? (
        <section id="rca-report" className="resultsSection">
          {viewedSavedReportId ? (
            <div className="savedReportNotice">
              <div>
                <span className="savedReportEyebrow">
                  {selectedIncident?.status === "Closed" &&
                  viewedSavedReportId ===
                    selectedIncident.finalRcaInvestigationId
                    ? "APPROVED FINAL RCA"
                    : "HISTORICAL REPORT"}
                </span>

                <strong>
                  {selectedIncident?.status === "Closed" &&
                  viewedSavedReportId ===
                    selectedIncident.finalRcaInvestigationId
                    ? "Viewing approved closure report"
                    : "Viewing saved investigation"}
                </strong>

                <code>{viewedSavedReportId}</code>
              </div>

              <button
                type="button"
                className="closeSavedReportButton"
                onClick={handleCloseSavedReport}
              >
                Close saved report
                <span aria-hidden="true">×</span>
              </button>
            </div>
          ) : null}
          <div className="resultsHeader">
            <div>
              <div className="eyebrow darkEyebrow">
                {selectedIncident?.status === "Closed"
                  ? "APPROVED CLOSURE RECORD"
                  : selectedIncident?.status === "Resolved"
                    ? "CLOSURE PREPARATION"
                    : selectedIncident?.status === "Investigating"
                      ? "INVESTIGATION IN PROGRESS"
                      : "INITIAL ASSESSMENT"}
              </div>

              <h2>
                {selectedIncident?.status === "Closed"
                  ? "Approved Final RCA"
                  : selectedIncident?.status === "Resolved"
                    ? "Final RCA draft"
                    : selectedIncident?.status === "Investigating"
                      ? "Preliminary RCA"
                      : "Initial investigation assessment"}
              </h2>
              <p>
                Investigation ID:{" "}
                <code>{result.investigationId}</code>
              </p>

              <div className="reportMetadata">
                <span>
                  <strong>Generated</strong>
                  {formatDate(result.generatedAtUtc)}
                </span>

                <span>
                  <strong>Controlled tools</strong>
                  {result.toolsExecuted.length} executed
                </span>

                <span>
                  <strong>AI runtime</strong>
                  Local Ollama
                </span>

                <span
                  className={
                    viewedSavedReportId
                      ? "reportSourceBadge reportSourceHistorical"
                      : "reportSourceBadge reportSourceFresh"
                  }
                >
                  {viewedSavedReportId
                    ? "Historical report"
                    : "Fresh investigation"}
                </span>
              </div>
            </div>

            <div className="resultHeaderActions">
              <button
                type="button"
                className="copyCompleteReportButton"
                onClick={() =>
                  void handleCopy(
                    buildCompleteReport(result),
                    "complete report",
                  )
                }
              >
                <span>
                  {copiedLabel === "complete report"
                    ? "Copied complete report"
                    : "Copy complete report"}
                </span>

                <span
                  className="copyCompleteReportArrow"
                  aria-hidden="true"
                >
                  {copiedLabel === "complete report"
                    ? "✓"
                    : "→"}
                </span>
              </button>

              <div className="resultBadges">
                <span className="resultBadge">
                {formatTechnicalLabel(
                  result.classification,
                )}
              </span>
                <span className="confidenceBadge">
                  {formatTechnicalLabel(
                    result.confidence,
                  )} confidence
                </span>
              </div>
            </div>
          </div>

          <article className="rootCauseCard">
            <span>
              {result.confidence === "InsufficientEvidence"
                ? "Current investigation hypothesis"
                : selectedIncident?.status === "Closed"
                  ? "Approved root cause"
                  : selectedIncident?.status === "Resolved"
                    ? "Final root cause draft"
                    : selectedIncident?.status === "Investigating"
                      ? "Preliminary root-cause finding"
                      : "Initial root-cause hypothesis"}
            </span>

            <p>{result.probableRootCause}</p>
          </article>

          <div className="resultColumns">
            <article className="listCard successCard">
              <h3>Confirmed facts</h3>
              <TextList
                items={result.confirmedFacts}
                emptyText="No confirmed facts are available. Add verified technical evidence before treating the investigation findings as factual."
              />
            </article>

            <article className="listCard warningCard">
              <h3>Assumptions</h3>
              <TextList
                items={result.assumptions}
                emptyText="No unsupported assumptions were identified. The report remains grounded in the available incident evidence."
              />
            </article>

            <article className="listCard neutralCard">
              <h3>Missing information</h3>
              <TextList
                items={result.missingInformation}
                emptyText="No critical evidence gaps were identified. The incident contains the core information required for the RCA."
              />
            </article>
          </div>

          <article className="timelineCard">
            <div className="cardTitleRow">
              <div>
                <h3>Controlled tool execution</h3>
                <p>Every backend action is visible and auditable.</p>
              </div>
              <span>{result.toolsExecuted.length} tools</span>
            </div>

            <div className="timeline">
              {result.toolsExecuted.map((tool) => (
                <div className="timelineItem" key={tool.stepNumber}>
                  <div
                    className={
                      tool.successful
                        ? "timelineIcon successTimelineIcon"
                        : "timelineIcon failedTimelineIcon"
                    }
                  >
                    {tool.successful ? "✓" : "!"}
                  </div>
                  <div>
                    <strong>
                      {tool.stepNumber}. {tool.toolName}
                    </strong>
                    <p>{tool.summary}</p>
                  </div>
                </div>
              ))}
            </div>
          </article>

          <div className="contentGrid">
            <ContentCard
              title="Technical summary"
              content={result.technicalSummary}
              onCopy={handleCopy}
            />

            <ContentCard
              title="ServiceNow work note"
              subtitle="Copy-ready technical incident update"
              content={result.serviceNowWorkNote}
              onCopy={handleCopy}
            />

            <ContentCard
              title="Stakeholder update"
              subtitle="Business-friendly communication"
              content={result.stakeholderUpdate}
              onCopy={handleCopy}
            />

            <ContentCard
              title="Root-cause analysis"
              subtitle="Evidence, impact, correction, and prevention"
              content={result.rootCauseAnalysis}
              onCopy={handleCopy}
            />

            <ContentCard
              title="Shift handover note"
              subtitle="Operational continuity summary"
              content={result.shiftHandoverNote}
              onCopy={handleCopy}
            />
          </div>

          <div id="report-actions" className="actionGrid">
            <article className="actionCard">
              <h3>Corrective actions</h3>
              <TextList
                items={result.correctiveActions}
                emptyText={
                  result.confidence === "InsufficientEvidence"
                    ? "Corrective actions are unavailable until the root cause is confirmed. Complete the evidence checklist and update the incident before approving remediation."
                    : "No corrective actions were recorded for this investigation. Review the matching runbook and confirmed resolution."
                }
              />
            </article>

            <article className="actionCard">
              <h3>Preventive actions</h3>
              <TextList
                items={result.preventiveActions}
                emptyText={
                  result.confidence === "InsufficientEvidence"
                    ? "Preventive actions require a confirmed failure mechanism. Collect the missing evidence before defining long-term controls."
                    : "No preventive actions were recorded. Review the approved runbook and consider controls that prevent recurrence."
                }
              />
            </article>

            <article className="actionCard">
              <h3>Validation checklist</h3>
              <TextList
                items={result.validationChecklist}
                emptyText={
                  result.confidence === "InsufficientEvidence"
                    ? "No validation procedure can be approved yet. First collect the missing identifiers, responses, and transaction evidence."
                    : "No validation steps were recorded. Confirm recovery through the approved operational runbook."
                }
              />
            </article>
          </div>

          <article className="evidenceCard">
            <h3>Trusted technical evidence</h3>
            <TextList
              items={result.evidence}
              emptyText="No trusted technical evidence is available. Update the incident with verified logs, identifiers, dependency responses, and transaction findings."
            />
          </article>
        </section>
      ) : (
        <section className="readyPanel">
          <div className="readyIcon">✦</div>
          <div>
            <h2>Ready for investigation</h2>
            <p>
              Select an incident and start the AI investigation to
              generate the complete handoff and RCA report.
            </p>
          </div>
        </section>
      )}

      {copiedLabel ? (
        <div className="copyToast" role="status">
          Copied {copiedLabel}
        </div>
      ) : null}

      {closureModalOpen &&
      selectedIncident?.status === "Resolved" ? (
        <ClosureReviewModal
          key={`closure-${selectedIncident.incidentNumber}`}
          incident={selectedIncident}
          investigations={history}
          onClose={() => setClosureModalOpen(false)}
          onClosed={handleIncidentClosed}
        />
      ) : null}

      {editModalOpen && selectedIncident ? (
        <EditIncidentModal
          key={selectedIncident.incidentNumber}
          open
          incident={selectedIncident}
          onClose={() => setEditModalOpen(false)}
          onUpdated={handleIncidentUpdated}
        />
      ) : null}

      <CreateIncidentModal
        open={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        onCreated={handleIncidentCreated}
      />

      <footer className="footer">
        <div>
          <strong>AI Incident Handoff &amp; RCA Agent</strong>
          <span>Built with controlled, evidence-grounded AI.</span>
        </div>
        <span>Local portfolio demonstration</span>
      </footer>
    </main>
  );
}
