# PolesTaxi — фаза 2 (Data Layer)

Копия фазы 1 + repository, подключения, миграции и интеграционные тесты. **Нет** HTTP API и выдачи JWT.

## Хранилища

```bash
docker compose -f deploy/docker-compose.yml up -d
```

Строки подключения: [`.env.example`](.env.example).

| Сервис | БД |
| --- | --- |
| User | MongoDB `polestaxi_users` |
| Auth | Redis (ключи `access:{jti}`, `refresh:{jti}`) |
| Driver | MongoDB `polestaxi_drivers` |
| Order | Elasticsearch `orders` + Redis cache |
| Wallet | PostgreSQL + Redis cache |
| Analytic | ClickHouse |
| Gateway | нет своей БД |

## Сборка и тесты

```bash
dotnet restore PolesTaxi.slnx
dotnet build PolesTaxi.slnx
dotnet test PolesTaxi.slnx
```

Тесты поднимают Testcontainers (нужен Docker). Compose нужен для ручного `dotnet run`.

## Запуск каркаса (подключение к БД, без HTTP)

```bash
# из корня 2-фаза, с переменными из .env.example
export MONGO_CONNECTION=mongodb://127.0.0.1:27017
dotnet run --project services/user-service/PolesTaxi.UserService.csproj
```

Сервис применяет индексы/миграции, пишет `ready` и ждёт Ctrl+C. Порт 8080 не слушает.

`SEED=true` у User Service создаёт админа `admin@polestaxi.local` / телефон `+70000000000`.

## Следующий шаг

Скопировать `2-фаза` → `3-фаза` и реализовать handlers (HTTP / gRPC / Kafka).
