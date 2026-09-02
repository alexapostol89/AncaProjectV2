const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5039";

export interface ContactFormData {
    name: string;
    email: string;
    phone?: string;
    sessionType: string;
    message: string;
    consent: boolean;
    /** Honeypot field — always left empty by real users, must stay out of the visible form. */
    website?: string;
}

export interface ContactApiResponse {
    message: string;
}

/** One field-level validation error returned by the backend (ASP.NET ValidationProblemDetails). */
export type ContactFieldErrors = Record<string, string[]>;

export class ContactApiError extends Error {
    status: number;
    fieldErrors?: ContactFieldErrors;

    constructor(message: string, status: number, fieldErrors?: ContactFieldErrors) {
        super(message);
        this.name = "ContactApiError";
        this.status = status;
        this.fieldErrors = fieldErrors;
    }
}

/**
 * Sends a contact form submission to the backend, which forwards it by
 * email to the psychologist's inbox. Throws ContactApiError on failure.
 */
export async function submitContactForm(data: ContactFormData): Promise<ContactApiResponse> {
    let response: Response;

    try {
        response = await fetch(`${API_URL}/api/contact`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data),
        });
    } catch {
        throw new ContactApiError(
            "Nu am putut contacta serverul. Verifică-ți conexiunea la internet și încearcă din nou.",
            0
        );
    }

    let payload: unknown = null;
    try {
        payload = await response.json();
    } catch {
        // Non-JSON or empty body; fall through to status-based handling below.
    }

    if (!response.ok) {
        if (response.status === 400 && payload && typeof payload === "object" && "errors" in payload) {
            throw new ContactApiError(
                "Te rugăm să corectezi câmpurile marcate.",
                400,
                (payload as { errors: ContactFieldErrors }).errors
            );
        }

        if (response.status === 429) {
            throw new ContactApiError(
                "Ai trimis prea multe solicitări într-un timp scurt. Te rugăm să încerci din nou peste câteva minute.",
                429
            );
        }

        const message =
            payload && typeof payload === "object" && "message" in payload
                ? String((payload as { message: unknown }).message)
                : "A apărut o eroare neașteptată. Te rugăm să încerci din nou.";

        throw new ContactApiError(message, response.status);
    }

    return payload as ContactApiResponse;
}
