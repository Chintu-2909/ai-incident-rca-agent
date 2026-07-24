export type IncidentSummary = {
  id: number;
  incidentNumber: string;
  title: string;
  integrationName: string;
  affectedSystem: string;
  environment: string;
  severity: string;
  status: string;
  knownRootCauseCategory: string;
  detectedAtUtc: string;
};

export type IncidentDetails = IncidentSummary & {
  description: string;
  businessImpact: string;
  technicalEvidence: string;
  errorCode?: string | null;
  errorMessage?: string | null;
  correlationId?: string | null;
  confirmedResolution?: string | null;
  createdAtUtc: string;
  resolvedAtUtc?: string | null;
  statusUpdatedAtUtc?: string | null;
  closedBy?: string | null;
  closedAtUtc?: string | null;
  closureSummary?: string | null;
  finalRcaInvestigationId?: string | null;
  closureValidationConfirmed: boolean;
};

export type ToolExecution = {
  stepNumber: number;
  toolName: string;
  summary: string;
  successful: boolean;
};

export type InvestigationResult = {
  investigationId: string;
  incidentNumber: string;
  classification: string;
  rootCauseCategory: string;
  confidence: string;
  probableRootCause: string;
  confirmedFacts: string[];
  assumptions: string[];
  missingInformation: string[];
  evidence: string[];
  technicalSummary: string;
  serviceNowWorkNote: string;
  stakeholderUpdate: string;
  rootCauseAnalysis: string;
  correctiveActions: string[];
  preventiveActions: string[];
  validationChecklist: string[];
  shiftHandoverNote: string;
  toolsExecuted: ToolExecution[];
  generatedAtUtc: string;
};

export type InvestigationRequest = {
  incidentNumber: string;
  objective: string;
};

export type CreateIncidentRequest = {
  incidentNumber: string;
  title: string;
  description: string;
  integrationName: string;
  affectedSystem: string;
  environment: string;
  businessImpact: string;
  technicalEvidence: string;
  errorCode?: string | null;
  errorMessage?: string | null;
  correlationId?: string | null;
  severity: string;
  status: string;
  rootCauseCategory: string;
  confirmedResolution?: string | null;
  detectedAtUtc?: string | null;
  resolvedAtUtc?: string | null;
};

export type InvestigationHistoryItem = {
  investigationId: string;
  incidentNumber: string;
  incidentTitle: string;
  objective: string;
  aiProvider: string;
  modelName: string;
  status: string;
  rootCauseCategory: string;
  confidence: string;
  toolExecutionCount: number;
  startedAtUtc: string;
  completedAtUtc?: string | null;
  durationMilliseconds?: number | null;
  failureReason?: string | null;
};

export type CloseIncidentRequest = {
  closedBy: string;
  closureSummary: string;
  finalRcaInvestigationId: string;
  validationConfirmed: boolean;
};

