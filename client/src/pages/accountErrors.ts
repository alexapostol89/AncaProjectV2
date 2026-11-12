import { ApiError } from '../services/api';

type AccountErrorKey =
    | 'wrongPassword' | 'adminAccount' | 'notFound'
    | 'validation' | 'tooManyRequests' | 'networkError' | 'generic';

/** Which accountErrors.* translation to show for an error from the profile or user-management API. */
export function accountErrorKey(error: unknown): AccountErrorKey {
    if (!(error instanceof ApiError)) return 'generic';

    switch (error.code) {
        case 'wrong_password': return 'wrongPassword';
        case 'admin_account': return 'adminAccount';
        case 'not_found': return 'notFound';
        case 'validation': return 'validation';
        case 'too_many_requests': return 'tooManyRequests';
        case 'network_error': return 'networkError';
        default: return 'generic';
    }
}
