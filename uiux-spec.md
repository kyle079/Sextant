# Sextant — UI/UX Specification

## Overview

Sextant is a desktop-first WH corp management tool. The design prioritizes operational clarity — information density without clutter, fast interactions for time-sensitive WH operations, and a dark aesthetic that complements a multi-monitor Eve Online setup.

-----

## Design Principles

1. **Information density over whitespace.** WH ops require seeing a lot of data at once. Don’t hide things behind extra clicks when screen real estate allows showing them.
1. **Fast path for common actions.** Pasting sigs, logging mass passes, and adding connections should be 1-2 clicks max.
1. **Dark theme, always.** No light mode. Eve is dark, second monitors should match.
1. **Mantine does the work.** Don’t build custom UI when a Mantine component exists. Custom components only for the five identified cases (ChainNode, ChainEdge, ActivityFeed, MassHealthBar, EolCountdown).
1. **Desktop-first, mobile-tolerant.** Layouts should not break on mobile but are not optimized for it. The chain map explicitly requires desktop.

-----

## Color System

Base: Mantine dark theme

|Role             |Color                    |Usage                                     |
|-----------------|-------------------------|------------------------------------------|
|Primary          |Cyan/Teal (`cyan.6`)     |Interactive elements, links, active states|
|Background       |Dark (`dark.8`, `dark.9`)|Page and card backgrounds                 |
|Surface          |Dark (`dark.7`)          |Cards, panels, sidebar                    |
|Accent — Safe    |Green (`green.6`)        |Fresh mass status, stable connections     |
|Accent — Warning |Yellow (`yellow.6`)      |Half mass, EOL approaching                |
|Accent — Critical|Red (`red.6`)            |Critical mass, structure alerts           |
|Text — Primary   |Gray (`gray.1`)          |Main body text                            |
|Text — Secondary |Gray (`gray.5`)          |Labels, timestamps, secondary info        |

WH Class color coding for chain map nodes:

|Class   |Color     |Reasoning                 |
|--------|----------|--------------------------|
|C1–C2   |Blue      |Low-class, relatively safe|
|C3      |Teal      |Mid-class                 |
|C4      |Yellow    |Requires more coordination|
|C5–C6   |Red/Orange|High-class, dangerous     |
|High-sec|Green     |Safe k-space              |
|Low-sec |Yellow    |Caution                   |
|Null-sec|Red       |Dangerous k-space         |

-----

## Layout Structure

```
┌─────────────────────────────────────────────────────┐
│  Top Bar: Sextant logo | Active character selector | │
│           Online members count | Eve DT banner      │
├──────┬──────────────────────────────────────────────┤
│      │                                              │
│  S   │                                              │
│  i   │            Main Content Area                 │
│  d   │                                              │
│  e   │                                              │
│  b   │                                              │
│  a   │                                              │
│  r   │                                              │
│      │                                              │
├──────┴──────────────────────────────────────────────┤
│  Status Bar: SignalR connection status | ESI status  │
└─────────────────────────────────────────────────────┘
```

### Top Bar

- Sextant logo/name (left)
- Active character selector dropdown (shows portrait + name, switch between linked characters)
- Online members indicator: small avatar stack of online corp members
- Eve downtime banner: appears 15 min before and during daily DT (yellow warning bar)

### Sidebar

- Fixed width (~220px), collapsible to icon-only (~60px)
- Navigation items with icons:
  - Dashboard (home icon)
  - Chain Map (network/graph icon)
  - PI Tracker (planet icon)
  - Fittings (wrench/ship icon)
  - Site Log (ISK/chart icon)
  - Structures (citadel icon)
  - Kill Feed (skull/crosshairs icon)
  - Members (people icon)
  - Settings (gear icon) — bottom of sidebar
- Active route highlighted with primary color
- Collapse toggle at bottom

### Status Bar

- SignalR connection indicator: green dot = connected, red = disconnected (auto-reconnecting)
- ESI status: green = normal, yellow = degraded, red = unreachable
- Last SDE refresh timestamp

-----

## Page Specifications

### Login Page

No sidebar, no top bar. Centered card layout.

Content:

- Sextant logo (large)
- “Login with Eve Online” button (uses CCP’s official SSO button styling)
- Brief tagline: “WH Corp Operations Hub”

After SSO redirect and callback:

- If corp membership valid → redirect to Dashboard
- If not in corp → error card: “Your character is not a member of [Corp Name]. Contact your CEO.”

### Dashboard

The “what happened while I was gone” screen.

Layout:

