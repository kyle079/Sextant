# Sextant — Backend Specification

## Overview

Sextant is a self-hosted WH corp management tool for Eve Online. The backend is a .NET 10 Minimal API serving a React frontend, real-time updates via SignalR, and integrating with CCP’s ESI API for game data.

-----

## Stack

|Component        |Technology                                        |
|-----------------|--------------------------------------------------|
|Framework        |.NET 10 Minimal API                               |
|ORM              |EF Core + SQLite                                  |
|Real-time        |SignalR                                           |
|API Docs         |Scalar (OpenAPI)                                  |
|Client Generation|NSwag (TypeScript frontend client + C# ESI client)|
|Auth             |Eve SSO OAuth2 (PKCE)                             |
|Token Encryption |ASP.NET Core Data Protection API                  |
|Caching          |IMemoryCache                                      |
|Resilience       |Microsoft.Extensions.Http.Resilience              |
|Hosting          |Docker container, self-hosted                     |

-----

## Project Structure

```
Sextant/
├── Program.cs
├── appsettings.json
├── nswag.json
├── Dockerfile
├── docker-compose.yml
│
├── Data/
│   └── AppDbContext.cs
│
├── Models/
│   ├── User.cs
│   ├── CharacterToken.cs
│   ├── ChainSystem.cs
│   ├── ChainConnection.cs
│   ├── Signature.cs
│   ├── MassPass.cs
│   ├── RollingSession.cs
│   ├── Fitting.cs
│   ├── SiteLog.cs
│   ├── ActivityEvent.cs
│   ├── WormholeType.cs          # SDE cache
│   └── WormholeSystem.cs        # SDE cache
│
├── Endpoints/
│   ├── AuthEndpoints.cs
│   ├── ChainEndpoints.cs
│   ├── SigEndpoints.cs
│   ├── MassEndpoints.cs
│   ├── RollingEndpoints.cs
│   ├── PiEndpoints.cs
│   ├── FittingEndpoints.cs
│   ├── SiteLogEndpoints.cs
│   ├── StructureEndpoints.cs
│   ├── KillEndpoints.cs
│   ├── MemberEndpoints.cs
│   ├── ActivityEndpoints.cs
│   └── AdminEndpoints.cs
│
├── Hubs/
│   └── SextantHub.cs             # single SignalR hub
│
├── ESI/
│   ├── EsiClient.generated.cs    # NSwag-generated from ESI swagger.json
│   ├── AuthHandler.cs            # injects bearer token, handles refresh
│   ├── RateLimitHandler.cs       # respects X-Ratelimit headers
│   └── CacheHandler.cs           # caches per Expires header
│
└── Services/
    ├── TokenService.cs           # encrypt/decrypt/refresh ESI tokens
    ├── SdeRefreshService.cs      # pulls WH data from Anoik.is / ESI
    └── ZKillService.cs           # pulls corp kills from zKillboard API
```

No repositories. No generic abstractions. Inject `AppDbContext` directly into endpoints.

-----

## Data Model

### User & Auth

```
User
├── Id                  int PK
├── CharacterName       string
├── PrimaryCharacterId  long
├── Role                enum (Admin, Member, ReadOnly)
├── CreatedAt           DateTime
└── LastLogin           DateTime

CharacterToken
├── Id                  int PK
├── UserId              int FK → User
├── CharacterId         long (unique)
├── CharacterName       string
├── CorporationId       int
├── EncryptedRefreshToken  string
├── Scopes              string
├── LastRefreshed       DateTime
└── IsActive            bool
```

### Chain Map

```
ChainSystem
├── Id                  int PK
├── SolarSystemId       long (Eve system ID)
├── SystemName          string
├── WormholeClass       int? (C1-C6, null for k-space)
├── Effect              string? (Pulsar, Wolf-Rayet, etc.)
├── Statics             string? (JSON array of WH type codes)
├── Notes               string?
├── AddedBy             long (character ID)
├── AddedAt             DateTime
├── DeletedAt           DateTime? (soft delete)
└── IsHome              bool

ChainConnection
├── Id                  int PK
├── FromSystemId        int FK → ChainSystem
├── ToSystemId          int FK → ChainSystem
├── SignatureId         int? FK → Signature
├── WormholeTypeCode    string? (e.g., "N062", "K162")
├── MaxMassKg           long? (from SDE)
├── MaxJumpMassKg       long? (from SDE)
├── LifetimeHours       int? (from SDE)
├── MassStatus          enum (Fresh, Half, Critical)
├── LifeStatus          enum (Stable, EOL)
├── ScannedBy           long (character ID)
├── ScannedAt           DateTime
├── EolAt               DateTime? (scannedAt + lifetimeHours)
├── CollapsedAt         DateTime? (soft delete)
└── DeletedAt           DateTime? (soft delete)
```

### Signatures

```
Signature
├── Id                  int PK
├── ChainSystemId       int FK → ChainSystem
├── SignatureId         string (in-game sig ID, e.g., "ABC-123")
├── GroupType           enum (Wormhole, Data, Relic, Gas, Combat, Unknown)
├── Name                string? (site name if scanned)
├── ScanPercentage      float?
├── ScannedBy           long (character ID)
├── ScannedAt           DateTime
├── UpdatedAt           DateTime
├── ClearedAt           DateTime? (soft delete)
└── DeletedAt           DateTime? (soft delete)

Unique constraint: (ChainSystemId, SignatureId) — one sig ID per system
```

### Mass Tracking

```
MassPass
├── Id                  int PK
├── ConnectionId        int FK → ChainConnection
├── CharacterId         long
├── ShipTypeId          int (Eve type ID)
├── ShipName            string
├── ShipMassKg          long (from ESI type data)
├── PropModActive       bool (doubles effective mass)
├── EffectiveMassKg     long (computed: shipMass * (propMod ? 2 : 1))
├── Direction           enum (Out, Back)
├── Timestamp           DateTime
└── Notes               string?

RollingSession
├── Id                  int PK
├── ConnectionId        int FK → ChainConnection
├── InitiatedBy         long (character ID)
├── StartedAt           DateTime
├── CompletedAt         DateTime?
├── Status              enum (Active, Completed, Cancelled)
└── Notes               string?
```

### Fittings

```
Fitting
├── Id                  int PK
├── Name                string
├── ShipTypeId          int
├── ShipName            string
├── DoctrineName        string? (e.g., "Armor Brawl", "Rolling Ships")
├── Role                string? (e.g., "DPS", "Logi", "Roller")
├── EftFormat           string (the full EFT fit string)
├── AddedBy             long (character ID)
├── AddedAt             DateTime
├── UpdatedAt           DateTime
└── DeletedAt           DateTime? (soft delete)
```

### Site Log

```
SiteLog
├── Id                  int PK
├── ChainSystemId       int FK → ChainSystem
├── SiteName            string
├── SiteType            enum (Combat, Gas, Relic, Data)
├── EstimatedIskValue   long?
├── CompletedBy         string (JSON array of character IDs)
├── CompletedAt         DateTime
└── Notes               string?
```

### Activity Feed

```
ActivityEvent
├── Id                  int PK
├── Type                enum (Sig, Chain, Mass, Rolling, PI, Kill, Fit, Structure, Site, Auth)
├── CharacterId         long
├── CharacterName       string
├── Summary             string (human-readable one-liner)
├── DetailJson          string? (structured payload)
├── Timestamp           DateTime
└── SystemId            long? (for chain-contextual filtering)
```

### SDE Cache

```
WormholeType
├── TypeCode            string PK (e.g., "N062", "C140")
├── SourceClassRange    string (e.g., "C2")
├── DestinationClass    string (e.g., "C5")
├── MaxMassKg           long
├── MaxJumpMassKg       long
├── LifetimeHours       int
├── MaxShipSize         string (e.g., "Large", "Medium")
├── LastUpdated         DateTime

WormholeSystem
├── SolarSystemId       long PK
├── SystemName          string
├── WormholeClass       int
├── Effect              string?
├── Statics             string (JSON array of type codes)
├── Region              string
├── ConstellationId     long
├── LastUpdated         DateTime
```

### EF Core Conventions

- Global query filters for soft-delete (`DeletedAt == null`)
- Use `.IgnoreQueryFilters()` when accessing historical data
- DateTimes stored as UTC
- JSON columns for small embedded arrays (statics, completedBy)

-----

## API Endpoints

All endpoints documented with `.WithTags()`, `.WithSummary()`, `.WithDescription()`, `.Produces<T>()` for Scalar and NSwag.

### Auth

|Method|Route                           |Description                                  |
|------|--------------------------------|---------------------------------------------|
|GET   |`/auth/login`                   |Redirect to Eve SSO                          |
|GET   |`/auth/callback`                |SSO callback, create/update user, store token|
|POST  |`/auth/logout`                  |Clear session                                |
|GET   |`/auth/me`                      |Current user + linked characters             |
|POST  |`/auth/characters`              |Link an alt character                        |
|DELETE|`/auth/characters/{characterId}`|Unlink a character                           |

### Chain

|Method|Route                    |Description                        |
|------|-------------------------|-----------------------------------|
|GET   |`/chain/systems`         |All active systems in chain        |
|POST  |`/chain/systems`         |Add a system to chain              |
|PUT   |`/chain/systems/{id}`    |Update system notes                |
|DELETE|`/chain/systems/{id}`    |Soft-delete system                 |
|GET   |`/chain/connections`     |All active connections             |
|POST  |`/chain/connections`     |Add a connection between systems   |
|PUT   |`/chain/connections/{id}`|Update connection (WH type, status)|
|DELETE|`/chain/connections/{id}`|Soft-delete (collapse) connection  |

### Signatures

|Method|Route                   |Description                         |
|------|------------------------|------------------------------------|
|GET   |`/sigs/{systemId}`      |All active sigs for a system        |
|POST  |`/sigs/{systemId}`      |Add a single sig                    |
|POST  |`/sigs/{systemId}/paste`|Bulk import from Eve clipboard paste|
|PUT   |`/sigs/{id}`            |Update sig details                  |
|DELETE|`/sigs/{id}`            |Soft-delete (clear) sig             |

### Mass Tracking

|Method|Route                        |Description                                |
|------|-----------------------------|-------------------------------------------|
|GET   |`/mass/{connectionId}`       |All passes for a connection + running total|
|POST  |`/mass/{connectionId}`       |Log a mass pass                            |
|DELETE|`/mass/{id}`                 |Remove erroneous pass (admin)              |
|GET   |`/mass/{connectionId}/status`|Current mass status + remaining estimate   |

### Rolling

|Method|Route                          |Description                              |
|------|-------------------------------|-----------------------------------------|
|POST  |`/rolling/{connectionId}/start`|Start a rolling session                  |
|POST  |`/rolling/{sessionId}/checkin` |Check in with available ships            |
|GET   |`/rolling/{sessionId}`         |Current session state + recommended order|
|POST  |`/rolling/{sessionId}/complete`|Mark hole as collapsed                   |
|POST  |`/rolling/{sessionId}/cancel`  |Cancel session                           |

### PI (proxied from ESI)

|Method|Route              |Description                                   |
|------|-------------------|----------------------------------------------|
|GET   |`/pi`              |All linked characters’ PI colonies with expiry|
|GET   |`/pi/{characterId}`|Single character’s PI detail                  |

### Fittings

|Method|Route                      |Description                      |
|------|---------------------------|---------------------------------|
|GET   |`/fittings`                |All active fits                  |
|GET   |`/fittings?doctrine={name}`|Filter by doctrine               |
|POST  |`/fittings`                |Add a fit                        |
|PUT   |`/fittings/{id}`           |Update a fit                     |
|DELETE|`/fittings/{id}`           |Soft-delete fit                  |
|GET   |`/fittings/{id}/eft`       |Raw EFT string for clipboard copy|

### Site Log

|Method|Route                 |Description                |
|------|----------------------|---------------------------|
|GET   |`/sites`              |All logged sites, paginated|
|GET   |`/sites?systemId={id}`|Filter by system           |
|POST  |`/sites`              |Log a completed site       |

### Members (proxied from ESI)

|Method|Route                   |Description                                      |
|------|------------------------|-------------------------------------------------|
|GET   |`/members`              |All corp members with online/location/ship status|
|GET   |`/members/{characterId}`|Single member detail                             |

### Structures (proxied from ESI)

|Method|Route        |Description                             |
|------|-------------|----------------------------------------|
|GET   |`/structures`|Corp structures with fuel/service status|

### Kills

|Method|Route   |Description                      |
|------|--------|---------------------------------|
|GET   |`/kills`|Recent corp kills from zKillboard|

### Activity Feed

|Method|Route                    |Description                  |
|------|-------------------------|-----------------------------|
|GET   |`/activity`              |Full activity feed, paginated|
|GET   |`/activity?type={type}`  |Filtered by event type       |
|GET   |`/activity?systemId={id}`|Filtered by system           |

### Admin

|Method|Route                   |Description           |
|------|------------------------|----------------------|
|POST  |`/admin/sde/refresh`    |Force SDE data refresh|
|GET   |`/admin/users`          |All users             |
|PUT   |`/admin/users/{id}/role`|Change user role      |

-----

## SignalR Hub

Single hub: `SextantHub`

### Events broadcast to all connected clients:

|Event                    |Payload                  |Trigger                  |
|-------------------------|-------------------------|-------------------------|
|`SigAdded`               |Signature                |POST /sigs               |
|`SigUpdated`             |Signature                |PUT /sigs                |
|`SigCleared`             |{ systemId, sigId }      |DELETE /sigs             |
|`SigsBulkUpdated`        |{ systemId, sigs[] }     |POST /sigs/paste         |
|`SystemAdded`            |ChainSystem              |POST /chain/systems      |
|`SystemRemoved`          |{ systemId }             |DELETE /chain/systems    |
|`ConnectionAdded`        |ChainConnection          |POST /chain/connections  |
|`ConnectionUpdated`      |ChainConnection          |PUT /chain/connections   |
|`ConnectionCollapsed`    |{ connectionId }         |DELETE /chain/connections|
|`MassPassLogged`         |MassPass + updated status|POST /mass               |
|`MassStatusChanged`      |{ connectionId, status } |When threshold crossed   |
|`RollingSessionStarted`  |RollingSession           |POST /rolling/start      |
|`RollingSessionUpdated`  |RollingSession           |POST /rolling/checkin    |
|`RollingSessionCompleted`|{ sessionId }            |POST /rolling/complete   |
|`ActivityEvent`          |ActivityEvent            |Any logged event         |

All endpoints that mutate chain/sig/mass data fire the corresponding SignalR event after persisting to the database.

-----

## ESI Integration

### Generated Client

NSwag generates `EsiClient` from `https://esi.evetech.net/latest/swagger.json`. This is a typed C# HTTP client covering all ESI endpoints.

### HTTP Pipeline

```
EsiClient → AuthHandler → RateLimitHandler → CacheHandler → HttpClient → ESI
```

Registered in `Program.cs`:

```csharp
builder.Services.AddHttpClient<EsiClient>()
    .AddHttpMessageHandler<AuthHandler>()
    .AddHttpMessageHandler<RateLimitHandler>()
    .AddHttpMessageHandler<CacheHandler>()
    .AddStandardResilienceHandler();
```

### AuthHandler

- Injects bearer token from IMemoryCache (access token)
- On cache miss: retrieves encrypted refresh token from DB via TokenService
- Calls ESI token endpoint to get new access token
- Caches new access token with TTL matching expiry
- Updates refresh token in DB if rotated

### RateLimitHandler

- Reads `X-Ratelimit-Remaining` and `X-Ratelimit-Reset` on every response
- If remaining drops below threshold, introduces delay via SemaphoreSlim
- On 429: respects `Retry-After` header

### CacheHandler

- Reads `Expires` header on every response
- Caches response body in IMemoryCache keyed by URL + character ID
- Returns cached response if within TTL window
- Skips caching for POST/PUT/DELETE

### Resilience Handler

- .NET built-in via `AddStandardResilienceHandler()`
- Exponential backoff on transient failures (503, timeouts)
- Circuit breaker on sustained failures

### ESI Scopes Requested

```
esi-location.read_location.v1
esi-location.read_ship_type.v1
esi-location.read_online.v1
esi-planets.manage_planets.v1
esi-killmails.read_killmails.v1
esi-fittings.read_fittings.v1
esi-fittings.write_fittings.v1
esi-corporations.read_structures.v1
esi-universe.read_structures.v1
esi-characters.read_corporation_roles.v1
```

-----

## Token Storage

- Refresh tokens encrypted via Data Protection API before storage in SQLite
- Data Protection keys persisted to `/app/keys` volume mount
- Access tokens cached in IMemoryCache only, never persisted
- TokenService handles encrypt/decrypt/refresh lifecycle

-----

## SDE Refresh

`SdeRefreshService` runs on a configurable schedule (default: weekly, Tuesday after 11:15 UTC downtime).

Sources:

- WH system data (class, statics, effects): Anoik.is API or eve-ref
- WH type data (mass limits, lifetime): ESI universe/types endpoint

Process:

1. Pull data from source
1. Upsert into WormholeSystem / WormholeType tables
1. Log refresh result to ActivityEvent

Also exposed as manual trigger: `POST /admin/sde/refresh`

-----

## Docker / Hosting

Single container serving both API and static frontend files.

```yaml
services:
  sextant:
    build: .
    ports:
      - "8080:8080"
    volumes:
      - ./data:/app/data       # SQLite database
      - ./keys:/app/keys       # Data Protection keys
    environment:
      - ESI_CLIENT_ID=xxx
      - ESI_CLIENT_SECRET=xxx
      - ESI_CALLBACK_URL=https://sextant.yourdomain.com/auth/callback
      - ALLOWED_CORP_ID=123456789
```

HTTPS via Caddy or Traefik reverse proxy (required for browser push notifications and Eve SSO callback).

-----

## Eve Downtime Handling

- Eve daily downtime: ~11:00 UTC, 5-15 minutes
- CacheHandler serves cached ESI data during outage
- RateLimitHandler backs off on ESI errors
- No auto-purge of chain data
- Frontend shows stale-data banner when ESI is unreachable
