import type {
  CloseIncidentRequest,
  CreateIncidentRequest,
  IncidentDetails,
  IncidentSummary,
  InvestigationHistoryItem,
  InvestigationRequest,
  InvestigationResult,
} from "@/types/incident";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.ok) {
    return response.json() as Promise<T>;
  }

  let message = `Request failed with HTTP ${response.status}.`;

  try {
    const errorBody = (await response.json()) as {
      message?: string;
      detail?: string;
    };

    if (errorBody.message) {
      message = errorBody.detail
        ? `${errorBody.message} ${errorBody.detail}`
        : errorBody.message;
    }
  } catch {
    // Keep the fallback error message.
  }

  throw new Error(message);
}

export async function getIncidents(): Promise<IncidentSummary[]> {
  const response = await fetch(`${API_BASE_URL}/api/incidents`, {
    cache: "no-store",
  });

  return parseResponse<IncidentSummary[]>(response);
}

export async function getIncident(
  incidentNumber: string,
): Promise<IncidentDetails> {
  const response = await fetch(
    `${API_BASE_URL}/api/incidents/${encodeURIComponent(incidentNumber)}`,
    {
      cache: "no-store",
    },
  );

  return parseResponse<IncidentDetails>(response);
}

export async function startInvestigation(
  request: InvestigationRequest,
): Promise<InvestigationResult> {
  const response = await fetch(`${API_BASE_URL}/api/investigations`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  return parseResponse<InvestigationResult>(response);
}

export async function createIncident(
  request: CreateIncidentRequest,
): Promise<IncidentDetails> {
  const response = await fetch(`${API_BASE_URL}/api/incidents`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  return parseResponse<IncidentDetails>(response);
}

export async function updateIncident(
  incidentNumber: string,
  request: CreateIncidentRequest,
): Promise<IncidentDetails> {
  const response = await fetch(
    `${API_BASE_URL}/api/incidents/${encodeURIComponent(incidentNumber)}`,
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    },
  );

  return parseResponse<IncidentDetails>(response);
}

export async function getInvestigationHistory(
  incidentNumber?: string,
): Promise<InvestigationHistoryItem[]> {
  const query = incidentNumber
    ? `?incidentNumber=${encodeURIComponent(incidentNumber)}`
    : "";

  const response = await fetch(
    `${API_BASE_URL}/api/investigations/history${query}`,
    {
      cache: "no-store",
    },
  );

  return parseResponse<InvestigationHistoryItem[]>(response);
}

export async function getInvestigation(
  investigationId: string,
): Promise<InvestigationResult> {
  const response = await fetch(
    `${API_BASE_URL}/api/investigations/${encodeURIComponent(
      investigationId,
    )}`,
    {
      cache: "no-store",
    },
  );

  return parseResponse<InvestigationResult>(response);
}

export async function closeIncident(
  incidentNumber: string,
  request: CloseIncidentRequest,
): Promise<IncidentDetails> {
  const response = await fetch(
    `${API_BASE_URL}/api/incidents/${encodeURIComponent(
      incidentNumber,
    )}/close`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    },
  );

  return parseResponse<IncidentDetails>(response);
}

