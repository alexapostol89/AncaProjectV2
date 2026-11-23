import { useEffect, useRef, useState, type FormEvent } from 'react';
import { MessageCircle, Send, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { API_URL } from '../services/api';
import '../Css/ChatWidget.css';

type Message = { role: 'user' | 'assistant'; content: string };

export default function ChatWidget() {
    const { t, i18n } = useTranslation();
    const [open, setOpen] = useState(false);
    const [input, setInput] = useState('');
    const [messages, setMessages] = useState<Message[]>([]);
    const [sending, setSending] = useState(false);
    const listRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: 'smooth' });
    }, [messages, sending, open]);

    async function submit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const content = input.trim();
        if (!content || sending) return;

        const nextMessages = [...messages, { role: 'user' as const, content }];
        setMessages(nextMessages);
        setInput('');
        setSending(true);

        try {
            const response = await fetch(`${API_URL}/api/chat`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    message: content,
                    language: i18n.resolvedLanguage ?? 'en',
                    history: nextMessages.slice(-8, -1),
                }),
            });
            const data = await response.json().catch(() => null) as { reply?: string } | null;
            if (!response.ok || !data?.reply) throw new Error('chat_failed');
            setMessages((current) => [...current, { role: 'assistant', content: data.reply! }]);
        } catch {
            setMessages((current) => [...current, { role: 'assistant', content: t('chat.error') }]);
        } finally {
            setSending(false);
        }
    }

    return (
        <div className="chat-widget">
            {open && <section className="chat-panel" aria-label={t('chat.title')}>
                <header className="chat-header">
                    <div><strong>{t('chat.title')}</strong><span>{t('chat.subtitle')}</span></div>
                    <button className="chat-icon-button" type="button" onClick={() => setOpen(false)} aria-label={t('chat.close')}><X size={19} /></button>
                </header>
                <div className="chat-messages" ref={listRef} aria-live="polite">
                    <p className="chat-message assistant">{t('chat.greeting')}</p>
                    {messages.map((message, index) => <p className={`chat-message ${message.role}`} key={`${index}-${message.role}`}>{message.content}</p>)}
                    {sending && <p className="chat-message assistant chat-typing">{t('chat.thinking')}</p>}
                </div>
                <form className="chat-form" onSubmit={submit}>
                    <input value={input} onChange={(event) => setInput(event.target.value)} placeholder={t('chat.placeholder')} aria-label={t('chat.placeholder')} maxLength={1000} disabled={sending} />
                    <button type="submit" disabled={sending || !input.trim()} aria-label={t('chat.send')}><Send size={18} /></button>
                </form>
                <p className="chat-disclaimer">{t('chat.disclaimer')}</p>
            </section>}
            <button className="chat-launcher" type="button" onClick={() => setOpen((value) => !value)} aria-label={open ? t('chat.close') : t('chat.open')} aria-expanded={open}>
                {open ? <X size={23} /> : <><MessageCircle size={22} /><span>{t('chat.open')}</span></>}
            </button>
        </div>
    );
}
