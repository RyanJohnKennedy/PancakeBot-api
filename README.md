# Pancake API

## Trackmania configuration

Trackmania settings are under the `Trackmania` section in `appsettings.json`.
The committed file contains endpoint defaults, South Africa as the default region,
a daily leaderboard size of 5, and the 5/4/3/2/1 points table. Add or change an
entry in `Trackmania:Regions` and set `Trackmania:DefaultRegion` to switch regions
without changing code.

Keep credentials out of `appsettings.json`. For local development, set them with
user secrets:

```bash
dotnet user-secrets set "Trackmania:Login" "your-login"
dotnet user-secrets set "Trackmania:Password" "your-password-or-token"
dotnet user-secrets set "Trackmania:Email" "you@example.com"
dotnet user-secrets set "Trackmania:ClientId" "your-oauth-client-id"
dotnet user-secrets set "Trackmania:ClientSecret" "your-oauth-client-secret"
```

The Nadeo core and live clients obtain their service tokens with the configured
basic credentials and send them as `Authorization: nadeo_v1 t={token}`. The live
leaderboard client requests a `NadeoLiveServices` token.

## Local Development Setup

This project uses a local PostgreSQL database running in Docker.

---

## First-Time Setup (Run Once)

Creates the PostgreSQL container.

```bash
docker run --name Pancake-db \
  -e POSTGRES_USER=Pancake \
  -e POSTGRES_PASSWORD=PancakePassword \
  -e POSTGRES_DB=Pancake \
  -p 5432:5432 \
  -d postgres
```

## Running database

Start Database.

```bash
docker start Pancake-db
```

Stop Database.

```bash
docker stop Pancake-db
```
