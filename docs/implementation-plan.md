# Sextant — Implementation Plan

## Philosophy

Each release delivers a testable, usable increment. Nothing ships broken or half-wired. The app should be functional after every release — just with fewer features.

The ActivityEvent table is created in the foundation and events are logged from Release 3 onward, even before the activity feed UI exists. This way, by the time the feed is built, there’s already data in it.

-----

## Release 0 — Foundation

**Goal:** Empty app that runs in Docker, has a database, and shows a shell UI.

### Backend

- [ ] `dotnet new web` project, .NET 10
- [ ] `appsettings.json` with configuration sections (ESI, Database, App)
- [ ] EF Core + SQLite setup in `AppDbContext.cs`
- [ ] Initial migration with `ActivityEvent` table only (log from day one)
- [ ] Scalar wired up: `AddOpenApi()` + `MapScalarApiReference()`
- [ ] Health check endpoint: `GET /health` → 200
- [ ] Static file serving for React frontend
- [ ] Dockerfile (multi-stage: build frontend, build backend, serve both)
- [ ] `docker-compose.yml` with volume mounts for `/app/data` and `/app/keys`

### Frontend

- [ ] Vite + React + TypeScript scaffold
- [ ] Mantine provider with dark theme configured
- [ ] React Router with route stubs for all pages
- [ ] App shell layout: top bar, sidebar nav (hardcoded links), main content area, status bar
- [ ] Sidebar collapse toggle
- [ ] All pages render placeholder text: “Chain Map — coming soon”
- [ ] Login page layout (no functionality yet)

### NSwag

- [ ] `nswag.json` configured for TypeScript client generation from backend OpenAPI spec
- [ ] First generation run producing `client.generated.ts`
- [ ] `client.ts` wrapper exporting configured `SextantClient`

### Testable Result

Run `docker compose up`, open browser, see the app shell with navigation. Hit `/scalar` and see the health endpoint documented. Everything compiles and containers build cleanly.

-----

## Release 1 — Auth

**Goal:** Users can log in with Eve SSO, app verifies corp membership, tokens stored securely.

### Backend

- [ ] EF migration: `User` and `CharacterToken` tables
- [ ] Data Protection API setup with file-persisted keys
- [ ] `TokenService`: encrypt, decrypt, store, refresh tokens
- [ ] Eve SSO OAuth2 flow:
  - `GET /auth/login` → redirect to CCP SSO with scopes
  - `GET /auth/callback` → exchange code for tokens, validate corp ID, create/update user
  - `POST /auth/logout` → clear session
  - `GET /auth/me` → return current user + linked characters
- [ ] Auth middleware: cookie-based session, reject unauthenticated requests
- [ ] Corp membership check against `ALLOWED_CORP_ID` environment variable
- [ ] Role assignment: first user gets Admin, subsequent users get Member
- [ ] `POST /auth/characters` → link an alt (re-auth via SSO)
- [ ] `DELETE /auth/characters/{characterId}` → unlink an alt

### Frontend

- [ ] Login page: “Login with Eve Online” button → redirects to `/auth/login`
- [ ] Auth context: call `/auth/me` on app load, redirect to login if unauthenticated
- [ ] Top bar: active character name + portrait (from ESI CDN URL)
- [ ] Character switcher dropdown in top bar (if multiple characters linked)
- [ ] Settings → My Characters tab: list linked characters, add/remove alts
- [ ] Protected route wrapper: redirects to `/login` if no session

### Testable Result

Click login, authenticate with CCP, land on the dashboard shell. See your character name and portrait in the top bar. Link an alt. Log out and get redirected to login. Try with a non-corp character and get rejected.

-----

## Release 2 — ESI Pipeline + SDE Data

**Goal:** ESI client is generated and operational. WH reference data is loaded and queryable.

### Backend

