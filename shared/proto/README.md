# Proto contracts

Канонический контракт HTTP + gRPC + Kafka. HTTP объявляется аннотациями `google.api.http`. Генерация C# и JSON transcoding — в следующих фазах.

Корень импортов: эта папка (`-I shared/proto`).

| Файл | Назначение |
| --- | --- |
| [user/user.proto](user/user.proto) | регистрация, профиль, VerifyCredentials |
| [auth/auth.proto](auth/auth.proto) | login, refresh, logout, ValidateToken |
| [driver/driver.proto](driver/driver.proto) | driver-only профиль, статус, trip RPC |
| [order/order.proto](order/order.proto) | заказы; GraphQL — [../graphql/order.graphql](../graphql/order.graphql) |
| [wallet/wallet.proto](wallet/wallet.proto) | кошелёк, debit/credit |
| [analytic/analytic.proto](analytic/analytic.proto) | статистика (Analyst) |
| [events/events.proto](events/events.proto) | Kafka payloads |

`google/api/annotations.proto` — зависимость googleapis / пакет `Google.Api.CommonProtos`.
