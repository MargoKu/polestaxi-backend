# Схемы данных PolesTaxi

Визуал: [databases.drawio](databases.drawio). Каждый сервис владеет своей БД. Прямых JOIN между сервисами нет. Согласованность — события Kafka и saga (реализация позже).

## User Service — MongoDB, БД `polestaxi_users`

Коллекция `users`

| Поле | Тип | Ограничения |
| --- | --- | --- |
| `_id` | ObjectId | PK |
| `name` | string | required, 1–100 |
| `phone` | string | required, unique |
| `email` | string | required, unique, email format |
| `passwordHash` | string | required, bcrypt/argon2 |
| `roles` | string[] | subset of `User`, `Driver`, `Analyst`, `Admin`; при регистрации только `User` или `Driver` |
| `rating` | double | 0–5, среднее по последним 20 поездкам |
| `recentRatings` | double[] | максимум 20, FIFO |
| `isDeleted` | bool | soft-delete, default false |
| `createdAt` | datetime | required |
| `updatedAt` | datetime | required |

Индексы: unique `email`, unique `phone`; `{ isDeleted: 1 }`.

## Driver Service — MongoDB, БД `polestaxi_drivers`

Коллекция `drivers`

| Поле | Тип | Ограничения |
| --- | --- | --- |
| `_id` | ObjectId | PK |
| `userId` | string | required, **unique** (логический FK на User._id) |
| `taxiType` | string | `Economy` \| `Comfort` \| `Business` |
| `licensePlate` | string | required |
| `status` | string | `offline` \| `available` \| `on-trip` |
| `location` | GeoJSON Point | optional, 2dsphere |
| `createdAt` / `updatedAt` | datetime | required |

Имя, email, телефон, пароль **не хранятся** — только в User Service.

Индексы: unique `userId`; `{ status: 1, taxiType: 1 }`; `2dsphere` на `location`.

Переходы статуса: `offline` ↔ `available`; `available` → `on-trip`; `on-trip` → `available` или `offline`. Запрещено: `offline` → `on-trip` (409 Conflict).

## Auth Service — Redis

| Ключ | TTL | Значение |
| --- | --- | --- |
| `access:{jti}` | 15–30 мин | JSON `{ userId, roles[] }` |
| `refresh:{jti}` | 7–30 дней | JSON `{ userId, roles[], family }` |

Logout удаляет оба ключа. Refresh ротирует пару (старые jti удаляются).

## Order Service — Elasticsearch index `orders` + Redis cache

Документ заказа

| Поле | Тип | Ограничения |
| --- | --- | --- |
| `id` | keyword | PK |
| `userId` | keyword | required |
| `driverId` | keyword | nullable до назначения |
| `taxiType` | keyword | Economy / Comfort / Business |
| `status` | keyword | см. жизненный цикл |
| `pickup` / `dropoff` | geo_point + address | required |
| `route` | geo_shape / polyline | optional |
| `distanceMeters` | long | ≥ 0 |
| `durationSeconds` | long | ≥ 0 |
| `price` | scaled_float | ≥ 0 |
| `surge` | float | ≥ 1.0 |
| `userRating` / `driverRating` | byte | 1–5, после completed |
| `comment` | text | optional |
| `createdAt`, `assignedAt`, `startedAt`, `finishedAt` | date | |

Жизненный цикл: `created` → `driver_assigned` → `in_progress` → `completed` \| `cancelled`.

Redis: `order:{id}` TTL для недавних заказов.

## Wallet Service — PostgreSQL, схема `wallet`

```sql
CREATE TABLE wallets (
    id          UUID PRIMARY KEY,
    user_id     TEXT NOT NULL UNIQUE,
    balance     NUMERIC(12, 2) NOT NULL CHECK (balance >= 0),
    currency    CHAR(3) NOT NULL DEFAULT 'RUB',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE transactions (
    id          UUID PRIMARY KEY,
    wallet_id   UUID NOT NULL REFERENCES wallets(id),
    type        TEXT NOT NULL CHECK (type IN ('debit', 'credit', 'refund')),
    amount      NUMERIC(12, 2) NOT NULL CHECK (amount > 0),
    status      TEXT NOT NULL CHECK (status IN ('pending', 'completed', 'failed', 'rolled_back')),
    order_id    TEXT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_transactions_wallet_created ON transactions (wallet_id, created_at DESC);
```

Чувствительные поля — шифрование at rest (фаза реализации). Аудит всех финансовых операций.

## Analytic Service — ClickHouse

Таблицы фактов (MergeTree), заполняются из Kafka:

- `user_registered` — `user_id`, `role`, `event_time`
- `order_completed` — `order_id`, `user_id`, `driver_id`, `taxi_type`, `price`, `distance`, `event_time`
- `trip_rated` — `order_id`, `from_role`, `score`, `event_time`

## Gateway Service

Своей БД нет. Политики RBAC — [RBAC.md](RBAC.md).
