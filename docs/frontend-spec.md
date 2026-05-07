# Sextant — Frontend Specification

## Overview

The Sextant frontend is a React + TypeScript SPA served as static files from the .NET backend. It uses Mantine for UI components, React Flow for the chain map, and a NSwag-generated client for type-safe API communication.

-----

## Stack

|Component       |Technology                              |
|----------------|----------------------------------------|
|Framework       |React 19 + TypeScript                   |
|Build           |Vite                                    |
|UI Library      |Mantine v7                              |
|Chain Map       |React Flow                              |
|State Management|Zustand (single store)                  |
|Server State    |TanStack Query (React Query)            |
|API Client      |NSwag-generated `SextantClient`         |
|Real-time       |@microsoft/signalr                      |
|Notifications   |Mantine notifications + browser Push API|
|Routing         |React Router v7                         |

-----

## Project Structure

```
frontend/
├── index.html
├── vite.config.ts
├── tsconfig.json
├── package.json
│
└── src/
    ├── main.tsx
    ├── App.tsx
    │
    ├── api/
    │   ├── client.generated.ts      # NSwag — never edit
    │   ├── client.ts                # instantiates SextantClient with base URL
    │   └── signalr.ts               # hub connection setup + event subscriptions
    │
    ├── store/
    │   └── useAppStore.ts           # Zustand — auth state, active system, UI state
    │
    ├── pages/
    │   ├── Login.tsx
    │   ├── Dashboard.tsx
    │   ├── ChainMap.tsx
    │   ├── PiTracker.tsx
    │   ├── Fittings.tsx
    │   ├── SiteLog.tsx
    │   ├── Structures.tsx
    │   ├── KillFeed.tsx
    │   ├── Members.tsx
    │   └── Settings.tsx
    │
    ├── panels/                      # contextual slide-out panels for chain map
    │   ├── SigPanel.tsx
    │   └── MassPanel.tsx
    │
    ├── components/                   # custom components ONLY where Mantine doesn't cover it
    │   ├── ChainNode.tsx            # React Flow custom node for a WH system
    │   ├── ChainEdge.tsx            # React Flow custom edge for a connection
    │   ├── ActivityFeed.tsx         # reusable feed component with optional type filter
    │   ├── MassHealthBar.tsx        # visual mass status indicator
    │   └── EolCountdown.tsx         # live countdown to EOL
    │
    ├── hooks/                       # only if reused 3+ times
    │   └── useSignalR.ts            # connect/disconnect lifecycle
    │
    └── utils/
        ├── sigParser.ts             # parse Eve probe scanner clipboard format
        └── constants.ts             # role enums, mass thresholds, etc.
```

No custom wrappers around Mantine components. Use Mantine directly in pages.

-----

## Routing

```
/login              → Login.tsx (public)
/                   → Dashboard.tsx
/chain              → ChainMap.tsx (with SigPanel + MassPanel overlays)
/pi                 → PiTracker.tsx
/fittings           → Fittings.tsx
/sites              → SiteLog.tsx
/structures         → Structures.tsx
/kills              → KillFeed.tsx
/members            → Members.tsx
/settings           → Settings.tsx
```

All routes except `/login` require authentication. Redirect to `/login` if no valid session.

-----

## State Management

### Zustand Store (`useAppStore.ts`)

Minimal — only UI state and auth context. Server data lives in TanStack Query cache.

```typescript
interface AppState {
  // Auth
  user: User | null;
  activeCharacterId: number | null;

  // Chain map UI state
  selectedSystemId: number | null;
  selectedConnectionId: number | null;
  sigPanelOpen: boolean;
  massPanelOpen: boolean;

  // Actions
  setUser: (user: User | null) => void;
  setActiveCharacter: (id: number) => void;
  selectSystem: (id: number | null) => void;
  selectConnection: (id: number | null) => void;
}
```

### TanStack Query

All API data fetched and cached via React Query. Handles stale time, background refetch, and error states.

```typescript
// Example: fetch chain systems
const { data: systems } = useQuery({
  queryKey: ['chain', 'systems'],
  queryFn: () => client.getChainSystems(),
  staleTime: 30_000,
});
```

