import { useTranslation } from 'react-i18next';
import { languageOptions } from '../i18n';

export default function LanguageSelector({ className = '' }: { className?: string }) {
    const { t, i18n } = useTranslation();
    const current = i18n.resolvedLanguage;

    return (
        <div className={`language-selector ${className}`} role="group" aria-label={t('languageSelector.label')}>
            {languageOptions.map((option) => (
                <button
                    key={option.id}
                    type="button"
                    className={`language-option ${current === option.id ? 'selected' : ''}`}
                    onClick={() => i18n.changeLanguage(option.id)}
                    aria-label={option.label}
                    aria-pressed={current === option.id}
                    title={option.label}
                >
                    <span aria-hidden="true">{option.flag}</span>
                    <span className="language-code">{option.id.toUpperCase()}</span>
                </button>
            ))}
        </div>
    );
}
