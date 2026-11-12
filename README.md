# Anca Berbunschi — Psychology Practice Website

Website for the private psychology practice of Anca Alexandra Berbunschi:
public pages (about, services, education, pricing, the Expat Woman's Journal, contact),
client accounts (register / login) and an admin area for user management.
A chatbot is planned for later.

| Part | Tech | Folder | Production |
|---|---|---|---|
| Frontend | React 19 + TypeScript + Vite, react-router, i18next | `client/` | https://anca-frontend.fly.dev |
| Backend API | ASP.NET Core (.NET 10), controllers + DTOs + services | `server/api/` | https://anca-api.fly.dev |
| Database | PostgreSQL on Neon (EU / Frankfurt), EF Core + Npgsql | — | Neon project, `main` branch |
| Email | Resend (contact form, password reset) | — | — |
| Hosting | Fly.io (`fra` region), Docker | `client/fly.toml`, `server/fly.toml` | — |

---

## Contents

1. [Getting started](#1-getting-started)
2. [Configuration and secrets](#2-configuration-and-secrets)
3. [Backend architecture](#3-backend-architecture)
4. [Database](#4-database)
5. [Authentication and roles](#5-authentication-and-roles)
6. [API reference](#6-api-reference)
7. [Frontend architecture](#7-frontend-architecture)
8. [Translations (i18n)](#8-translations-i18n)
9. [Responsive design](#9-responsive-design)
10. [Deployment](#10-deployment)
11. [Common tasks](#11-common-tasks)
12. [Troubleshooting](#12-troubleshooting)

---

## 1. Getting started

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org) (the Docker build uses Node 22)
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- Access to the Neon project (ask for the **dev** branch connection string)
- Optional: [flyctl](https://fly.io/docs/flyctl/) for deployments

### First-time setup

Both apps have a `.env.example` listing every setting. Copy it and fill in the values — the copies are git-ignored.

```bash
# 1. Backend
cd server/api
cp .env.example .env            # fill in: ConnectionStrings__Default, Jwt__Key, (optional) Seed__Admin*

# 2. Frontend
cd ../../client
npm install
cp .env.example .env.local      # VITE_API_URL is already right for local dev; add DEEPL_API_KEY to run translations
```

Generate a JWT key, for example with `openssl rand -base64 48`.

> The API loads `server/api/.env` itself at startup (`Configuration/DotEnv.cs`). .NET user-secrets
> (`dotnet user-secrets set "Jwt:Key" "..."`) also still work if you prefer them; a value in `.env` wins.

### Run locally

Two terminals:

```bash
# Terminal 1 — API on http://localhost:5039
cd server/api
dotnet run --launch-profile http

# Terminal 2 — website on http://localhost:5173
cd client
npm run dev
```

In Development the API **applies database migrations automatically** on startup
(`Database:MigrateOnStartup = true` in `appsettings.Development.json`) and creates the seed admin if configured.

### Useful commands

| Where | Command | What it does |
|---|---|---|
| `client/` | `npm run dev` | Start the dev server |
| `client/` | `npm run build` | Check translations → type-check → production build |
| `client/` | `npm run lint` | ESLint |
| `client/` | `npm run translate` | Auto-translate new/changed English texts with DeepL |
| `client/` | `npm run i18n:check` | Fail if any translation is missing or outdated |
| `server/api/` | `dotnet build` | Build the API |
| `server/api/` | `dotnet ef migrations add <Name> -o Data/Migrations` | Create a migration after changing entities |
| `server/api/` | `dotnet ef database update` | Apply migrations to your local (dev) database |

---

## 2. Configuration and secrets

**Rule: secrets never go into `appsettings*.json`, `.env`, or any committed file.**

### Backend (`server/api`)

Configuration comes from `appsettings.json` → `appsettings.Development.json` → user-secrets → environment variables
(locally filled from `server/api/.env`; in production from Fly secrets). Later sources win.
In `.env` and environment variables, `:` becomes `__` (e.g. `Jwt:Key` → `Jwt__Key`).
**The template with every secret is `server/api/.env.example`.**

| Key | Secret? | Where | Purpose |
|---|---|---|---|
| `ConnectionStrings:Default` | yes | `.env` / Fly secret | Neon PostgreSQL connection string (use the **pooled** host) |
| `DATABASE_URL` | yes | repo-root `.env.local` (written by the Neon CLI) | Alternative to the above as a `postgresql://` URL; only used when `ConnectionStrings:Default` is empty |
| `Jwt:Key` | yes | `.env` / Fly secret | Signs login tokens (≥ 32 chars) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryHours` | no | `appsettings.json` | Token settings (login lasts 8 hours) |
| `Email:ApiKey` | yes | `.env` / Fly secret | Resend API key. If empty in Development, reset links are printed to the console instead of emailed |
| `GoogleCalendar:ClientId`, `ClientSecret`, `RefreshToken` | yes | `.env` / Fly secrets | Server-only OAuth credentials for availability checks and event creation |
| `GoogleCalendar:CalendarId` | no | `.env` / `appsettings.json` | Calendar to check and add bookings to; `primary` for the connected account |
| `Email:From`, `Email:FromName`, `Email:To` | no | `appsettings*.json` / Fly secret | Sender and the practice inbox |
| `ClientApp:BaseUrl` | no | `appsettings*.json` | Frontend URL, used to build links in emails |
| `Cors:AllowedOrigins` | no | `appsettings*.json` / Fly secrets | Which frontends may call the API |
| `Database:MigrateOnStartup` | no | `appsettings*.json` | `true` in dev, `false` in prod |
| `Seed:AdminEmail`, `Seed:AdminPassword`, `Seed:AdminFullName` | password yes | `.env` / Fly secret | First admin, created at startup if that email doesn't exist |

The app refuses to start with a clear message if the connection string or JWT key is missing.

### Google Calendar booking setup

Bookings use the practice's Google Calendar through the Calendar API. Create an OAuth client in Google Cloud, enable the Google Calendar API, and authorize the client as `psihologancaalexandra@gmail.com` with the `calendar.freebusy` and `calendar.events` scopes. Store the resulting client ID, client secret, and offline refresh token as `GoogleCalendar__ClientId`, `GoogleCalendar__ClientSecret`, and `GoogleCalendar__RefreshToken` in `server/api/.env` locally and as Fly secrets in production. Keep the refresh token server-side. Event creation sends Google Calendar updates to attendees; the API also emails the practice inbox when a booking succeeds.

The booking endpoint currently offers 50-minute slots Monday through Saturday from 10:00 to 20:00 Europe/Bucharest, up to 90 days ahead. Configure `GoogleCalendar:SessionMinutes` to change the duration. A Google connection is required before bookings can be completed.

### Frontend (`client`)

| Variable | File | Purpose |
|---|---|---|
| `VITE_API_URL` | `.env` / `.env.local`, and `client/fly.toml` for production | API base URL. **Everything starting with `VITE_` is public** (built into the JS bundle) |
| `DEEPL_API_KEY` | `.env.local` only | Used by `npm run translate` on your machine. Never prefix it with `VITE_` |

`.env.local` and `.env` are git-ignored. `.env.example` documents the variables.

---

## 3. Backend architecture

```
server/api/
├── Program.cs               # composition root: config, DB, auth, CORS, rate limits, DI
├── Configuration/           # strongly-typed settings classes (JwtSettings, SeedSettings, ...)
├── Controllers/             # HTTP layer only — no business logic
│   ├── ApiControllerBase.cs # CurrentUserId + ServiceError → HTTP status mapping
│   ├── AuthController.cs    # /api/auth/*
│   ├── AdminController.cs   # /api/admin/*  (Admin role only)
│   └── ContactController.cs # /api/contact
├── DTOs/                    # request/response shapes + validation attributes
├── Models/                  # EF entities: User, Role, UserRole enum
├── Data/                    # AppDbContext, DatabaseInitializer (migrate + seed), Migrations/
└── Services/                # business logic (AuthService, UserAdminService, TokenService, EmailService)
    └── Results/             # ServiceResult / ServiceError
```

**Request flow:** Controller → DTO (validated automatically by `[ApiController]`) → Service → `AppDbContext` → PostgreSQL.

**Conventions**
- Controllers stay thin: call a service, turn the `ServiceResult` into a response with `ErrorResult(...)`.
- Services never touch HTTP; they return `ServiceResult` / `ServiceResult<T>` with a `ServiceError(Type, Message, Code)`.
- Error responses always look like `{ "message": "...", "code": "email_taken" }`. The frontend switches on `code`, not on the text.
- Validation errors (wrong DTO fields) are the standard ASP.NET `ValidationProblemDetails` (`{ errors: { Field: [...] } }`).
- Never return entities (e.g. `User`, which contains the password hash) — always map to a DTO (`UserDto.FromUser`).
- User-facing messages from the API are in Romanian; the frontend shows its own translated text based on `code`.

**Rate limiting** (per client IP, `Configuration/RateLimitPolicies.cs`): contact form 5/min, auth endpoints 10/min.
`UseForwardedHeaders` makes the real visitor IP visible behind Fly's proxy.

---

## 4. Database

PostgreSQL on **Neon**. Use one Neon **branch per environment**: `dev` for local work, `main` for production.

### Schema

```
Roles                         Users
─────────────────             ──────────────────────────────────────
Id    int  PK  ◄───────────── RoleId                      int  FK (Restrict)
Name  text (unique)           Id                          uuid PK
                              Email                       text (unique, stored lowercase)
(1, Admin)                    FullName                    text
(2, User)                     PasswordHash                text (PBKDF2 via PasswordHasher)
                              CreatedAt                   timestamptz
                              LastLoginAt                 timestamptz?
                              PasswordResetTokenHash      text?  (SHA-256 of the emailed token)
                              PasswordResetTokenExpiresAt timestamptz?
```

- **Roles** is a fixed lookup table. Its two rows are inserted by the migration (`HasData`). In C#, the enum `UserRole { Admin = 1, User = 2 }` matches those ids.
- One user has exactly one role (one-to-many), so there is no join table.
- `__EFMigrationsHistory` is EF Core's own bookkeeping table.
- **Planned (chatbot):** `Conversations` and `ChatMessages`, each with a foreign key to `Users` using **cascade delete**, so deleting an account deletes the user's data (GDPR).

### Migrations

```bash
cd server/api
# after changing Models/ or AppDbContext:
dotnet ef migrations add AddSomething -o Data/Migrations
# dev: just start the API (migrates automatically) or run:
dotnet ef database update
# production (MigrateOnStartup is false there):
dotnet ef database update --connection "<main branch connection string>"
```

Always commit the migration files. Never edit a migration that has already been applied to production — add a new one.

---

## 5. Authentication and roles

- **Login** (`POST /api/auth/login`) checks the password hash and returns a **JWT valid for 8 hours** plus the user.
  The frontend sends it as `Authorization: Bearer <token>` on every protected call.
- **Logout** = the frontend deletes the token (there is no server session).
- The JWT contains `sub` (user id), `email`, `name`, `role`. Claim names are kept short (`MapInboundClaims = false`).
- `[Authorize]` = logged in. `[Authorize(Roles = nameof(UserRole.Admin))]` = admin only.
- **Registration always creates role `User`.** The first admin comes from the `Seed:*` settings; admins can then promote others.
- Safety rules: an admin cannot demote or delete themselves, and the last admin cannot delete their own account.
- **Password rules** (backend `PasswordRules` and frontend `isStrongPassword`, keep them in sync): 8+ characters, a lowercase letter, an uppercase letter, a digit.
- **Password reset:** a random token is emailed, only its SHA-256 hash is stored, valid 1 hour, usable once.

**Known trade-off (by design, for simplicity):** a JWT cannot be revoked before it expires. A demoted or deleted user keeps
their current token for up to 8 hours. Role changes take effect at the next login.

---

## 6. API reference

Base URL: `http://localhost:5039` (dev), `https://anca-api.fly.dev` (prod). 🔒 = needs `Authorization: Bearer <token>`.

### `/api/auth`

| Method | Path | Body | Success | Errors (`code`) |
|---|---|---|---|---|
| POST | `/register` | `{ fullName, email, password }` | 200 `AuthResponse` | 400 validation, 409 `email_taken` |
| POST | `/login` | `{ email, password }` | 200 `AuthResponse` | 401 `invalid_credentials` |
| GET 🔒 | `/me` | — | 200 `UserDto` | 401 |
| POST | `/forgot-password` | `{ email }` | 200 (always the same answer) | — |
| POST | `/reset-password` | `{ email, token, newPassword }` | 200 | 400 `invalid_link` |
| POST 🔒 | `/change-password` | `{ currentPassword, newPassword }` | 200 | 400 `wrong_password` |
| POST 🔒 | `/delete-account` | `{ password }` | 204 | 400 `wrong_password`, 409 `last_admin` |

`AuthResponse = { token, expiresAt, user: UserDto }`
`UserDto = { id, email, fullName, role: "Admin" | "User", createdAt, lastLoginAt }`

### `/api/admin` (🔒 Admin only — others get 403)

| Method | Path | Body / query | Success | Errors (`code`) |
|---|---|---|---|---|
| GET | `/users?page=1&pageSize=20&search=` | — | 200 `{ items, page, pageSize, totalCount }` | — |
| GET | `/users/{id}` | — | 200 `UserDto` | 404 `not_found` |
| PUT | `/users/{id}/role` | `{ role: "Admin" \| "User" }` | 200 `UserDto` | 404, 409 `cannot_demote_self` |
| DELETE | `/users/{id}` | — | 204 | 404, 409 `cannot_delete_self` |

### Other

| Method | Path | Notes |
|---|---|---|
| POST | `/api/contact` | Contact/booking form → email to the practice. Honeypot field `website` must stay empty |
| GET | `/health` | Used by Fly health checks. Also checks the database: `503` if it can't connect |

Any endpoint can also return **429** when the rate limit is hit.

---

## 7. Frontend architecture

```
client/src/
├── main.tsx                 # imports ./i18n before rendering
├── App.tsx                  # AuthProvider + routes
├── i18n/                    # i18next setup + type declarations for t('...')
├── locales/                 # en.json (source), ro.json, da.json, translation-lock.json
├── context/
│   ├── AuthContext.tsx      # <AuthProvider>: session in localStorage, auto-logout at expiry
│   └── useAuth.ts           # useAuth() hook
├── services/
│   ├── api.ts               # apiRequest() + ApiError — use this for every API call
│   ├── authService.ts       # login/register calls, User types, password/email checks
│   └── contactService.ts    # contact form
├── components/              # Navbar (☰ menu on small screens), Footer, LanguageSelector, BookingLink
├── pages/                   # one component per route
└── Css/                     # one stylesheet per page + App.css (global, navbar, responsive rules)
```

### Routes

| Path | Page | Notes |
|---|---|---|
| `/` | Home | |
| `/despre` | About | |
| `/servicii` | Services | |
| `/studii` | Education | |
| `/preturi` | Pricing | |
| `/jurnal` | Expat Woman's Journal | |
| `/contact` | Contact / booking form | Name and email pre-filled when logged in |
| `/autentificare` | Login | Redirects to `state.from` after login |
| `/inregistrare` | Register | Same redirect behaviour |

### Auth in the frontend

- `const { user, isLoggedIn, token, login, register, logout } = useAuth();`
- The session (`{ token, expiresAt, user }`) is saved in `localStorage` under `anca-auth` and dropped when expired.
- For protected API calls: `apiRequest('/api/...', { token })`.
- **Booking requires an account.** Use `<BookingLink>` for every "book a session" button: logged-out users go to
  `/autentificare` with `state={{ from: '/contact' }}` and are sent on to the booking form after logging in.

---

## 8. Translations (i18n)

Library: **i18next** + **react-i18next**. Languages: **English (source)**, Romanian, Danish.

### The workflow

1. Add or change text **only in `client/src/locales/en.json`**.
2. Use it in a component:
   ```tsx
   const { t } = useTranslation();
   <h1>{t('services.title')}</h1>
   t('home.slide', { number: 2 })                    // "Slide {{number}}" → placeholders
   t('about.story', { returnObjects: true })          // arrays / objects
   // or, inside one section:
   const { t } = useTranslation('translation', { keyPrefix: 'contact' });
   t('name')                                          // = contact.name
   ```
3. Run `npm run translate` → DeepL fills in `ro.json` and `da.json` (only new or changed texts).
4. Review the generated text and commit all locale files, **including `translation-lock.json`**.

### How it works

- `translation-lock.json` stores, for each translated text, a hash of the English text it was made from.
  If the English changes, that text is re-translated; otherwise it is left alone. **Hand-made fixes in `ro.json` / `da.json` are kept**
  as long as the English text doesn't change.
- `{{placeholders}}` are protected so DeepL doesn't translate them.
- Keys are **type-checked**: `t('servcies.title')` fails to compile (types come from `en.json`, see `src/i18n/i18next.d.ts`).
- `npm run build` runs `npm run i18n:check` first and **fails if a translation is missing or outdated**, so half-translated
  pages never reach production. The check needs no API key.
- Language on first visit = the browser's language if supported, otherwise English. The choice is saved in
  `localStorage` (`anca-language`). Missing texts fall back to English.

### Adding a language

Add it to `TARGETS` in `client/scripts/translate.mjs` (with its DeepL code), to `languages` / `languageOptions` in
`src/i18n/index.ts`, import its JSON there, then run `npm run translate`.

### Don'ts

- Don't hardcode user-visible text in components (also `alt`, `aria-label`, `placeholder`, `title`).
- Don't put data in translations (prices, URLs, icons) — keep those in code and match them to translated items by index.
- Don't edit `ro.json` / `da.json` for new keys — add them to `en.json` and run the script.

---

## 9. Responsive design

Breakpoints (see the bottom of `Css/App.css`):

| Width | Behaviour |
|---|---|
| ≥ 1800px / ≥ 2400px | Whole site scaled up 15% / 35% via `--page-zoom` (so it isn't tiny on big monitors) |
| ≤ 1440px | Login/logout buttons show only their icon; brand subtitle hidden |
| ≤ 1200px | Language codes hidden (flags only) |
| ≤ 1100px | Navigation moves into the **☰ dropdown menu** |
| ≤ ~900px | Page grids become one column (each page's CSS); home slider grows with its content, arrows hidden |
| ≤ 600px | Compact phone header; language flags move into the ☰ menu |

Check new pages at **360, 768, 1024, 1280, 1920 and 2560px** — there must be no horizontal scrolling.
If you use `vh` units for heights, divide by `var(--page-zoom)` (see `.hero-slider-container`).

---

## 10. Deployment

Both apps deploy to Fly.io from their folder with `fly deploy`.

### API (`server/`, app `anca-api`)

One-time / when they change, set the secrets:

```bash
fly secrets set -a anca-api \
  ConnectionStrings__Default="<Neon main branch, pooled>" \
  Jwt__Key="<random ≥32 chars>" \
  Email__ApiKey="<Resend key>" \
  GoogleCalendar__ClientId="<OAuth client id>" \
  GoogleCalendar__ClientSecret="<OAuth client secret>" \
  GoogleCalendar__RefreshToken="<offline refresh token>" \
  Seed__AdminEmail="<admin email>" Seed__AdminPassword="<strong password>"
```

Then:

1. Apply new migrations to production: `dotnet ef database update --connection "<main branch connection string>"`
2. `cd server && fly deploy`

### Website (`client/`, app `anca-frontend`)

1. Make sure translations are up to date (`npm run translate`, commit the locale files) — the Docker build runs `npm run build`, which fails otherwise.
2. `VITE_API_URL` for production is set in `client/fly.toml` (`[build.args]`).
3. `cd client && fly deploy`

CORS: the API only accepts the origins in `Cors:AllowedOrigins` (Fly secrets `Cors__AllowedOrigins__0..n`). Add the frontend URL there when it changes.

---

## 11. Common tasks

**Add an API endpoint**
1. Request/response classes in `DTOs/` with validation attributes.
2. Method on the service interface + implementation in `Services/`, returning `ServiceResult`.
3. Action in a controller (inherit `ApiControllerBase`), add `[Authorize]` / rate limiting as needed.
4. Frontend: a function in `services/*.ts` using `apiRequest`.

**Add a database table**
1. Entity class in `Models/`, `DbSet` + configuration in `AppDbContext`.
2. Link it to `Users` with a cascade-delete foreign key if it holds personal data.
3. `dotnet ef migrations add <Name> -o Data/Migrations`, start the API locally, commit the migration.

**Add a page**
1. Component in `pages/`, stylesheet in `Css/`, route in `App.tsx`, link in `Navbar.tsx` and `Footer.tsx`.
2. Texts in `en.json`, then `npm run translate`.
3. Check it at the widths listed in [Responsive design](#9-responsive-design).

**Make someone admin**
Log in as an admin and call `PUT /api/admin/users/{id}/role` with `{ "role": "Admin" }`.

---

## 12. Troubleshooting

| Problem | Cause / fix |
|---|---|
| API won’t start: "ConnectionStrings:Default is not configured" | Fill it in `server/api/.env` (see [Getting started](#1-getting-started)) |
| API won’t start: "Jwt:Key must be set..." | Set `Jwt__Key` in `server/api/.env` (≥ 32 chars) |
| Browser: CORS error | Frontend origin missing from `Cors:AllowedOrigins` |
| `429 Too Many Requests` | Rate limit (10/min on auth). Wait a minute |
| `npm run build` fails with "Translations are out of date" | Run `npm run translate` and commit the locale files |
| `npm run translate`: "Missing DEEPL_API_KEY" | Add it to `client/.env.local` |
| Forgot-password email never arrives in dev | Without `Email:ApiKey`, the reset link is printed in the API console |
| First request is slow | Neon (free tier) and Fly machines sleep when idle; the first request wakes them |
| Logged out after a while | Tokens expire after 8 hours by design |