```
┌────────────────────────────────────────────┐
│  Summary Cards (4 across)                  │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐     │
│  │Active│ │Unscan│ │PI Exp│ │Online│      │
│  │Conns │ │Sigs  │ │Soon  │ │Membrs│      │
│  └──────┘ └──────┘ └──────┘ └──────┘     │
├────────────────────────────────────────────┤
│  Activity Feed (full width, scrollable)    │
│  ┌────────────────────────────────────┐    │
│  │ [14:23] Kyle added WH: Home→J1452 │    │
│  │ [14:25] Kyle logged mass: Mega... │    │
│  │ [14:30] J145201 is now HALF MASS  │    │
│  │ [15:10] Moon's PI expires in 4h   │    │
│  │ ...                                │    │
│  └────────────────────────────────────┘    │
└────────────────────────────────────────────┘
```

Summary cards:

- **Active Connections**: count of non-collapsed connections. Click → go to Chain Map.
- **Unscanned Sigs**: count of sigs at < 100% scan. Click → go to Chain Map.
- **PI Expiring Soon**: count of colonies expiring within 24h. Click → go to PI Tracker.
- **Online Members**: count currently online. Shows avatar stack on hover.

Activity feed:

- Full ActivityFeed component, unfiltered
- Each entry: timestamp, character name, icon by event type, summary text
- Scrollable, paginated (load more on scroll)
- Color-coded left border by event type

### Chain Map

The core operational view. Full-width content area (sidebar remains).

Layout:

```
┌──────────────────────────────────────────────────────┐
│  Toolbar: [+ System] [+ Connection] [Fit View]      │
│           [Center Home] [Toggle Activity]            │
├──────────────────────────────────┬───────────────────┤
│                                  │ Activity sidebar  │
│                                  │ (collapsible)     │
│     React Flow Canvas            │                   │
│     (pan, zoom, drag)            │ Chain/Sig/Mass    │
│                                  │ events only       │
│     ┌────┐     ┌────┐          │                   │
│     │Home├────►│J145│          │                   │
│     └────┘     └──┬─┘          │                   │
│                   │             │                   │
│                ┌──▼─┐          │                   │
│                │Jita│          │                   │
│                └────┘          │                   │
│                                  │                   │
├──────────────────────────────────┴───────────────────┤
│  (SigPanel or MassPanel slides in from right when    │
│   a system or connection is selected)                │
└──────────────────────────────────────────────────────┘
```

#### Chain Map Interactions

**Pan & Zoom:**

- Mouse drag to pan canvas
- Scroll wheel to zoom
- Minimap in bottom-right corner (React Flow built-in)

**Selecting a system (click on node):**

- Node gets highlighted border
- SigPanel drawer opens from the right
- If MassPanel was open, it closes
- Zustand: `selectedSystemId` set, `sigPanelOpen = true`

**Selecting a connection (click on edge):**

- Edge gets highlighted
- MassPanel drawer opens from the right
- If SigPanel was open, it closes
- Zustand: `selectedConnectionId` set, `massPanelOpen = true`

**Clicking empty canvas:**

- Deselects everything
- Closes any open panel

**Adding a system:**

- Toolbar button opens a modal
- Search/autocomplete for J-number (pulls from WormholeSystem SDE cache)
- On selection: system added to canvas, WH class/effect/statics auto-populated
- Node appears at a default position near existing nodes (user can drag to arrange)

**Adding a connection:**

- Toolbar button or drag from one node’s handle to another (React Flow native)
- Modal opens: select WH type from dropdown (filtered by source system class statics)
- Auto-populates mass limits and lifetime from SDE
- Connection created, edge rendered with appropriate styling

**Removing a connection (collapse):**

- Right-click on edge → context menu → “Mark as Collapsed”
- Confirmation dialog: “Mark connection to [system] as collapsed?”
- Soft-delete, edge disappears, activity event logged
- If the destination system has no other connections, prompt: “Remove [system] from chain?”

#### SigPanel (Drawer, right side)

```
┌─ Signatures: J145201 (C3 Wolf-Rayet) ────────────┐
│                                                    │
│  ┌──────────────────────────────────────────────┐  │
│  │ Paste sigs from game here...                 │  │
│  │ (textarea, Ctrl+V)                           │  │
│  └──────────────────────────────────────────────┘  │
│  [Import Paste]                                    │
│                                                    │
│  ┌────┬──────┬──────────────┬─────┬──────┬─────┐  │
│  │ ID │ Type │ Name         │ Scan│ By   │ EOL │  │
│  ├────┼──────┼──────────────┼─────┼──────┼─────┤  │
│  │ABC │ WH   │ Unstable WH  │100% │ Kyle │ 18h │  │
│  │DEF │ Data │ Forgotten... │ 75% │ Moon │  -  │  │
│  │GHI │  ?   │              │  0% │  -   │  -  │  │
│  └────┴──────┴──────────────┴─────┴──────┴─────┘  │
│                                                    │
│  Row actions (on hover):                           │
│  - WH sigs: [Add Connection] button               │
│  - All sigs: [Edit] [Clear] actions                │
└────────────────────────────────────────────────────┘
```