- [ ] NSwag: generate `EsiClient.generated.cs` from ESI swagger.json
- [ ] `AuthHandler` (DelegatingHandler): inject bearer token, handle 401 + token refresh
- [ ] `RateLimitHandler` (DelegatingHandler): read `X-Ratelimit-*` headers, throttle when low
- [ ] `CacheHandler` (DelegatingHandler): cache responses per `Expires` header in IMemoryCache
- [ ] Register ESI HttpClient pipeline in `Program.cs` with `AddStandardResilienceHandler()`
- [ ] EF migration: `WormholeSystem` and `WormholeType` tables
- [ ] `SdeRefreshService`:
  - Pull WH system data (class, statics, effects) from Anoik.is or eve-ref
  - Pull WH type data (mass limits, lifetime) from ESI type attributes
  - Upsert into SQLite
  - Log ActivityEvent on completion
- [ ] `POST /admin/sde/refresh` → trigger manual refresh
- [ ] `GET /admin/sde/status` → last refresh time, record counts
- [ ] Initial SDE data load on first startup (if tables empty)
- [ ] Background task: scheduled SDE refresh (weekly Tuesday after 11:15 UTC)

### Frontend

- [ ] Settings → SDE Data tab (admin only): last refresh timestamp, force refresh button, status
- [ ] Status bar: ESI connection indicator (green/yellow/red based on last response)

### Testable Result

Hit `/scalar`, call the SDE refresh endpoint, verify WH system and type data is populated. Query a few systems — confirm J-numbers have correct class, statics, effects. Confirm ESI calls work by hitting a simple public endpoint through the generated client.

-----

## Release 3 — Chain Map (Core)

**Goal:** Members can add systems and connections, see them on a visual map.

### Backend

- [ ] EF migration: `ChainSystem` and `ChainConnection` tables with soft-delete
- [ ] Global query filter: `HasQueryFilter(x => x.DeletedAt == null)`
- [ ] Chain endpoints:
  - `GET /chain/systems` — all active systems
  - `POST /chain/systems` — add system (auto-populate class/statics/effect from SDE cache)
  - `PUT /chain/systems/{id}` — update notes
  - `DELETE /chain/systems/{id}` — soft-delete
  - `GET /chain/connections` — all active connections
  - `POST /chain/connections` — add connection (auto-populate mass/lifetime from SDE WH type)
  - `PUT /chain/connections/{id}` — update WH type, status
  - `DELETE /chain/connections/{id}` — soft-delete (collapse)
- [ ] Auto-populate: when adding a system, look up `WormholeSystem` table for class/statics/effect. When adding a connection with a WH type code, look up `WormholeType` for mass limits and lifetime. Calculate `EolAt` from `ScannedAt + LifetimeHours`.
- [ ] Log ActivityEvents for all chain mutations

### Frontend

- [ ] `ChainMap.tsx` page: React Flow canvas, full content area
- [ ] `ChainNode.tsx`: custom node showing system name, class badge, effect icon
- [ ] `ChainEdge.tsx`: custom edge showing WH type label, mass status color, EOL indicator
- [ ] Toolbar: [+ System] [+ Connection] [Fit View] [Center Home]
- [ ] Add System modal: J-number search/autocomplete against SDE data
- [ ] Add Connection modal: select source → destination, WH type dropdown
- [ ] Drag-to-connect: drag from node handle to create connection (React Flow native)
- [ ] Right-click context menus on nodes and edges
- [ ] Node dragging to arrange layout (positions saved per-session in Zustand, not persisted)
- [ ] Home system: distinct visual treatment (thicker border or icon)
- [ ] `EolCountdown.tsx` component: live countdown on edges approaching EOL
- [ ] Empty state: “No systems mapped yet. Click + System to add your home.”

### Testable Result

Add your home J-system, see it appear on the canvas with correct class and effect. Add a connection to the HS static, see the edge with WH type and mass limits. Drag nodes around. Right-click to collapse a connection. Add a few more systems to build a small chain. Refresh the page — chain persists from the database.

-----

## Release 4 — Sig Tracker

