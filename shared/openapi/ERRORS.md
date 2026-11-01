# Error catalog

Единый JSON для HTTP:

```json
{ "code": "VALIDATION", "message": "email is required" }
```

| code | HTTP | Когда |
| --- | --- | --- |
| `VALIDATION` | 400 | неверные поля, роль регистрации не User/Driver, score не 1–5 |
| `UNAUTHORIZED` | 401 | нет/битый/истёкший/отозванный токен, неверный пароль |
| `FORBIDDEN` | 403 | роль не проходит RBAC Gateway |
| `NOT_FOUND` | 404 | пользователь, заказ, кошелёк |
| `CONFLICT` | 409 | unique email/phone, duplicate driver userId, illegal status, insufficient funds |
| `INTERNAL` | 500 | непредвиденная ошибка |

gRPC mapping: InvalidArgument, Unauthenticated, PermissionDenied, NotFound, AlreadyExists/FailedPrecondition, Internal.
