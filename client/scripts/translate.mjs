/**
 * Automatic translation of the website texts with DeepL.
 *
 *   src/locales/en.json   ← the ONLY file you edit (English is the source language)
 *   src/locales/ro.json   ← generated (but hand edits are kept)
 *   src/locales/da.json   ← generated (but hand edits are kept)
 *   src/locales/translation-lock.json ← remembers which English text each translation was made from
 *
 * Usage:
 *   npm run translate        translate new / changed English texts (needs DEEPL_API_KEY in .env.local)
 *   npm run i18n:check       only check; fails if something is missing or outdated (no API key needed)
 *
 * How it decides what to translate, per text (e.g. "services.book"):
 *   - missing in ro/da                          → translate
 *   - English text changed since last translate → translate again
 *   - otherwise                                 → keep as is (so manual fixes in ro/da survive)
 * Texts removed from en.json are removed from ro/da as well.
 */
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const LOCALES_DIR = path.resolve(import.meta.dirname, '../src/locales');
const SOURCE = { code: 'en', deepl: 'EN' };
const TARGETS = [
    { code: 'ro', deepl: 'RO' },
    { code: 'da', deepl: 'DA' },
];
const BATCH_SIZE = 50; // DeepL accepts at most 50 texts per request

const checkOnly = process.argv.includes('--check');

// ---- helpers ------------------------------------------------------------------

const readJson = (file) => (fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : {});
const writeJson = (file, data) => fs.writeFileSync(file, JSON.stringify(data, null, 2) + '\n');
const hash = (text) => crypto.createHash('sha1').update(text).digest('hex').slice(0, 16);

/** { a: { b: ['x', 'y'] } } → { 'a.b.0': 'x', 'a.b.1': 'y' } */
function flatten(obj, prefix = '', out = {}) {
    for (const [key, value] of Object.entries(obj)) {
        const fullKey = prefix ? `${prefix}.${key}` : key;
        if (value !== null && typeof value === 'object') flatten(value, fullKey, out);
        else out[fullKey] = value;
    }
    return out;
}

/** Rebuilds the English structure (same keys, same order, arrays stay arrays) with translated leaves. */
function rebuild(source, translations, prefix = '') {
    if (Array.isArray(source)) return source.map((v, i) => rebuild(v, translations, `${prefix}.${i}`));
    if (source !== null && typeof source === 'object') {
        return Object.fromEntries(Object.entries(source).map(([k, v]) => [k, rebuild(v, translations, prefix ? `${prefix}.${k}` : k)]));
    }
    return translations[prefix];
}

/**
 * Texts are sent as XML so {{placeholders}} can be wrapped in an ignored <x> tag
 * (DeepL then leaves them untouched). That means &, < and > must be escaped first.
 */
const protect = (text) => text
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/\{\{\s*(\w+)\s*\}\}/g, '<x>{{$1}}</x>');
const unprotect = (text) => text
    .replace(/<x>(\{\{\w+\}\})<\/x>/g, '$1')
    .replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&');

async function deeplTranslate(texts, targetLang, apiKey) {
    // Free API keys end with ":fx" and use a different host.
    const host = apiKey.endsWith(':fx') ? 'https://api-free.deepl.com' : 'https://api.deepl.com';
    const response = await fetch(`${host}/v2/translate`, {
        method: 'POST',
        headers: { Authorization: `DeepL-Auth-Key ${apiKey}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({
            text: texts.map(protect),
            source_lang: SOURCE.deepl,
            target_lang: targetLang,
            tag_handling: 'xml',
            ignore_tags: ['x'],
            preserve_formatting: true,
        }),
    });
    if (!response.ok) {
        throw new Error(`DeepL error ${response.status}: ${await response.text()}`);
    }
    const data = await response.json();
    return data.translations.map((t) => unprotect(t.text));
}

// ---- main ---------------------------------------------------------------------

const sourceTree = readJson(path.join(LOCALES_DIR, `${SOURCE.code}.json`));
const source = flatten(sourceTree);
const lockFile = path.join(LOCALES_DIR, 'translation-lock.json');
const lock = readJson(lockFile);

let problems = 0;

for (const target of TARGETS) {
    const targetFile = path.join(LOCALES_DIR, `${target.code}.json`);
    const existing = flatten(readJson(targetFile));
    const targetLock = lock[target.code] ?? {};

    const outdated = Object.keys(source).filter(
        (key) => typeof existing[key] !== 'string' || targetLock[key] !== hash(source[key]),
    );
    const removed = Object.keys(existing).filter((key) => !(key in source));

    if (outdated.length === 0 && removed.length === 0) {
        console.log(`✔ ${target.code}: up to date (${Object.keys(source).length} texts)`);
        continue;
    }

    if (checkOnly) {
        problems += outdated.length + removed.length;
        console.error(`✖ ${target.code}: ${outdated.length} to translate, ${removed.length} to remove`);
        outdated.slice(0, 10).forEach((key) => console.error(`    - ${key}`));
        if (outdated.length > 10) console.error(`    … and ${outdated.length - 10} more`);
        continue;
    }

    const apiKey = process.env.DEEPL_API_KEY;
    if (!apiKey) {
        console.error('Missing DEEPL_API_KEY. Put it in client/.env.local (see .env.example).');
        process.exit(1);
    }

    const translated = { ...existing };
    for (let i = 0; i < outdated.length; i += BATCH_SIZE) {
        const keys = outdated.slice(i, i + BATCH_SIZE);
        const results = await deeplTranslate(keys.map((key) => source[key]), target.deepl, apiKey);
        keys.forEach((key, j) => {
            translated[key] = results[j];
            targetLock[key] = hash(source[key]);
        });
    }
    for (const key of removed) delete targetLock[key];

    writeJson(targetFile, rebuild(sourceTree, translated));
    lock[target.code] = targetLock;
    console.log(`✔ ${target.code}: translated ${outdated.length}, removed ${removed.length}`);
}

if (checkOnly) {
    if (problems > 0) {
        console.error('\nTranslations are out of date. Run: npm run translate');
        process.exit(1);
    }
} else {
    writeJson(lockFile, lock);
}
