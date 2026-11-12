import { ApiError } from '../services/api';

type BookingErrorKey =
    | 'slotTaken' | 'tooManyPending' | 'notConfigured' | 'invalidTime' | 'notPending'
    | 'inPast' | 'calendarBusy' | 'tooManyRequests' | 'networkError' | 'generic';

/** Which bookingErrors.* translation to show for an error from the booking API. */
export function bookingErrorKey(error: unknown): BookingErrorKey {
    if (!(error instanceof ApiError)) return 'generic';

    switch (error.code) {
        case 'slot_taken': return 'slotTaken';
        case 'too_many_pending': return 'tooManyPending';
        case 'calendar_not_configured': return 'notConfigured';
        case 'invalid_time': return 'invalidTime';
        case 'booking_not_pending': return 'notPending';
        case 'booking_in_past': return 'inPast';
        case 'calendar_busy': return 'calendarBusy';
        case 'too_many_requests': return 'tooManyRequests';
        case 'network_error': return 'networkError';
        default: return 'generic';
    }
}
