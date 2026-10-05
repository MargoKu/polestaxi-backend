# PolesTaxi — фаза 1

Архитектура, схемы данных и контракты API. Реализация слоёв repository / handler / service — в папках `2-фаза` … `5-фаза`.

Репозиторий **private**. NDA: не публиковать.

## Что здесь

| InnoTaxi | Здесь |
| --- | --- |
| Phase 1 Architecture Foundation | структура монорепо, [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/architecture.drawio](docs/architecture.drawio) |
| Phase 2 Database Design | [docs/databases.drawio](docs/databases.drawio), [docs/DATA.md](docs/DATA.md) |
| Phase 3 API Contracts | [shared/proto](shared/proto), [shared/openapi](shared/openapi), [shared/graphql](shared/graphql) |

Диаграммы: открыть `.drawio` в [diagrams.net](https://app.diagrams.net/) или в VS Code (расширение Draw.io).

## Стек (язык C#, остальное как в InnoTaxi)

| InnoTaxi (Go) | PolesTaxi (C#) |
| --- | --- |
| Gin / Fiber | ASP.NET Core |
| gRPC-Gateway (`google.api.http`) | gRPC JSON transcoding |
| mongo-driver | MongoDB.Driver |
| sqlx | Dapper + Npgsql |
| Redis | StackExchange.Redis |
| golang-jwt | System.IdentityModel.Tokens.Jwt |
| Kafka | Confluent.Kafka |
| Elasticsearch | Elastic.Clients.Elasticsearch |
| ClickHouse | ClickHouse.Client |
| GraphQL | HotChocolate |
| NGINX API gateway | NGINX + YARP (Gateway Service) |
| testify / gomock / dockertest | xUnit / NSubstitute / Testcontainers (со 2-й фазы) |

Фронтенды (Vue 3, React) — отдельным этапом, не в этой фазе.

## Сборка каркаса

```bash
dotnet restore PolesTaxi.slnx
dotnet build PolesTaxi.slnx
```

Каркас **не** поднимает HTTP-серверы и не ходит в БД.

## Следующий шаг

Скопировать эту папку в `2-фаза` и реализовать Data Layer (сначала User + Auth).
