import { languageOptions, useLanguage } from '../i18n';

export default function LanguageSelector({ className = '' }: { className?: string }) {
    const { language, setLanguage, messages } = useLanguage();
    return <div className={`language-selector ${className}`} role="group" aria-label={messages.home.flags}>
        {languageOptions.map((option) => <button key={option.id} type="button" className={`language-option ${language === option.id ? 'selected' : ''}`} onClick={() => setLanguage(option.id)} aria-label={option.label} aria-pressed={language === option.id} title={option.label}>
            <span aria-hidden="true">{option.flag}</span><span className="language-code">{option.id.toUpperCase()}</span>
        </button>)}
    </div>;
}