**Goal:** Members can scan sigs, paste them from the game client, and manage them per system.

### Backend

- [ ] EF migration: `Signature` table with soft-delete and unique constraint on `(ChainSystemId, SignatureId)`
- [ ] Sig endpoints:
  - `GET /sigs/{systemId}` — all active sigs for a system
  - `POST /sigs/{systemId}` — add single sig
  - `POST /sigs/{systemId}/paste` — bulk import from Eve clipboard format
  - `PUT /sigs/{id}` — update sig details
  - `DELETE /sigs/{id}` — soft-delete (clear)
- [ ] Paste parser (server-side): parse tab-separated Eve clipboard format, upsert logic (match on sig ID, update if exists, add if new, never auto-delete missing sigs)
- [ ] Log ActivityEvents for sig mutations

### Frontend

- [ ] `SigPanel.tsx` drawer: opens from right when a system is clicked on the chain map
- [ ] Panel header: system name, class badge, effect
- [ ] Paste textarea at top with [Import] button
- [ ] Sig table: ID, type (icon + label), name, scan %, scanned by, EOL countdown for WH sigs
- [ ] Inline editing: click type/name cells to edit
- [ ] Row hover actions: [Edit] [Clear]
- [ ] “Add Connection” button on WH sigs: opens add-connection modal pre-populated with source system and sig reference
- [ ] `sigParser.ts`: client-side parser for preview before submitting to the paste endpoint
- [ ] Update ChainNode: show sig count badge on each system node
- [ ] Keyboard shortcut: `V` focuses paste textarea when SigPanel is open

### Testable Result

Click a system on the chain map, SigPanel opens. Paste sigs from Eve — see them appear in the table. Edit a sig type. Clear a sig. Paste again with updated scan results — existing sigs update, new ones appear. Click “Add Connection” on a WH sig — connection modal opens pre-filled. Sig count shows on the system node.

-----

## Release 5 — Real-Time + Activity Feed

**Goal:** All clients see changes instantly. Activity feed shows operational history.

### Backend

- [ ] `SextantHub.cs` SignalR hub
- [ ] Hub registration in `Program.cs`
- [ ] Wire all chain, sig, and future endpoints to broadcast SignalR events after DB writes:
  - `SigAdded`, `SigUpdated`, `SigCleared`, `SigsBulkUpdated`
  - `SystemAdded`, `SystemRemoved`
  - `ConnectionAdded`, `ConnectionUpdated`, `ConnectionCollapsed`
  - `ActivityEvent`
- [ ] Activity endpoints:
  - `GET /activity` — paginated, full feed
  - `GET /activity?type={type}` — filtered by event type
  - `GET /activity?systemId={id}` — filtered by system

### Frontend

- [ ] `signalr.ts`: hub connection setup with auto-reconnect
- [ ] `useSignalR.ts` hook: connect on mount, subscribe to events, invalidate TanStack Query caches
- [ ] `ActivityFeed.tsx` component: reusable, accepts optional type/system filter props
- [ ] Dashboard: full unfiltered activity feed below summary cards
- [ ] Chain Map: filtered activity sidebar (chain/sig events only), collapsible
- [ ] Status bar: SignalR connection indicator (green dot / red dot)
- [ ] Reconnection behavior: automatic retry, subtle indicator during disconnection
- [ ] Summary cards on Dashboard: active connections count, unscanned sigs count (wired to real queries now)

### Testable Result

Open the app in two browser windows. Add a sig in one — it appears in the other instantly. Collapse a connection — both windows update. Check the dashboard activity feed — see the history of everything done in Releases 3-4. Disconnect network briefly — see red dot, reconnect — see green dot and catch-up data.

-----

## Release 6 — Mass Tracker + Rolling

**Goal:** Members can log mass passes, see hole health, calculate rolling combos, and run collaborative rolling ops.

### Backend