- Drawer width: ~450px
- Header shows system name, class badge, effect if any
- Paste textarea at top — paste and click Import (or keyboard shortcut)
- Table below with all active sigs for this system
- Type column: icon + label, color-coded by sig type
- EOL column: EolCountdown component for WH sigs, dash for others
- Row hover reveals action buttons
- “Add Connection” on WH sig: opens add-connection modal pre-populated with this sig

#### MassPanel (Drawer, right side)

```
┌─ Connection: Home → J145201 (N062) ──────────────┐
│                                                    │
│  Status: HALF MASS                                 │
│  ┌──────────────────────────────────────────────┐  │
│  │ ████████████░░░░░░░░░░░  1,200M / 2,000M kg │  │
│  └──────────────────────────────────────────────┘  │
│  Remaining: ~800,000,000 kg                        │
│  EOL: 16h 23m remaining                            │
│  Max jump: 300,000,000 kg                          │
│                                                    │
│  ── Log a Pass ──────────────────────────────────  │
│  Ship: [▾ Megathron          ]                     │
│  Direction: [Out] [Back]                           │
│  Prop mod: [✓]                                     │
│  Effective mass: 200,000,000 kg                    │
│  [Log Pass]                                        │
│                                                    │
│  ── Pass History ────────────────────────────────  │
│  ┌──────────┬───────┬──────────┬──────┬────────┐  │
│  │ Ship     │ Dir   │ Mass     │ Who  │ When   │  │
│  ├──────────┼───────┼──────────┼──────┼────────┤  │
│  │ Mega     │ Out   │ 200M kg  │ Kyle │ 14:25  │  │
│  │ Mega     │ Back  │ 200M kg  │ Kyle │ 14:26  │  │
│  │ Orca     │ Out   │ 500M kg  │ Moon │ 14:28  │  │
│  └──────────┴───────┴──────────┴──────┴────────┘  │
│                                                    │
│  ── Rolling Calculator ──────────────────────────  │
│  Available ships: [✓ Mega] [✓ Orca] [✓ Higgs BS]  │
│  Recommended: Mega (prop on) out + back = ~400M    │
│  Remaining after: ~400M kg                         │
│                                                    │
│  [Start Rolling Op]                                │
└────────────────────────────────────────────────────┘
```

- Drawer width: ~450px
- MassHealthBar at top — color-coded, animated
- “Log a Pass” form: ship dropdown (searchable, favorites/recent at top), direction toggle, prop mod checkbox
- Ship dropdown shows ship name + mass. Mass auto-looked up from ESI cache.
- Prop mod toggle recalculates effective mass in real-time (doubles mass)
- Pass history table below, reverse chronological
- Rolling calculator section: check available ships, see recommended combos
- “Start Rolling Op” button opens a collaborative rolling session

### PI Tracker

```
┌────────────────────────────────────────────────────┐
│  Sort: [Expiring Soonest ▾]                        │
│                                                    │
│  ┌──────────┬──────────┬─────────┬───────┬──────┐  │
│  │Character │ Planet   │ Product │Expires│Status│  │
│  ├──────────┼──────────┼─────────┼───────┼──────┤  │
│  │ Kyle     │ P4 Lava  │ Coolant │ 2h 15m│ ⚠️   │  │
│  │ Moon     │ P2 Ocean │ Electro │ 8h 30m│ ✓    │  │
│  │ Kyle     │ P3 Gas   │ Oxidiz  │ 1d 4h │ ✓    │  │
│  └──────────┴──────────┴─────────┴───────┴──────┘  │
│                                                    │
│  All data from ESI. Refresh: every 30 min.         │
└────────────────────────────────────────────────────┘
```

- Table sorted by expiry by default
- Color-coded status: red (< 4h), yellow (< 24h), green (> 24h)
- Clicking a row expands to show planet detail (extraction heads, routes)
- No manual data entry — 100% ESI

### Fittings

```
┌────────────────────────────────────────────────────┐
│  Doctrine: [All ▾]  Role: [All ▾]  [+ Add Fit]    │
│                                                    │
│  ┌──────────────────────────────────────────────┐  │
│  │ ⛵ Megathron                                  │  │
│  │ Doctrine: Rolling Ships | Role: Roller        │  │
│  │ Added by Kyle · 3 days ago                    │  │
│  │ [View Fit] [Copy EFT] [Delete]               │  │
│  ├──────────────────────────────────────────────┤  │
│  │ ⛵ Praxis                                     │  │
│  │ Doctrine: Home Defense | Role: DPS            │  │
│  │ Added by Moon · 1 week ago                    │  │
│  │ [View Fit] [Copy EFT] [Delete]               │  │
│  └──────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────┘
```

