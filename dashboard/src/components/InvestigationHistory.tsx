"use client";

import { formatTechnicalLabel } from "@/lib/format";
import type {
  InvestigationHistoryItem,
} from "@/types/incident";

type InvestigationHistoryProps = {
  incidentNumber: string;
  history: InvestigationHistoryItem[];
  loading: boolean;
  viewedReportId: string | null;
  finalRcaInvestigationId?: string | null;
  onRefresh: () => void;
  onViewReport: (investigationId: string) => void;
};

function formatHistoryDate(value?: string | null) {
  if (!value) {
    return "Not completed";
  }

  return new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatDuration(value?: number | null) {
  if (value === null || value === undefined) {
    return "Not available";
  }

  if (value < 1000) {
    return `${value} ms`;
  }

  return `${(value / 1000).toFixed(1)} sec`;
}

function getConfidenceClass(confidence: string) {
  switch (confidence) {
    case "High":
      return "historyBadge historyConfidenceHigh";
    case "Medium":
      return "historyBadge historyConfidenceMedium";
    case "Low":
      return "historyBadge historyConfidenceLow";
    case "InsufficientEvidence":
      return "historyBadge historyConfidenceInsufficient";
    default:
      return "historyBadge historyBadgeNeutral";
  }
}

export default function InvestigationHistory({
  incidentNumber,
  history,
  loading,
  viewedReportId,
  finalRcaInvestigationId,
  onRefresh,
  onViewReport,
}: InvestigationHistoryProps) {
  return (
    <section className="historyPanel">
      <header className="historyHeader">
        <div className="historyHeadingGroup">
          <span className="sectionNumber">03</span>

          <div>
            <h2>Investigation history</h2>
            <p>
              Review previous reports and confidence progression for{" "}
              <strong>{incidentNumber}</strong>.
            </p>
          </div>
        </div>

        <button
          type="button"
          className="refreshHistoryButton"
          disabled={loading}
          onClick={onRefresh}
        >
          {loading ? "Refreshing..." : "Refresh history"}
        </button>
      </header>

      {loading ? (
        <div className="historyLoading">
          <span className="spinner darkSpinner" />
          Loading investigation history...
        </div>
      ) : history.length === 0 ? (
        <div className="historyEmpty">
          <div className="historyEmptyIcon">◎</div>

          <div>
            <h3>No investigations yet</h3>
            <p>
              Start an AI investigation to create the first
              evidence-based report for this incident.
            </p>
          </div>
        </div>
      ) : (
        <div className="historyTimeline">
          {history.map((item, index) => (
            <article
              className="historyItem"
              key={item.investigationId}
            >
              <div className="historyRail">
                <div
                  className={
                    item.confidence === "High"
                      ? "historyNode historyNodeHigh"
                      : "historyNode"
                  }
                >
                  {history.length - index}
                </div>

                {index < history.length - 1 ? (
                  <span className="historyLine" />
                ) : null}
              </div>

              <div
                className={
                  viewedReportId === item.investigationId
                    ? "historyCard historyCardViewing"
                    : "historyCard"
                }
              >
                <div className="historyCardHeader">
                  <div>
                    <div className="historySequence">
                      Investigation {history.length - index}

                      {index === 0 ? (
                        <span className="latestHistoryLabel">
                          Latest
                        </span>
                      ) : null}

                      {finalRcaInvestigationId ===
                      item.investigationId ? (
                        <span className="finalRcaHistoryLabel">
                          Final RCA
                        </span>
                      ) : null}

                      {viewedReportId ===
                      item.investigationId ? (
                        <span className="viewingHistoryLabel">
                          Currently viewing
                        </span>
                      ) : null}
                    </div>

                    <h3>{item.incidentTitle}</h3>
                  </div>

                  <div className="historyBadges">
                    <span
                      className={getConfidenceClass(
                        item.confidence,
                      )}
                    >
                      {formatTechnicalLabel(
                        item.confidence,
                      )}
                    </span>

                    <span className="historyBadge historyStatusBadge">
                      {item.status}
                    </span>
                  </div>
                </div>

                <div className="historyMetadata">
                  <div>
                    <span>Category</span>
                    <strong>
                      {formatTechnicalLabel(
                        item.rootCauseCategory,
                      )}
                    </strong>
                  </div>

                  <div>
                    <span>AI provider</span>
                    <strong>
                      {item.aiProvider} · {item.modelName}
                    </strong>
                  </div>

                  <div>
                    <span>Controlled tools</span>
                    <strong>
                      {item.toolExecutionCount} executed
                    </strong>
                  </div>

                  <div>
                    <span>Duration</span>
                    <strong>
                      {formatDuration(
                        item.durationMilliseconds,
                      )}
                    </strong>
                  </div>
                </div>

                <p className="historyObjective">
                  {item.objective}
                </p>

                {item.failureReason ? (
                  <div className="historyFailure">
                    {item.failureReason}
                  </div>
                ) : null}

                <footer className="historyCardFooter">
                  <div>
                    <span>
                      Started{" "}
                      {formatHistoryDate(item.startedAtUtc)}
                    </span>

                    <span>
                      Completed{" "}
                      {formatHistoryDate(item.completedAtUtc)}
                    </span>
                  </div>

                  <button
                    type="button"
                    className="viewReportButton"
                    disabled={item.status !== "Completed"}
                    onClick={() =>
                      onViewReport(item.investigationId)
                    }
                  >
                    View saved report
                    <span aria-hidden="true">→</span>
                  </button>
                </footer>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
