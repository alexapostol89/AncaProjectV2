/** Base URL of the ASP.NET Core API (VITE_API_URL in .env / fly.toml). */
export const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5039";

/**
 * Error thrown by apiRequest. `code` comes from the backend's ErrorResponse
 * (e.g. "invalid_credentials", "email_taken"), or is one of our own:
 * "network_error", "too_many_requests", "validation", "unknown".
 */
export class ApiError extends Error {
    status: number;
    code: string;

    constructor(message: string, status: number, code: string) {
        super(message);
        this.name = "ApiError";
        this.status = status;
        this.code = code;
    }
}

interface RequestOptions {
    method?: "GET" | "POST" | "PUT" | "DELETE";
    body?: unknown;
    /** JWT from the logged-in user; sent as "Authorization: Bearer ..." */
    token?: string | null;
}

/** Calls the API and returns the parsed JSON body, or throws ApiError. */
export async function apiRequest<T>(path: string, { method = "GET", body, token }: RequestOptions = {}): Promise<T> {
    const headers: Record<string, string> = {};
    if (body !== undefined) headers["Content-Type"] = "application/json";
    if (token) headers["Authorization"] = `Bearer ${token}`;

    let response: Response;
    try {
        response = await fetch(`${API_URL}${path}`, {
            method,
            headers,
            body: body !== undefined ? JSON.stringify(body) : undefined,
        });
    } catch {
        throw new ApiError("Network error", 0, "network_error");
    }

    // 204 No Content has no body.
    const payload: unknown = response.status === 204 ? null : await response.json().catch(() => null);

    if (!response.ok) {
        if (response.status === 429) throw new ApiError("Too many requests", 429, "too_many_requests");

        const data = (payload ?? {}) as { message?: string; code?: string; errors?: unknown };
        const code = data.code ?? (data.errors ? "validation" : "unknown");
        throw new ApiError(data.message ?? "Request failed", response.status, code);
    }

    return payload as T;
}
