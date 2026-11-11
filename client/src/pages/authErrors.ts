import { ApiError } from '../services/api';

type AuthErrorKey = 'invalidCredentials' | 'emailTaken' | 'tooManyRequests' | 'networkError' | 'serverError';

/** Which auth.* translation to show for an error from login/register. */
export function authErrorKey(error: unknown): AuthErrorKey {
    if (!(error instanceof ApiError)) return 'serverError';

    switch (error.code) {
        case 'invalid_credentials': return 'invalidCredentials';
        case 'email_taken': return 'emailTaken';
        case 'too_many_requests': return 'tooManyRequests';
        case 'network_error': return 'networkError';
        default: return 'serverError';
    }
}
