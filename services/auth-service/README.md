# Auth Service

Хранилище: **Redis**. Data layer хранит payload по `access:{jti}` и `refresh:{jti}` с TTL. Выдача JWT — следующая фаза.
