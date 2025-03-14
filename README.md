<h1 align="center">Market Monitor</h1>

<h4 align="center">An easy-to-use bot that notifies you of undercutting in the markets of Eorzea<br/>and tracks your sale and purchase history.</h4>

<p align="center">
  <a href="https://discord.gg/66dp9gxMZx">
    <img src="https://discordapp.com/api/guilds/918704583717572639/widget.png?style=shield" alt="Discord Server">
  </a>
  <a href="https://github.com/Azorant/MarketMonitor/actions">
    <img src="https://img.shields.io/github/actions/workflow/status/Azorant/MarketMonitor/docker-publish.yml?label=Build" alt="GitHub Actions">
  </a>
</p>

# Getting Started
Install [Docker](https://docs.docker.com/engine/install/) and [Docker Compose](https://docs.docker.com/compose/install/)

Create a `docker-compose.yml` with the following content:

```yaml
version: '3.8'

services:
  marketmonitor:
    image: ghcr.io/azorant/marketmonitor:latest
    container_name: marketmonitor
    restart: unless-stopped
    environment:
      - DISCORD_INVITE=server invite
      - REDIS=redis
      - LOG_CHANNEL=channel for logging status updates
      - GUILD_CHANNEL=channel for logging guild join/leave
      - TOKEN=bot token
      - DB=server=maria;user=root;password=Market;database=marketmonitor
    ports:
      - 3400:3400
    depends_on:
      maria:
        condition: service_started
      redis:
        condition: service_started
  maria:
    image: mariadb:latest
    container_name: maria
    restart: unless-stopped
    environment:
      - MARIADB_ROOT_PASSWORD=Market
    expose:
      - 3306
  redis:
    image: redis:alpine
    container_name: redis
    restart: unless-stopped
    expose:
      - 6379
```

Run `docker compose up -d` to startup MarketMonitor, MariaDB and Redis.

That's all!