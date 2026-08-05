# Pancake API

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
