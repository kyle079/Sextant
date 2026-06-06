# Sextant

Containerized deployment supports separate dev and prod profiles:

- `dev` profile: backend + SQLite (no backup service)
- `prod` profile: backend + PostgreSQL + scheduled backup sidecar

The backend serves the frontend SPA from static files in `wwwroot` inside the app container. A separate frontend container is not required for production.

## Docker Compose profiles

### Development (SQLite, no backup)

```bash
docker compose --profile dev up --build -d
```

### Production (PostgreSQL + backups)

Required env var:

- `POSTGRES_PASSWORD`

Optional env vars:

- `POSTGRES_DB` (default: `sextant`)
- `POSTGRES_USER` (default: `sextant`)
- `BACKUP_SCHEDULE` (default: `@daily`)
- `BACKUP_KEEP_DAYS` (default: `7`)
- `BACKUP_KEEP_WEEKS` (default: `4`)
- `BACKUP_KEEP_MONTHS` (default: `6`)

Run:

```bash
docker compose --profile prod up --build -d
```

Backups are written to `./backups` on the host. Point that path to your NAS mount if you want direct NAS storage.