- [ ] EF migration: `MassPass` and `RollingSession` tables
- [ ] Mass endpoints:
  - `GET /mass/{connectionId}` — all passes + running total + status
  - `POST /mass/{connectionId}` — log a pass (ship type, direction, prop mod)
  - `DELETE /mass/{id}` — remove erroneous pass (admin)
  - `GET /mass/{connectionId}/status` — current status + remaining estimate
- [ ] Mass status calculation: sum `EffectiveMassKg` of all passes, compare against `MaxMassKg` from connection. Determine Fresh/Half/Critical thresholds.
- [ ] Ship mass lookup: query ESI type endpoint for ship mass, cache in IMemoryCache (24h TTL)
- [ ] Rolling endpoints:
  - `POST /rolling/{connectionId}/start` — create session
  - `POST /rolling/{sessionId}/checkin` — member checks in with available ships
  - `GET /rolling/{sessionId}` — session state + recommended jump order
  - `POST /rolling/{sessionId}/complete` — mark collapsed, soft-delete connection
  - `POST /rolling/{sessionId}/cancel` — cancel session
- [ ] Rolling calculator logic: given remaining mass and available ships, compute safe jump combinations that won’t strand someone on the wrong side
- [ ] SignalR events: `MassPassLogged`, `MassStatusChanged`, `RollingSessionStarted`, `RollingSessionUpdated`, `RollingSessionCompleted`
- [ ] Log ActivityEvents for mass and rolling mutations

### Frontend

- [ ] `MassPanel.tsx` drawer: opens from right when a connection is clicked on chain map
- [ ] `MassHealthBar.tsx`: color-coded bar (green → yellow → red), numeric labels, animated
- [ ] Log Pass form: ship dropdown (searchable), direction toggle (Out/Back), prop mod checkbox
- [ ] Real-time effective mass preview when prop mod toggled
- [ ] Pass history table: ship, direction, effective mass, character, timestamp
- [ ] Rolling calculator section: ship checkboxes, recommended combos display
- [ ] “Start Rolling Op” button → collaborative session view
- [ ] Rolling session UI: who’s checked in, recommended jump order, log-as-you-go
- [ ] Update ChainEdge: mass status color (green/yellow/red) driven by real data
- [ ] SignalR: `MassPassLogged` and `MassStatusChanged` invalidate mass queries + update edge colors

### Testable Result

Click a connection, MassPanel opens. Log a Megathron pass with prop mod on — see 200M kg added, health bar moves. Log more passes — watch it cross half mass (yellow) and critical (red). Open rolling calculator — check available ships, see recommended sequence. Start a rolling op in one window, check in from another — both see the session. Complete the roll — connection collapses on both clients.

-----

## Release 7 — Notifications

**Goal:** Toasts for important events. Browser push for critical alerts when the tab is backgrounded.

### Backend

- [ ] No new endpoints needed — notifications are purely frontend, driven by existing SignalR events

### Frontend

- [ ] Mantine notifications integration (`@mantine/notifications`)
- [ ] Toast rules wired to SignalR events:
  - **Info** (auto-dismiss 5s): sig added, connection added, session started
  - **Warning** (auto-dismiss 10s): mass at half, EOL < 4h
  - **Critical** (persist until dismissed): mass critical, structure fuel low
- [ ] Browser push notifications:
  - Service worker registration on app load
  - `Notification.requestPermission()` prompt on first login
  - Critical events fire `new Notification()` when `document.hidden`
  - Events: mass critical, structure fuel < 24h, rolling session awaiting jump
- [ ] Eve downtime banner: yellow bar in top area, appears 15 min before 11:00 UTC, disappears after DT confirmed over (ESI responds normally)
- [ ] Notification preferences in Settings (optional): toggle which events trigger push

### Testable Result

Add a sig — see info toast bottom-right, auto-dismisses. Push mass past half — see yellow warning toast. Push mass to critical — see red persistent toast. Tab away to Eve, push mass to critical — see browser push notification. Confirm DT banner appears at the right time.

-----