- Card list, filterable by doctrine and role
- “View Fit” opens a modal with the full EFT text + ship stats from ESI (mass, hull type)
- “Copy EFT” copies the EFT string to clipboard (one-click paste into Eve)
- “Add Fit” modal: paste EFT format, app parses ship name, user adds doctrine/role tags

### Site Log

- Table: system, site name, site type, estimated ISK, completed by, date
- “Log Site” modal: select system (from active chain), site type, name, ISK estimate, who participated
- Summary stats at top: total ISK this week/month, sites by type chart
- Filterable by system, type, date range

### Structures

- Card per structure (Fortizar, Astrahus, Raitaru, etc.)
- Shows: name, type, system, fuel level (bar + days remaining), online services, state
- Color-coded fuel bar: green (> 7 days), yellow (< 7 days), red (< 24 hours)
- All from ESI, auto-refreshing

### Kill Feed

- Card-based feed of recent kills/losses
- Shows: victim ship + name, attacker(s), system, ISK value, timestamp
- Corp kills have green border, losses have red border
- Links to full kill mail on zKillboard
- Data from zKillboard API, cached with short TTL

### Members

- Card grid of corp members
- Each card: character portrait, name, online status (green/red dot), current system, current ship
- Sort by: online first, then alphabetical
- Clicking a member → shows their linked alts, PI status, recent activity

### Settings

Three tabs:

**My Characters:**

- List of linked characters with portraits
- “Add Character” button → Eve SSO re-auth for alt
- “Remove” to unlink an alt

**Corp Settings (admin only):**

- Allowed corp ID
- Default notification preferences
- Home system designation

**SDE Data (admin only):**

- Last refresh timestamp
- “Force Refresh” button
- Status of last refresh (success/failure, records updated)

-----

## Interaction Patterns

### Keyboard Shortcuts (Chain Map)

|Shortcut|Action                                         |
|--------|-----------------------------------------------|
|`V`     |Paste sigs (focuses paste textarea in SigPanel)|
|`Esc`   |Close open panel, deselect                     |
|`F`     |Fit view (zoom to show all nodes)              |
|`H`     |Center on home system                          |
|`Delete`|Remove selected (with confirmation)            |

### Right-Click Context Menus

**On system node:**

- View signatures
- Add connection from here
- Set as home system (admin)
- Remove system

**On connection edge:**

- View mass tracker
- Mark as EOL
- Mark as collapsed
- Start rolling op

### Toast Notification Behaviors

- Info toasts: bottom-right, auto-dismiss 5s
- Warning toasts: bottom-right, auto-dismiss 10s, yellow accent
- Critical toasts: bottom-right, persist until dismissed, red accent, optional sound

### Loading States

- Use Mantine `Skeleton` components for initial page loads
- Use Mantine `LoadingOverlay` for actions (saving, importing)
- SignalR disconnection: subtle red dot in status bar, no blocking overlay
- ESI errors: inline error messages on affected components, not full-page errors

-----

## Responsive Behavior

### Desktop (> 1200px)

- Sidebar expanded with labels
- Chain map at full size
- Panels are 450px drawers
- All features available

### Tablet (768px — 1200px)

- Sidebar collapsed to icons only
- Chain map still functional but tighter
- Panels take full width as bottom sheets instead of side drawers
- All features available

### Mobile (< 768px)

- Sidebar becomes hamburger menu
- Chain map replaced with message: “Chain map requires a desktop browser”
- PI, Fittings, Kills, Members render as stacked single-column layouts
- Dashboard fully functional
- Settings fully functional

-----

## Empty States

Every page should have a helpful empty state, not a blank screen:

|Page         |Empty State                                                        |
|-------------|-------------------------------------------------------------------|
|Chain Map    |“No systems mapped yet. Click + System to add your home.”          |
|Signatures   |“No signatures scanned. Paste from your probe scanner.”            |
|Mass Tracker |“No passes logged for this connection.”                            |
|PI Tracker   |“No PI colonies found. Make sure characters have granted PI scope.”|
|Fittings     |“No doctrine fits yet. Add your first fit.”                        |
|Site Log     |“No sites logged yet.”                                             |
|Kill Feed    |“No recent kills or losses.”                                       |
|Activity Feed|“Nothing has happened yet. Start by mapping your home system.”     |
