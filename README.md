# Fintech Dev Lab — Платёжный сервис

Надёжный сервис обработки платежных операций через внешнего провайдера.

Главная задача проекта — **гарантировать корректное состояние платежа при повторах запросов, конкурентных `submit`, сетевых ошибках, потерянных HTTP-ответах, ранних callback'ах и перезапусках сервиса**.

Внешний платёжный провайдер представлен отдельным `provider-simulator`.

> **Главный принцип:** успешный HTTP-ответ от провайдера не означает, что платёж завершён. Финальное состояние операции определяется только callback-квитанцией.

---

## Содержание

* [Основные гарантии](#основные-гарантии)
* [Технологический стек](#технологический-стек)
* [Архитектура](#архитектура)
* [Жизненный цикл операции](#жизненный-цикл-операции)
* [Идемпотентность](#идемпотентность)
* [Обработка callback](#обработка-callback)
* [Восстановление после перезапуска](#восстановление-после-перезапуска)
* [API](#api)
* [Запуск](#запуск)
* [Полный сценарий](#полный-сценарий)
* [Тестирование](#тестирование)
* [Структура проекта](#структура-проекта)
* [Надёжность и отказоустойчивость](#надёжность-и-отказоустойчивость)

---

# Основные гарантии

Для каждой операции сервис гарантирует:

* не более **одного платежа у провайдера**;
* одинаковый `Idempotency-Key` для всех повторных попыток;
* сохранение намерения отправки **до внешнего HTTP-вызова**;
* отсутствие блокировки операции на время HTTP-запроса к провайдеру;
* корректную обработку конкурентных `submit`;
* корректную обработку повторных callback;
* корректную обработку callback, пришедшего **до HTTP-ответа провайдера**;
* невозможность вернуть финальную операцию обратно в `PROCESSING`;
* сохранение `providerPaymentId`;
* сохранение истории переходов состояния;
* продолжение обработки незавершённых операций после перезапуска;
* сохранение данных при пересоздании контейнера.

Ключевой инвариант:

> **Одна операция → не более одного платежа провайдера → один финальный результат, определяемый callback.**

---

# Технологический стек

* **C#**
* **.NET 10**
* **ASP.NET Core**
* **Entity Framework Core**
* **PostgreSQL**
* **Npgsql**
* **Docker**
* **Docker Compose**
* **xUnit**
* **FluentAssertions**
* **Swagger / OpenAPI**

Архитектура построена с разделением ответственности между слоями:

```text
API
 ↓
Application
 ↓
Core
 ↑
Infrastructure
```

---

# Архитектура

Сервис состоит из нескольких основных частей.

```text
                    ┌─────────────────────┐
                    │   Client / Tests    │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │    ASP.NET Core     │
                    │      Web API        │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │   Application       │
                    │  OperationService   │
                    └──────────┬──────────┘
                               │
                 ┌─────────────┴─────────────┐
                 ▼                           ▼
        ┌─────────────────┐         ┌─────────────────┐
        │   PostgreSQL    │         │ Background      │
        │                 │         │ Processing      │
        │ Operations      │         │                 │
        │ Attempts        │         │ Provider HTTP   │
        │ Events          │         │ Retry           │
        └─────────────────┘         └────────┬────────┘
                                             │
                                             ▼
                                  ┌─────────────────────┐
                                  │ Provider Simulator  │
                                  └──────────┬──────────┘
                                             │
                                             │ callback
                                             ▼
                                  ┌─────────────────────┐
                                  │ POST /receipts      │
                                  └─────────────────────┘
```

---

# Состояния операции

Операция проходит через четыре основных состояния:

```text
CREATED
   │
   │ submit
   ▼
PROCESSING
   │
   ├───────────────┐
   │               │
   ▼               ▼
COMPLETED       REJECTED
```

### `CREATED`

Операция создана, но отправка провайдеру ещё не запланирована.

### `PROCESSING`

Намерение отправки сохранено в PostgreSQL.

С этого момента сервис обязан продолжить отправку даже после перезапуска.

### `COMPLETED`

Провайдер прислал callback:

```json
{
  "result": "COMPLETED"
}
```

### `REJECTED`

Провайдер прислал callback:

```json
{
  "result": "REJECTED"
}
```

Финальные состояния:

```text
COMPLETED
REJECTED
```

не могут быть изменены обратно в `PROCESSING`.

---

# Идемпотентность

Это центральная часть проекта.

При отправке платежа используется:

```http
Idempotency-Key: operation-123
X-Correlation-ID: operation-123
```

При этом:

```text
Idempotency-Key == operationId
```

и тело запроса при повторных попытках остаётся неизменным.

Например:

```http
POST /payments
Idempotency-Key: operation-123
X-Correlation-ID: operation-123
```

```json
{
  "operationId": "operation-123",
  "amount": "1000.00",
  "currency": "RUB"
}
```

Если HTTP-запрос завершился сетевой ошибкой, сервис **не знает**, успел ли провайдер создать платёж.

Поэтому нельзя просто создать новый платёж.

Вместо этого используется тот же:

```text
Idempotency-Key = operation-123
```

При повторном запросе провайдер возвращает тот же:

```text
providerPaymentId
```

Таким образом:

```text
network failure
      │
      ▼
unknown result
      │
      ▼
retry with same idempotency key
      │
      ▼
same provider payment
```

---

# Почему `PROCESSING` сохраняется до вызова провайдера

`submit` сначала сохраняет намерение:

```text
CREATED
   │
   │ transaction
   ▼
PROCESSING
   │
   │ commit
   ▼
HTTP request to provider
```

Это важно из-за возможного падения процесса:

```text
POST /submit
      │
      ▼
save PROCESSING
      │
      ▼
COMMIT
      │
      ▼
service crashes
```

После перезапуска сервис видит:

```text
PROCESSING
```

и понимает:

> Эта операция должна быть продолжена.

Если бы состояние сохранялось только после HTTP-вызова:

```text
HTTP provider
      │
      ▼
service crashes
      │
      X
save PROCESSING
```

то сервис потерял бы информацию о необходимости отправки.

---

# Конкурентный `submit`

Несколько клиентов могут одновременно выполнить:

```text
POST /operations/123/submit
```

Например:

```text
Request A ─┐
Request B ─┤
Request C ─┼──> operation-123
Request D ─┤
Request E ─┘
```

Только один запрос должен создать намерение отправки.

Остальные запросы видят уже сохранённое состояние:

```text
CREATED
   │
   ├── Request A → creates submission
   │
   ├── Request B → existing submission
   ├── Request C → existing submission
   ├── Request D → existing submission
   └── Request E → existing submission
```

В результате:

```text
5 HTTP requests
       ↓
1 submission intent
       ↓
1 provider payment
```

---

# Callback

Callback является единственным источником истины для финального результата платежа.

Провайдер вызывает:

```http
POST /receipts
```

Пример:

```json
{
  "providerPaymentId": "aa5b7856-e9f2-4fd5-955b-38b1f28d9c57",
  "operationId": "operation-123",
  "result": "COMPLETED",
  "message": "Payment completed",
  "occurredAt": "2026-07-15T12:00:00Z"
}
```

После обработки:

```text
PROCESSING
     │
     │ callback COMPLETED
     ▼
COMPLETED
```

или:

```text
PROCESSING
     │
     │ callback REJECTED
     ▼
REJECTED
```

---

# Ранний callback

Callback может прийти **раньше HTTP-ответа от провайдера**.

Например:

```text
Candidate
   │
   │ POST /payments
   ▼
Provider
   │
   ├────── callback ──────► Candidate
   │
   │
   └──── HTTP 202 ────────► Candidate
```

Поэтому нельзя строить логику так:

```text
HTTP 202
   ↓
providerPaymentId saved
   ↓
only then callback allowed
```

Callback может прийти раньше.

Если валидная квитанция содержит `providerPaymentId`, а локально он ещё не сохранён, сервис устанавливает его из callback.

Поздний HTTP-ответ провайдера не должен откатывать финальное состояние.

---

# Повторный callback

Один и тот же callback может быть доставлен несколько раз:

```text
callback #1 → COMPLETED
callback #2 → COMPLETED
callback #3 → COMPLETED
```

Обработка:

```text
#1 → transition PROCESSING → COMPLETED
#2 → ignored
#3 → ignored
```

Новый переход состояния не создаётся.

---

# Конфликтующий callback

После:

```text
PROCESSING → COMPLETED
```

может прийти:

```json
{
  "result": "REJECTED"
}
```

Такой callback не должен менять состояние:

```text
COMPLETED
   │
   X
REJECTED
```

Сервис фиксирует конфликт и возвращает:

```http
204 No Content
```

Финальный статус остаётся:

```text
COMPLETED
```

---

# История событий

Каждое изменение состояния записывается в историю.

Пример:

```json
[
  {
    "eventId": 1,
    "type": "CREATED",
    "fromStatus": null,
    "toStatus": "CREATED",
    "message": "Operation created",
    "occurredAt": "2026-07-15T12:00:00Z"
  },
  {
    "eventId": 2,
    "type": "SUBMITTED",
    "fromStatus": "CREATED",
    "toStatus": "PROCESSING",
    "message": "Operation submitted",
    "occurredAt": "2026-07-15T12:00:01Z"
  },
  {
    "eventId": 3,
    "type": "COMPLETED",
    "fromStatus": "PROCESSING",
    "toStatus": "COMPLETED",
    "message": "Payment completed",
    "occurredAt": "2026-07-15T12:00:03Z"
  }
]
```

`eventId` монотонно возрастает в рамках операции.

История является частью постоянного состояния и сохраняется в PostgreSQL.

---

# Восстановление после перезапуска

Все данные хранятся в PostgreSQL.

После запуска фоновой обработчик ищет незавершённые операции:

```text
PROCESSING
```

и продолжает их обработку.

Например:

```text
Operation
   │
   ▼
PROCESSING
   │
   ▼
provider accepted payment
   │
   X
service crashed
```

После перезапуска:

```text
PostgreSQL
   │
   │ PROCESSING
   ▼
Background Processor
   │
   ▼
retry with same Idempotency-Key
```

Используется тот же:

```text
operationId
```

поэтому повторная отправка не создаёт новый платёж.

---

# API

## Health Check

```http
GET /health
```

Ответ:

```http
200 OK
```

---

## Создание операции

```http
POST /operations
Content-Type: application/json
```

Запрос:

```json
{
  "operationId": "operation-123",
  "amount": "1000.00",
  "currency": "RUB",
  "description": "Оплата заказа"
}
```

Ответ:

```http
201 Created
```

```json
{
  "operationId": "operation-123",
  "amount": "1000.00",
  "currency": "RUB",
  "description": "Оплата заказа",
  "status": "CREATED",
  "providerPaymentId": null
}
```

Повторное создание:

```http
409 Conflict
```

---

## Отправка операции

```http
POST /operations/{id}/submit
```

Первый запрос:

```http
202 Accepted
```

Повторный запрос для операции, которая уже находится в:

```text
PROCESSING
COMPLETED
REJECTED
```

возвращает:

```http
200 OK
```

и не создаёт новое намерение.

---

## Получение операции

```http
GET /operations/{id}
```

Ответ:

```json
{
  "operationId": "operation-123",
  "amount": "1000.00",
  "currency": "RUB",
  "status": "COMPLETED",
  "providerPaymentId": "aa5b7856-e9f2-4fd5-955b-38b1f28d9c57"
}
```

---

## Получение истории

```http
GET /operations/{id}/events
```

Возвращает массив событий в порядке их фиксации.

---

## Callback

```http
POST /receipts
Content-Type: application/json
```

```json
{
  "providerPaymentId": "aa5b7856-e9f2-4fd5-955b-38b1f28d9c57",
  "operationId": "operation-123",
  "result": "COMPLETED",
  "message": "Payment completed",
  "occurredAt": "2026-07-15T12:00:00Z"
}
```

Успешная обработка:

```http
204 No Content
```

---

# Запуск

## Требования

Необходимы:

* Docker
* Docker Compose

Проверить:

```bash
docker --version
docker compose version
```

---

## Запуск всего проекта

Из корня репозитория:

```bash
docker compose up --build
```

После запуска:

```text
Candidate Service:
http://localhost:8080

Provider Simulator:
http://localhost:8081
```

Swagger:

```text
http://localhost:8080/swagger
```

---

# Конфигурация

URL внешнего провайдера задаётся через:

```text
PROVIDER_URL
```

В Docker Compose:

```yaml
environment:
  PROVIDER_URL: http://provider-simulator:8081
```

Для локального запуска:

```text
PROVIDER_URL=http://localhost:8081
```

PostgreSQL использует постоянное хранилище.

---

# Полный сценарий

## 1. Создать операцию

```bash
curl -X POST http://localhost:8080/operations \
  -H "Content-Type: application/json" \
  -d '{
    "operationId": "operation-123",
    "amount": "1000.00",
    "currency": "RUB",
    "description": "Оплата заказа"
  }'
```

Ожидаемый результат:

```text
201 Created
```

Состояние:

```text
CREATED
```

---

## 2. Запланировать отправку

```bash
curl -X POST \
  http://localhost:8080/operations/operation-123/submit
```

Ожидаемый результат:

```text
202 Accepted
```

Состояние:

```text
PROCESSING
```

---

## 3. Дождаться callback

Провайдер самостоятельно отправит:

```text
POST /receipts
```

После callback операция перейдёт в:

```text
COMPLETED
```

или:

```text
REJECTED
```

---

## 4. Проверить состояние

```bash
curl \
  http://localhost:8080/operations/operation-123
```

---

## 5. Проверить историю

```bash
curl \
  http://localhost:8080/operations/operation-123/events
```

---

# Тестирование

Для запуска тестов:

```bash
dotnet test
```

Интеграционные тесты:

```bash
dotnet test \
  src/Tests/IntegrationTests/IntegrationTests.csproj
```

Тесты проверяют в том числе:

* создание операции;
* валидацию суммы;
* валидацию валюты;
* повторное создание;
* конкурентное создание;
* конкурентный `submit`;
* идемпотентность;
* переход `CREATED → PROCESSING`;
* callback;
* повторный callback;
* конфликтующий callback;
* получение истории;
* работу после ошибок провайдера;
* восстановление после перезапуска.

---

# CI

Проект использует GitHub Actions.

CI выполняет:

```text
Checkout
   ↓
Setup .NET
   ↓
Restore
   ↓
Build
   ↓
Start PostgreSQL
   ↓
Apply EF Core migrations
   ↓
Integration Tests
```

Каждый push и pull request автоматически проверяет сборку и интеграционные тесты.

---

# Структура проекта

```text
fintech-dev-lab/
│
├── src/
│   │
│   ├── Core/
│   │   ├── Models/
│   │   ├── Enums/
│   │   └── Core.csproj
│   │
│   ├── Application/
│   │   ├── Features/
│   │   │   └── Operations/
│   │   ├── Services/
│   │   └── Application.csproj
│   │
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Migrations/
│   │   │   ├── AppDbContext.cs
│   │   │   └── UnitOfWork.cs
│   │   ├── Providers/
│   │   └── Infrastructure.csproj
│   │
│   └── Tests/
│       └── IntegrationTests/
│           ├── Operations/
│           └── IntegrationTests.csproj
│
├── compose.yaml
├── Dockerfile
├── fintech-dev-lab.slnx
└── README.md
```

---

# Постоянное хранение

PostgreSQL используется как источник постоянного состояния.

Основные данные:

```text
Operations
PaymentAttempts
OperationEvents
```

Хранилище переживает пересоздание контейнера благодаря Docker volume.

Принцип:

```text
Container restart
       │
       ▼
PostgreSQL volume
       │
       ▼
previous state restored
```

---

# Транзакционность

Изменение состояния операции и связанной истории выполняется атомарно.

Например:

```text
PROCESSING
    │
    ├── set providerPaymentId
    ├── change status
    └── create event
           │
           ▼
        COMMIT
```

Если транзакция завершается ошибкой:

```text
ROLLBACK
```

и частичное состояние не сохраняется.

---

# Retry и сетевые ошибки

При временных ошибках провайдера используется ограниченный retry с backoff и jitter.

Принцип:

```text
attempt 1
   │
   X
   │
   ▼
backoff
   │
   ▼
attempt 2
   │
   X
   │
   ▼
backoff
   │
   ▼
attempt 3
```

При этом каждая попытка использует тот же:

```text
Idempotency-Key
```

Это критически важно.

Retry без идемпотентности мог бы привести к:

```text
operation-123
    │
    ├── payment #1
    ├── payment #2
    └── payment #3
```

Вместо этого:

```text
operation-123
    │
    ├── attempt #1
    ├── attempt #2
    └── attempt #3
             │
             ▼
       same provider payment
```

---

# Обработка неизвестного результата

Одна из ключевых проблем системы:

```text
Candidate ── request ──► Provider
                            │
                            ▼
                       payment created
                            │
                            X
                      network failure
```

Клиент получает:

```text
connection timeout
```

Но это **не означает**, что платежа нет.

Поэтому сервис не переводит операцию в:

```text
REJECTED
```

и не создаёт новый платёж.

Вместо этого:

```text
PROCESSING
    │
    ▼
retry with same idempotency key
```

---

# Почему HTTP 202 не означает `COMPLETED`

Провайдер может ответить:

```http
202 Accepted
```

с:

```json
{
  "providerPaymentId": "...",
  "status": "ACCEPTED"
}
```

Это означает только:

> Провайдер принял запрос на обработку.

Это **не означает**:

```text
COMPLETED
```

Финальный результат приходит отдельно:

```text
Provider
   │
   │ callback
   ▼
Candidate Service
```

И только callback переводит операцию в:

```text
COMPLETED
```

или:

```text
REJECTED
```

---

# Принцип работы сервиса

Вся система в упрощённом виде:

```text
                 POST /operations
                        │
                        ▼
                  ┌───────────┐
                  │  CREATED  │
                  └─────┬─────┘
                        │
                   POST /submit
                        │
                        ▼
                 ┌─────────────┐
                 │ PROCESSING  │
                 └──────┬──────┘
                        │
                 persist intent
                        │
                        ▼
                Background Worker
                        │
                        ▼
                 Provider HTTP
                        │
              ┌─────────┴─────────┐
              │                   │
           success             network error
              │                   │
              │                   └──── retry ────┐
              │                                   │
              ▼                                   │
       providerPaymentId                          │
              │                                   │
              └──────────────────┬────────────────┘
                                 │
                                 ▼
                            callback
                                 │
                       ┌─────────┴─────────┐
                       │                   │
                       ▼                   ▼
                  COMPLETED            REJECTED
```

---

# Что проверяется

Автоматические проверки моделируют реальные проблемные сценарии:

1. обычный платёж до `COMPLETED`;
2. платёж с результатом `REJECTED`;
3. множество конкурентных `submit`;
4. повторные `submit`;
5. сетевой сбой;
6. потерю HTTP-ответа после фактического создания платежа;
7. callback до HTTP-ответа;
8. повторный callback;
9. конфликтующий callback;
10. остановку сервиса во время обработки;
11. восстановление после запуска;
12. сохранность истории;
13. отсутствие дублирования платежей у провайдера.

---

# Ключевой инвариант проекта

В конечном счёте вся архитектура строится вокруг одного требования:

```text
             ONE OPERATION
                   │
                   ▼
          ONE PROVIDER PAYMENT
                   │
                   ▼
          ONE FINAL RESULT
```

Даже если происходят:

```text
retries
concurrent requests
network failures
lost responses
early callbacks
duplicate callbacks
process crashes
container restarts
```

сервис должен сохранить корректное состояние.

---

# Лицензия

Проект создан в рамках вступительного задания **Fintech Dev Lab**.