## Release 8 — PI Tracker

**Goal:** See all corp members’ PI colonies and expiry times, fully automated from ESI.

### Backend

- [ ] PI endpoints (proxy ESI data, not stored):
  - `GET /pi` — all linked characters’ colonies with calculated expiry
  - `GET /pi/{characterId}` — single character’s PI detail
- [ ] ESI PI data processing: call `esi-planets.manage_planets.v1` endpoints, extract colony data, calculate extraction expiry from extractor head data
- [ ] ActivityEvent: log when a colony enters < 24h expiry window (checked on each PI data fetch)

### Frontend

- [ ] `PiTracker.tsx` page
- [ ] Table: character, planet, product, expiry countdown, status indicator
- [ ] Sort by expiry (soonest first) by default
- [ ] Color-coded status: red (< 4h), yellow (< 24h), green (> 24h)
- [ ] Row expand: planet detail (extraction heads, routes, production chain)
- [ ] Auto-refresh on TanStack Query interval (every 30 min, matching ESI cache)
- [ ] Dashboard summary card: “PI Expiring Soon” count wired to real data
- [ ] Toast notifications for PI expiry < 4h

### Testable Result

Open PI Tracker — see all linked characters’ colonies with accurate expiry times. Sort by expiry. Expand a row to see planet detail. Check dashboard — PI summary card shows correct count. If a colony is near expiry, see toast.

-----

## Release 9 — Fittings Library

**Goal:** Store, browse, and share corp doctrine fits.

### Backend

- [ ] EF migration: `Fitting` table with soft-delete
- [ ] Fitting endpoints:
  - `GET /fittings` — all active fits
  - `GET /fittings?doctrine={name}` — filter by doctrine
  - `POST /fittings` — add a fit (parse EFT format to extract ship name/type)
  - `PUT /fittings/{id}` — update fit
  - `DELETE /fittings/{id}` — soft-delete
  - `GET /fittings/{id}/eft` — raw EFT string for clipboard
- [ ] EFT parser: extract ship type name from first line of EFT format, look up ship type ID from ESI
- [ ] Log ActivityEvents for fit mutations

### Frontend

- [ ] `Fittings.tsx` page
- [ ] Card list of fits, filterable by doctrine and role dropdowns
- [ ] “Add Fit” modal: paste EFT text, app parses ship name, user tags doctrine + role
- [ ] “View Fit” modal: formatted fit display + ship stats from ESI (mass, hull class)
- [ ] “Copy EFT” button: one-click copy EFT string to clipboard
- [ ] Delete with confirmation
- [ ] Empty state: “No doctrine fits yet. Add your first fit.”

### Testable Result

Add a Megathron rolling fit by pasting EFT — see it appear as a card tagged “Rolling Ships / Roller”. Filter by doctrine. View the fit. Copy EFT to clipboard, paste into Eve — fits correctly. Delete a fit.

-----

## Release 10 — Members + Kills + Structures

**Goal:** ESI-powered dashboards for corp member status, kill feed, and structure health.

### Backend

- [ ] Member endpoints (proxy ESI):
  - `GET /members` — all corp members with online/location/ship from ESI
  - `GET /members/{characterId}` — single member detail
- [ ] `ZKillService`: pull recent corp kills from zKillboard API, short TTL cache
- [ ] Kill endpoints:
  - `GET /kills` — recent corp kills/losses, paginated
- [ ] Structure endpoints (proxy ESI):
  - `GET /structures` — corp structures with fuel/services/state
- [ ] ActivityEvent: log kills automatically when new kills appear

### Frontend

- [ ] `Members.tsx` page: card grid with portrait, name, online dot, system, ship. Sorted online-first.
- [ ] `KillFeed.tsx` page: card feed of kills/losses. Green border = kill, red = loss. Link to zKillboard.
- [ ] `Structures.tsx` page: card per structure with fuel bar, service list, state.
- [ ] Fuel bar color: green (> 7d), yellow (< 7d), red (< 24h).
- [ ] Update `ChainNode.tsx`: show member avatars on systems where members are currently located (from member location data)
- [ ] Dashboard summary card: “Online Members” wired to real data with avatar stack