SignalR events trigger query invalidation — not direct state mutation:

```typescript
// When a sig is added via SignalR, invalidate the sigs query
hub.on('SigAdded', (sig) => {
  queryClient.invalidateQueries({ queryKey: ['sigs', sig.chainSystemId] });
});
```

This keeps one source of truth (the server) and lets React Query handle re-fetching.

-----

## NSwag Client

Generated from the backend’s OpenAPI spec at `http://localhost:8080/openapi/v1.json`.

```
nswag run
→ outputs: src/api/client.generated.ts
→ exports: class SextantClient with typed methods for every endpoint
```

Thin wrapper in `client.ts`:

```typescript
import { SextantClient } from './client.generated';

const baseUrl = import.meta.env.VITE_API_URL ?? '';
export const client = new SextantClient(baseUrl);
```

All pages import `client` and call methods directly. No intermediate service layer.

-----

## SignalR Integration

### Connection Setup (`signalr.ts`)

```typescript
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export const hub = new HubConnectionBuilder()
  .withUrl('/hub/sextant')
  .withAutomaticReconnect()
  .configureLogging(LogLevel.Warning)
  .build();
```

### Hook (`useSignalR.ts`)

Handles lifecycle — connect on mount, disconnect on unmount. Subscribes to events and invalidates relevant React Query caches.

### Events Handled

|SignalR Event            |Action                                               |
|-------------------------|-----------------------------------------------------|
|`SigAdded`               |Invalidate sigs query, show toast                    |
|`SigUpdated`             |Invalidate sigs query                                |
|`SigCleared`             |Invalidate sigs query                                |
|`SigsBulkUpdated`        |Invalidate sigs query                                |
|`SystemAdded`            |Invalidate chain query                               |
|`SystemRemoved`          |Invalidate chain query                               |
|`ConnectionAdded`        |Invalidate chain query, show toast                   |
|`ConnectionUpdated`      |Invalidate chain query                               |
|`ConnectionCollapsed`    |Invalidate chain query, show toast                   |
|`MassPassLogged`         |Invalidate mass query                                |
|`MassStatusChanged`      |Invalidate mass query, show warning toast if critical|
|`RollingSessionStarted`  |Invalidate rolling query, show toast                 |
|`RollingSessionUpdated`  |Invalidate rolling query                             |
|`RollingSessionCompleted`|Invalidate chain + rolling queries, show toast       |
|`ActivityEvent`          |Invalidate activity query                            |

-----

## Notifications — Three Tiers

### Tier 1 — SignalR Real-Time Updates

All data on screen updates live via query invalidation. No user action required.

### Tier 2 — Mantine Toasts

Use `@mantine/notifications` for in-app alerts. Triggered by SignalR events.

Priority levels:

- **Info** (blue): sig added, connection added, session started
- **Warning** (yellow): mass at half, EOL approaching (<4h remaining)
- **Critical** (red): mass critical, structure fuel low

### Tier 3 — Browser Push Notifications

For when the app tab is backgrounded (Eve is fullscreen).

Critical events only:

- Mass crossed critical threshold
- Structure fuel below 24 hours
- Rolling session awaiting your jump

Implementation:

- Service worker registered on app load
- `Notification.requestPermission()` on first login
- SignalR events for critical alerts trigger `new Notification()` when document is hidden

```typescript
hub.on('MassStatusChanged', (data) => {
  if (data.status === 'Critical' && document.hidden) {
    new Notification('Sextant — Mass Critical', {
      body: `Connection to ${data.systemName} is critical. Do not jump caps.`,
      icon: '/sextant-icon.png',
    });
  }
});
```

-----

## Sig Parser (`sigParser.ts`)

Parses Eve Online’s probe scanner clipboard format.

Input format (tab-separated):

```
ABC-123\tCosmic Signature\tWormhole\tUnstable Wormhole\t100.0%\t4.2 AU
DEF-456\tCosmic Signature\tData Site\tForgotten Data Terminal\t50.0%\t8.1 AU
GHI-789\tCosmic Signature\tUnknown\t\t0.0%\t10.3 AU
```

Output:

```typescript
interface ParsedSig {
  signatureId: string;    // "ABC-123"
  group: string;          // "Cosmic Signature"
  type: SigGroupType;     // Wormhole | Data | Relic | Gas | Combat | Unknown
  name: string | null;    // "Unstable Wormhole" or null if not scanned
  scanPercentage: number; // 100.0
}
```

The parser:

1. Splits input by newlines
1. Splits each line by tabs
1. Maps columns to fields
1. Ignores anomalies (Cosmic Anomaly group)
1. Returns array of `ParsedSig`

The paste endpoint uses delta logic:

- New sig IDs → add
- Existing sig IDs with new data → update
- Sigs NOT in paste but in DB → do NOT auto-delete (scanner may have filtered)

-----

## Theme

Mantine dark theme as base. Customize the color scheme to feel like Eve:

```typescript
const theme = createTheme({
  primaryColor: 'cyan',
  defaultRadius: 'sm',
  colors: {
    // extend with Eve-flavored palette if desired
  },
});
```

Dark backgrounds, cyan/teal accents, minimal borders. Let Mantine handle the rest.

-----

## Custom Components

Only five custom components — everything else uses Mantine directly.

### ChainNode (`ChainNode.tsx`)

React Flow custom node for a system in the chain.

Displays:

- System name (J-number or k-space name)
- WH class badge (C1-C6) with color coding
- Effect icon if applicable (Pulsar, Wolf-Rayet, etc.)
- Number of active sigs in system
- Avatars of members currently in system (from ESI location data)
- Home system gets a distinct border/indicator

Click → opens SigPanel for that system.

### ChainEdge (`ChainEdge.tsx`)

React Flow custom edge for a connection.

Displays:

- WH type code label (e.g., “N062”)
- Mass status color: green (fresh), yellow (half), red (critical)
- EOL indicator if life status is EOL
- Max ship size icon (large/medium/small)

Click → opens MassPanel for that connection.

### ActivityFeed (`ActivityFeed.tsx`)

Reusable feed component.

Props:

- `typeFilter?: ActivityType[]` — filter by event type
- `systemFilter?: number` — filter by system
- `limit?: number` — max items to show

Used on Dashboard (unfiltered), Chain Map sidebar (chain/sig/mass types only), and feature pages (filtered to their type).

### MassHealthBar (`MassHealthBar.tsx`)

Visual indicator of connection mass status.

- Horizontal bar showing % of max mass consumed
- Color gradient: green → yellow → red
- Numeric label: “1,200M / 2,000M kg”
- Animated transition on updates

### EolCountdown (`EolCountdown.tsx`)

Live countdown timer to estimated EOL.

- Calculates from scannedAt + lifetimeHours
- Updates every minute
- Changes color when < 4 hours remaining
- Shows “EOL” badge when expired

-----

## Pages — Key Details

### Dashboard

- Greeting with current character name
- Activity feed (full, unfiltered)
- Summary cards: active connections count, sigs to scan, PI expiring soon, members online
- Quick links to chain map

### ChainMap

- Full-screen React Flow canvas
- Sidebar: filtered activity feed (chain/sig/mass events)
- SigPanel slides in from right when a system is selected
- MassPanel slides in from right when a connection is selected
- Both panels are Mantine `Drawer` components, position right
- Toolbar: add system, add connection, fit view, center on home

### SigPanel

- Table of sigs for the selected system
- Paste input textarea at top for bulk import
- Inline editing for sig type/name
- EOL countdown on WH sigs
- “Add Connection” action on WH sigs → creates ChainConnection
- Delete/clear individual sigs

### MassPanel

- Mass health bar at top
- Mass pass log table (ship, direction, mass, who, when)
- “Log Pass” form: ship dropdown (favorites first), direction toggle, prop mod toggle
- Rolling calculator section: available ships → recommended jump combos
- “Start Rolling Op” button → initiates RollingSession

-----

## Responsive Behavior

- Desktop-first layouts using Mantine’s responsive props
- Chain map: hidden on screens < 768px, show a “use desktop for chain map” message
- PI Tracker, Kill Feed, Fittings: stack to single column on mobile
- Sidebar nav collapses to hamburger menu on small screens
- No mobile-specific layouts or components
