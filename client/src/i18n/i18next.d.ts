import 'i18next';
import type en from '../locales/en.json';

/**
 * Makes t('...') type-safe: TypeScript knows every key in en.json,
 * so a typo like t('servcies.title') is a compile error.
 */
declare module 'i18next' {
    interface CustomTypeOptions {
        defaultNS: 'translation';
        resources: { translation: typeof en };
    }
}