### Testable Result

Open Members — see corp members with online status and locations. See member avatars on chain map nodes. Check Kill Feed — see recent kills with ISK values. Check Structures — see fuel levels and service status. All auto-refreshing.

-----

## Release 11 — Site Log + Dashboard Polish

**Goal:** Track site running income. Polish the dashboard into the real “home” screen.

### Backend

- [ ] EF migration: `SiteLog` table
- [ ] Site log endpoints:
  - `GET /sites` — paginated, filterable by system/type/date
  - `POST /sites` — log a completed site
- [ ] Log ActivityEvents for site completions
- [ ] Dashboard aggregate endpoints (or extend existing):
  - Total ISK from sites this week/month
  - Sites by type breakdown

### Frontend

- [ ] `SiteLog.tsx` page: table of completed sites, “Log Site” modal (system selector, type, name, ISK, participants)
- [ ] Summary stats at top: ISK this week/month, sites by type
- [ ] Dashboard polish:
  - All four summary cards wired to real data and clickable
  - Activity feed finalized with icons per event type, color-coded borders
  - Clean empty states everywhere
- [ ] Settings → Corp Settings tab (admin): allowed corp ID, home system designation
- [ ] Settings → Admin: user management, role changes

### Testable Result

Log a combat site completion — see it in the site log with ISK value. Check dashboard — summary stats reflect real data. Activity feed shows full history. Everything is polished, no placeholder text remains.

-----

## Release 12 — Hardening

**Goal:** Handle edge cases, improve resilience, final cleanup.

### Tasks

- [ ] Eve downtime handling: stale-data banner, ESI error graceful degradation
- [ ] Concurrency: test simultaneous sig pastes, duplicate connection prevention
- [ ] Error states: inline errors on ESI failures, not full-page crashes
- [ ] Loading states: Mantine Skeleton on page loads, LoadingOverlay on actions
- [ ] Keyboard shortcuts: `V` (paste sigs), `Esc` (close panel), `F` (fit view), `H` (center home)
- [ ] Browser push notification preferences in Settings
- [ ] Audit all empty states
- [ ] Regenerate NSwag clients (both TypeScript and C# ESI) — verify everything is in sync
- [ ] Docker image size optimization (if needed)
- [ ] Test full onboarding flow: new user → login → rejected (wrong corp) and accepted (right corp)
- [ ] Test token refresh: let access token expire, verify seamless refresh
- [ ] Test SDE refresh: trigger manual, verify data updates
- [ ] README with setup instructions for self-hosting

-----

## Release Summary

|Release|Name                        |Core Deliverable                              |
|-------|----------------------------|----------------------------------------------|
|0      |Foundation                  |App shell runs in Docker                      |
|1      |Auth                        |Eve SSO login with corp gate                  |
|2      |ESI + SDE                   |Generated ESI client, WH reference data loaded|
|3      |Chain Map                   |Add systems and connections, visual graph     |
|4      |Sig Tracker                 |Paste sigs from game, manage per system       |
|5      |Real-Time                   |SignalR live updates, activity feed           |
|6      |Mass + Rolling              |Log passes, health tracking, rolling ops      |
|7      |Notifications               |Toasts + browser push for critical alerts     |
|8      |PI Tracker                  |Colony expiry tracking from ESI               |
|9      |Fittings                    |Doctrine fit library                          |
|10     |Members + Kills + Structures|ESI-powered corp dashboards                   |
|11     |Site Log + Polish           |Income tracking, dashboard finalized          |
|12     |Hardening                   |Edge cases, error handling, final QA          |

Each release builds on the previous. The app is usable after Release 3 (you can map your chain). It becomes genuinely valuable at Release 6 (mass tracking while mapping). Everything after that is additive.
