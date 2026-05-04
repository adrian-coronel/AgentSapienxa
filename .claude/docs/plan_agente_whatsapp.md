# Plan — Agente WhatsApp AgentSapienxa

> Migración de los 6 workflows n8n a una API .NET 8 con Clean Architecture, diseñada como producto reusable.
>
> Convenciones del documento: 🧑 = paso manual (humano) · 🤖 = paso Claude Code.
>
> Mientras `Features:PaymentsEnabled = false`: todo el código de pagos se implementa pero los endpoints no se exponen, los tools no se registran y el router siempre va al agente general.

---

## 0. Principios de diseño rectores

Estos principios condicionan cada decisión del plan. Si una decisión choca con uno de ellos, hay que repensarla.

1. **Producto, no proyecto.** Núcleo genérico (motor del agente, leads, enrollments, webhook, memoria) separado de la capa de personalización por cliente (prompts, marca, tono, modelo, temperatura). Lo que varía por cliente vive en BD, no en código.
2. **Catálogo genérico en dominio.** Las capas de Domain y Application **no conocen "cursos"**. Manejan `CatalogItem`. La capa de presentación (controlador `CoursesController`) y la configuración del agente lo llaman "curso" para este cliente.
3. **`AgentConfig` es entidad de primera clase.** No es una tabla de configuración aislada; es la unidad base del futuro hub multi-agente. Diseñarla bien ahora ahorra una refactorización completa después.
4. **Pagos: implementación completa, exposición cero.** Feature flag `Features:PaymentsEnabled = false`. La calidad del código de pagos debe ser igual a la del resto, pero ningún cliente puede llegar a él hasta que se active.
5. **WhatsApp Business API de Meta, no Evolution API.** El webhook tiene `GET` para verificación (`hub.challenge`) y `POST` para eventos (`entry[].changes[].value`). Toda la capa de mensajería se aísla detrás de `IMessagingChannel` para que mañana se pueda swap a Telegram/Instagram/etc.
6. **Validación de pago asíncrona.** El endpoint de validación responde inmediatamente y persiste un estado pendiente; un endpoint distinto recibe la decisión humana. Nada de bloqueo síncrono tipo Gmail sendAndWait.

---

## 1. Estructura de carpetas (Clean Architecture)

Asumo que la solución ya existe (`AgentSapienxa.API` + `Application` + `Domain` + `Infrastructure` en `~/servicios/edusapienxa`). Los archivos a crear / mover van marcados con `+`. Nombres genéricos en dominio y application; nombres específicos del cliente solo en API/configuración.

```
src/
├── AgentSapienxa.Domain/
│   ├── Common/
│   │   ├── Entity.cs                                +
│   │   ├── ValueObject.cs                           +
│   │   └── Result.cs                                +
│   ├── ValueObjects/
│   │   └── PhoneNumber.cs                           +
│   ├── Catalog/
│   │   ├── CatalogItem.cs                           +   ← mapea a tabla `courses`
│   │   └── Instructor.cs                            +
│   ├── Leads/
│   │   ├── Lead.cs                                  +
│   │   ├── LeadStatus.cs                            +
│   │   └── SalesAgent.cs                            +
│   ├── Enrollments/
│   │   ├── Enrollment.cs                            +   ← mapea a `leads_enrollments`
│   │   └── EnrollmentStatus.cs                      +   ← state machine
│   ├── Payments/
│   │   ├── PaymentMethod.cs                         +
│   │   └── PaymentValidation.cs                     +   ← entidad nueva (async)
│   ├── Agents/
│   │   ├── AgentConfig.cs                           +
│   │   └── AgentKey.cs                              +   ← strongly-typed key ('cursos_general', 'cursos_pagos')
│   └── Conversations/
│       ├── ConversationMessage.cs                   +
│       └── ConversationRole.cs                      +
│
├── AgentSapienxa.Application/
│   ├── Common/
│   │   ├── Abstractions/
│   │   │   ├── IFeatureFlags.cs                     +
│   │   │   ├── IMessagingChannel.cs                 +   ← capa que hoy es WhatsApp, mañana lo que sea
│   │   │   ├── IMediaTranscriber.cs                 +
│   │   │   ├── IImageDescriber.cs                   +
│   │   │   ├── ILlmProvider.cs                      +
│   │   │   └── IClock.cs                            +
│   │   └── Errors/
│   │       └── DomainError.cs                       +
│   ├── Catalog/
│   │   ├── Queries/
│   │   │   ├── GetCatalog/                          +
│   │   │   └── GetInstructor/                       +
│   │   └── Repositories/
│   │       ├── ICatalogRepository.cs                +
│   │       └── IInstructorRepository.cs             +
│   ├── Leads/
│   │   ├── Commands/CaptureLead/                    +
│   │   └── Repositories/
│   │       ├── ILeadRepository.cs                   +
│   │       └── ISalesAgentRepository.cs             +
│   ├── Enrollments/
│   │   ├── Commands/
│   │   │   ├── CreateEnrollment/                    +
│   │   │   ├── Checkout/                            +   (gated)
│   │   │   └── EscalateToHuman/                     +   (gated)
│   │   └── Repositories/IEnrollmentRepository.cs    +
│   ├── Payments/                                    +   (gated en routing/exposición, no en compilación)
│   │   ├── Commands/
│   │   │   ├── RequestPaymentValidation/            +
│   │   │   └── ResolvePaymentValidation/            +
│   │   ├── Queries/GetPaymentMethods/               +
│   │   └── Repositories/
│   │       ├── IPaymentMethodRepository.cs          +
│   │       └── IPaymentValidationRepository.cs      +
│   ├── Conversations/
│   │   ├── Commands/ProcessIncomingMessage/         +   ← orquestador principal
│   │   ├── Repositories/IConversationRepository.cs  +
│   │   └── Services/
│   │       ├── IIntentClassifier.cs                 +
│   │       └── IAgentRouter.cs                      +
│   └── Agents/
│       ├── IAgent.cs                                +
│       ├── IAgentTool.cs                            +
│       ├── AgentRegistry.cs                         +   ← descubre IAgent registrados en DI
│       ├── GeneralAgent.cs                          +
│       ├── PaymentAgent.cs                          +   (gated)
│       ├── Tools/
│       │   ├── CaptureLeadTool.cs                   +
│       │   ├── EnrollmentTool.cs                    +
│       │   ├── GetCatalogItemTool.cs                +
│       │   ├── GetInstructorTool.cs                 +
│       │   ├── GetPaymentMethodsTool.cs             +   (gated)
│       │   ├── GenerateCheckoutTool.cs              +   (gated)
│       │   ├── EscalateToHumanTool.cs               +   (gated)
│       │   └── PaymentValidationTool.cs             +   (gated)
│       └── Repositories/IAgentConfigRepository.cs   +
│
├── AgentSapienxa.Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs                  +/M
│   │   ├── Conversions/MoneyConverter.cs            +   ← MONEY ↔ decimal
│   │   ├── Configurations/                          +
│   │   │   ├── CatalogItemConfiguration.cs              ← .ToTable("courses")
│   │   │   ├── InstructorConfiguration.cs
│   │   │   ├── LeadConfiguration.cs
│   │   │   ├── EnrollmentConfiguration.cs               ← .ToTable("leads_enrollments")
│   │   │   ├── PaymentMethodConfiguration.cs
│   │   │   ├── SalesAgentConfiguration.cs
│   │   │   ├── ConversationMessageConfiguration.cs
│   │   │   ├── AgentConfigConfiguration.cs
│   │   │   └── PaymentValidationConfiguration.cs
│   │   ├── Repositories/                            +
│   │   └── Migrations/                              +
│   ├── Llm/
│   │   ├── OpenAi/
│   │   │   ├── OpenAiLlmProvider.cs                 +
│   │   │   ├── OpenAiOptions.cs                     +
│   │   │   ├── OpenAiToolSchemaBuilder.cs           +
│   │   │   └── WhisperTranscriber.cs                +
│   │   └── Vision/OpenAiImageDescriber.cs           +
│   ├── Messaging/
│   │   ├── WhatsApp/
│   │   │   ├── MetaWhatsAppOptions.cs               +
│   │   │   ├── MetaWhatsAppClient.cs                +   ← envío + descarga de media
│   │   │   ├── MetaWebhookSignatureValidator.cs     +
│   │   │   ├── MetaWebhookPayload.cs                +   ← DTOs del payload Meta
│   │   │   └── MetaIncomingMessageMapper.cs         +
│   │   └── WhatsAppMessagingChannel.cs              +   ← implementa IMessagingChannel
│   ├── FeatureFlags/ConfigurationFeatureFlags.cs    +
│   ├── Time/SystemClock.cs                          +
│   └── DependencyInjection.cs                       +/M
│
└── AgentSapienxa.API/
    ├── Controllers/
    │   ├── LeadsController.cs                       +
    │   ├── EnrollmentsController.cs                 +   (action Escalate gated)
    │   ├── CoursesController.cs                     +   ← presentación: alias de catalog para este cliente
    │   ├── InstructorsController.cs                 +
    │   ├── CheckoutController.cs                    +   (gated)
    │   ├── PaymentMethodsController.cs              +   (gated)
    │   ├── PaymentsController.cs                    +   (gated; validate + decision)
    │   └── Webhooks/WhatsAppWebhookController.cs    +
    ├── Filters/
    │   ├── PaymentsFeatureSwaggerFilter.cs          +   ← oculta gated en Swagger
    │   └── PaymentsFeatureGate.cs                   +   ← devuelve 404 si flag = false
    ├── Middleware/MetaWebhookSignatureMiddleware.cs +
    ├── Configuration/
    │   ├── FeaturesOptions.cs                       +
    │   └── DependencyInjection.cs                   +
    ├── Program.cs                                   M
    ├── appsettings.json                             M
    └── appsettings.Development.json                 M

tests/
├── AgentSapienxa.UnitTests/                           +
│   ├── Leads/CaptureLeadHandlerTests.cs
│   ├── Enrollments/CreateEnrollmentHandlerTests.cs
│   ├── Enrollments/CheckoutHandlerTests.cs
│   ├── Conversations/ProcessIncomingMessageHandlerTests.cs
│   └── Agents/AgentRouterTests.cs                       ← prueba que con flag=false siempre va a GeneralAgent
└── AgentSapienxa.IntegrationTests/                    +
    ├── Webhooks/WhatsAppWebhookTests.cs
    ├── Payments/PaymentsFeatureFlagTests.cs             ← 404 + Swagger oculto
    └── Persistence/EnrollmentRepositoryTests.cs
```

`+` archivo nuevo · `M` archivo a modificar · `+/M` puede ser nuevo o existente

---

## 2. Entidades de dominio

### 2.1 Mapeo de tablas existentes a entidades genéricas

| Tabla PostgreSQL | Entidad de dominio | Notas |
|---|---|---|
| `courses` | `CatalogItem` | El nombre de tabla queda fijo (no renombrar BD); `.ToTable("courses")` en EF |
| `instructors` | `Instructor` | FK desde `CatalogItem.InstructorId` |
| `payment_methods` | `PaymentMethod` | `LimitAmount` mapea de `MONEY` a `decimal` con converter |
| `leads` | `Lead` | |
| `leads_enrollments` | `Enrollment` | `TotalCost` de `MONEY` a `decimal` |
| `sales_agents` | `SalesAgent` | |

### 2.2 Tablas / columnas nuevas a crear (migración EF)

1. **`leads_enrollments.sale_agent`** — `UUID NULL REFERENCES sales_agents(id)`. Falta en el DDL real, la usa el flujo de escalamiento.
2. **`conversation_history`** — memoria conversacional propia (hoy en tabla interna n8n):
   ```sql
   id            UUID PK
   session_id    TEXT NOT NULL          -- = phone_number del usuario
   role          TEXT NOT NULL          -- 'user' | 'assistant' | 'system' | 'tool'
   content       TEXT NOT NULL
   tool_calls    JSONB NULL             -- para mensajes assistant con tool_calls
   tokens_in     INT NULL
   tokens_out    INT NULL
   model         TEXT NULL
   created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
   INDEX idx_conversation_history_session_created (session_id, created_at)
   ```
3. **`agent_config`** — corazón del producto multi-agente:
   ```sql
   id             UUID PK
   agent_key      TEXT UNIQUE NOT NULL   -- 'cursos_general', 'cursos_pagos', 'cursos_intent'
   name           TEXT NOT NULL
   description    TEXT NULL
   system_prompt  TEXT NOT NULL
   model          TEXT NOT NULL          -- 'gpt-4.1', 'gpt-4.1-mini'
   temperature    NUMERIC(3,2) NOT NULL DEFAULT 0.2
   max_tokens     INT NULL
   memory_window  INT NOT NULL DEFAULT 10
   is_active      BOOLEAN NOT NULL DEFAULT true
   created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
   updated_at     TIMESTAMPTZ NOT NULL DEFAULT now()
   ```
   Sembrar tres filas: `cursos_intent` (Analizer), `cursos_general` (DatapathBot), `cursos_pagos` (PaymentBot) con los prompts actuales de los workflows.
4. **`payment_validations`** — estado de validación asíncrona:
   ```sql
   id              UUID PK
   enrollment_id   UUID NOT NULL REFERENCES leads_enrollments(id)
   voucher_detail  TEXT NULL
   voucher_url     TEXT NULL
   status          TEXT NOT NULL          -- 'Pendiente' | 'Aprobado' | 'Inválido' | 'Expirado'
   observation     TEXT NULL
   requested_by    TEXT NULL              -- session_id / phone
   resolved_by     TEXT NULL              -- email del agente humano
   requested_at    TIMESTAMPTZ NOT NULL DEFAULT now()
   resolved_at     TIMESTAMPTZ NULL
   ```

### 2.3 Lista completa de entidades de dominio

| Entidad | Tabla | Comentarios |
|---|---|---|
| `CatalogItem` | `courses` | Genérica; nombrada "curso" solo en API/config |
| `Instructor` | `instructors` | |
| `PaymentMethod` | `payment_methods` | `LimitAmount: decimal` |
| `Lead` | `leads` | `PhoneNumber` value object |
| `SalesAgent` | `sales_agents` | |
| `Enrollment` | `leads_enrollments` | Estado fuerte (`EnrollmentStatus`), transiciones validadas |
| `ConversationMessage` | `conversation_history` | Inmutable; append-only |
| `AgentConfig` | `agent_config` | Pilar del producto multi-agente |
| `PaymentValidation` | `payment_validations` | Tracking de validación humana asíncrona |

### 2.4 Value objects y enums

- `PhoneNumber` (validación de formato, normalización a E.164).
- `EnrollmentStatus`: `Interesado`, `PendientePago`, `Pagado`, `EscaladoAHumano`, `Inactivo`. Métodos `CanTransitionTo(...)`.
- `LeadStatus`: `New`, `Active`, `Closed` (extensible).
- `ConversationRole`: `User`, `Assistant`, `System`, `Tool`.
- `AgentKey`: wrapper sobre string para evitar magic strings al buscar configs.

---

## 3. Endpoints

> **Convención de exposición:**
> - Endpoints **activos** se exponen siempre.
> - Endpoints **gated** existen siempre en código pero `[ServiceFilter(typeof(PaymentsFeatureGate))]` devuelve 404 si `PaymentsEnabled = false`, y `PaymentsFeatureSwaggerFilter` los oculta de Swagger.
> - Todos los endpoints de negocio se autenticarán a futuro; en esta fase quedan abiertos detrás de un middleware de API key sencillo (suficiente para llamadas server-to-server desde el orquestador).

### 3.1 Activos desde el inicio

#### `POST /api/leads/capture`
Equivalente a `CaptureLeads`. Upsert por `phoneNumber`; si es nuevo, asigna primer `SalesAgent` disponible.

**Request**
```json
{
  "phoneNumber": "+51987654321",
  "name": "Juan Pérez",
  "email": "juan@example.com",
  "contactMethod": "WhatsApp"
}
```
**Response 200**
```json
{ "status": "ok", "leadId": "uuid", "isNew": true, "message": "Lead capturado" }
```
**Errores:** `400` validación (phone inválido), `409` colisión inesperada.

#### `POST /api/enrollments`
Equivalente a `LeadEnrollment`. Si el lead no existe → `409` indicando que primero use `capture_lead`. Si ya hay enrollment activo para `(lead, catalogItem)` → idempotente.

**Request**
```json
{ "phoneNumber": "+51987654321", "catalogItemId": "uuid" }
```
**Response 200**
```json
{ "status": "ok", "enrollmentId": "uuid", "alreadyEnrolled": false }
```

#### `GET /api/courses`
Lista de `CatalogItem` → DTO `CourseDto`. Soporta `?onlyAvailable=true`.

**Response 200**
```json
[{
  "id": "uuid", "code": "DATAENG", "title": "Data Engineering",
  "shortDescription": "...", "cost": 1500.00, "availablePlaces": "12",
  "startDate": "2026-05-15", "instructorId": "uuid", "link": "https://datapath.ai/..."
}]
```

#### `GET /api/courses/{id}`
Detalle, útil como tool del agente.

#### `GET /api/instructors/{id}`
**Response 200**
```json
{
  "id": "uuid", "name": "Ana Torres", "email": "...", "phoneNumber": "...",
  "profilePicture": "url", "expertise": "AI,ML", "summary": "..."
}
```

### 3.2 Gated (implementados completos, ocultos cuando `PaymentsEnabled = false`)

#### `GET /api/payment-methods`
**Response 200**: array `{ id, name, description, image, limitAmount }`.

#### `POST /api/checkout`
Equivalente a `Checkout`. Valida enrollment activo, valida `cost <= limitAmount`, mueve enrollment a `PendientePago`.

**Request**
```json
{ "phoneNumber": "+51...", "catalogItemId": "uuid", "paymentMethodName": "Yape" }
```
**Response 200**
```json
{
  "status": "ok",
  "courseTitle": "Data Engineering",
  "amount": 1500.00,
  "paymentMethod": { "name": "Yape", "description": "...", "image": "url" }
}
```
**Errores:** `404` lead/enrollment no encontrado, `409` enrollment en estado inválido, `422` monto excede límite del método.

#### `POST /api/payments/validate`
Equivalente al **lado de solicitud** del `Payment Validation Agent`. **Asíncrono.** Crea `PaymentValidation` con estado `Pendiente`, persiste el voucher, dispara notificación al humano (en esta fase: solo log + insert), responde de inmediato.

**Request**
```json
{
  "sessionId": "+51...",
  "phoneNumber": "+51...",
  "enrollmentId": "uuid",
  "voucherDetail": "Transferencia BCP 1234, S/ 1500, hoy 10:23",
  "voucherUrl": "https://..."
}
```
**Response 202 Accepted**
```json
{ "status": "in_progress", "validationId": "uuid", "message": "Validación enviada al equipo de ventas" }
```

#### `POST /api/payments/validate/decision`
Lado de **resolución humana**. Reemplaza al Gmail sendAndWait de n8n. Reservado a un grupo (auth posterior — por ahora API key compartida, distinta a la del orquestador).

**Request**
```json
{
  "validationId": "uuid",
  "decision": "Aprobado",
  "observation": "Comprobante OK"
}
```
**Response 200**
```json
{ "status": "ok", "enrollmentStatus": "Pagado" }
```
Reglas: si `Aprobado` → `Enrollment.Status = Pagado`. Si `Inválido` → no cambia status, registra observación, dispara mensaje al usuario por el canal (vía `IMessagingChannel`). `404` si validación no existe, `409` si ya estaba resuelta.

#### `POST /api/enrollments/escalate`
Equivalente a `Escale To Human - Enrollment`.

**Request**
```json
{ "phoneNumber": "+51...", "enrollmentId": "uuid", "observation": "Cliente pide hablar con asesor" }
```
**Response 200**
```json
{ "status": "ok", "salesAgentEmail": "vendedor@datapath.ai" }
```
Si el lead no tiene `salesAgent`: asigna el primero disponible y lo persiste tanto en `lead.sales_agent` como en `enrollment.sale_agent` (columna nueva).

### 3.3 Webhook WhatsApp (Phase 2)

#### `GET /api/webhooks/whatsapp`
Verificación de Meta. Lee `?hub.mode`, `?hub.verify_token`, `?hub.challenge`. Si `hub.mode = subscribe` y `hub.verify_token == config:Meta:VerifyToken`, responde con `hub.challenge` en plain text. Si no, `403`.

#### `POST /api/webhooks/whatsapp`
Recibe eventos. Middleware `MetaWebhookSignatureMiddleware` valida `X-Hub-Signature-256` (HMAC-SHA256 del body con app secret). Mapper extrae:
- `entry[].changes[].value.messages[]` con `from`, `type`, `text.body | audio.id | image.id`, `timestamp`
- Descarta `statuses[]` (read receipts, etc.) — solo log.

**Comportamiento:** responde `200 OK` siempre que la firma sea válida (Meta reintenta si no), encola el procesamiento al handler `ProcessIncomingMessage`. Esta fase puede ser síncrona dentro del request si el SLA aguanta; si no, `IBackgroundTaskQueue` (`Channel<T>` en memoria) — preferible empezar síncrono y refactorizar si hace falta.

### 3.4 Endpoints internos / no expuestos

- Memoria conversacional, intent classifier y agentes son servicios internos. No exponen HTTP.
- `AgentConfig` no se expone aún (se administra por SQL en esta fase). Cuando llegue el hub multi-agente, se agregará un controlador admin protegido.

---

## 4. Mecánica del feature flag de pagos

Cuatro puntos de aplicación del flag, para que la desactivación sea hermética:

1. **Routing del agente** — `IAgentRouter.ResolveAgent(intent)`:
   ```
   if (!features.PaymentsEnabled) return GeneralAgent;
   return intent == Intent.Pagar ? PaymentAgent : GeneralAgent;
   ```
2. **Registro de tools** — `AgentRegistry` solo registra los tools de pago en `GeneralAgent`/`PaymentAgent` cuando `PaymentsEnabled = true`. Con flag = false, los tools no aparecen en el schema enviado al LLM.
3. **Visibilidad en Swagger** — `PaymentsFeatureSwaggerFilter : IDocumentFilter` quita los paths de los controladores marcados con `[PaymentsFeature]` cuando flag = false.
4. **Gate runtime** — `PaymentsFeatureGate : IAsyncActionFilter` devuelve `NotFound()` si el endpoint pertenece a un controlador `[PaymentsFeature]` y flag = false. Cubre el caso de que alguien conozca la URL de memoria.

> Activar pagos = cambiar `Features:PaymentsEnabled` a `true` en `appsettings.json` y reiniciar. Cero cambios de código.

---

## 5. Orden de implementación

### Fase 0 — Preparación

- 🧑 Definir si trabajamos contra el repo del VPS (`~/servicios/edusapienxa`) por SSH o sobre una copia local. Recomendado: copia local + push a VPS.
- 🧑 Proveer secretos de desarrollo: OpenAI API key, Meta WhatsApp token de prueba, app secret, verify token. Quedan en `appsettings.Development.json` (gitignored) o user-secrets.
- 🧑 Confirmar branch base y crear `feature/agente-whatsapp`.
- 🤖 Inspeccionar la solución existente para confirmar capas, frameworks (EF Core ya?, MediatR ya?), y patrón actual de DI. Actualizar este plan si difiere.
- 🤖 Crear `FeaturesOptions`, `IFeatureFlags`, `ConfigurationFeatureFlags`. Probar con un endpoint dummy.

### Fase 1a — Dominio + persistencia

- 🤖 Crear entidades de dominio del §2 con value objects y state machine de `EnrollmentStatus`.
- 🤖 Crear `MoneyConverter` para EF (PostgreSQL `MONEY` ↔ `decimal`).
- 🤖 Crear configuraciones EF mapeando entidades genéricas a tablas existentes (`.ToTable("courses")`, etc.).
- 🤖 Crear migración EF: agregar `leads_enrollments.sale_agent` + crear `conversation_history`, `agent_config`, `payment_validations`.
- 🤖 Crear migración de seed con tres `AgentConfig`: `cursos_intent`, `cursos_general`, `cursos_pagos` (system prompts copiados de los workflows actuales).
- 🧑 Aplicar migraciones en BD de **desarrollo** (no producción).
- 🤖 Implementar repositorios EF.
- 🤖 Tests de integración con Testcontainers para repositorios críticos (Enrollment, Lead).

### Fase 1b — Endpoints activos

- 🤖 `POST /api/leads/capture` (`CaptureLeadHandler` + `LeadsController`).
- 🤖 `POST /api/enrollments` (`CreateEnrollmentHandler` + `EnrollmentsController`).
- 🤖 `GET /api/courses`, `GET /api/courses/{id}` (`CoursesController` que mapea `CatalogItem` → `CourseDto`).
- 🤖 `GET /api/instructors/{id}`.
- 🤖 Tests unitarios de handlers (idempotencia, validación, errores de dominio).
- 🧑 Verificar manualmente con Swagger que los 5 endpoints funcionan contra la BD real.

### Fase 1c — Endpoints gated (implementación completa)

- 🤖 `Checkout`, `RequestPaymentValidation`, `ResolvePaymentValidation`, `EscalateToHuman`, `GetPaymentMethods` con sus controladores.
- 🤖 `[PaymentsFeature]` attribute, `PaymentsFeatureGate`, `PaymentsFeatureSwaggerFilter`.
- 🤖 Test de integración: con flag=false, endpoints devuelven 404 y no aparecen en `/swagger/v1/swagger.json`. Con flag=true, funcionan.

### Fase 2 — Webhook WhatsApp

- 🤖 `MetaWhatsAppOptions`, `MetaWhatsAppClient` (envío + descarga de media usando `media_id` y bearer token).
- 🤖 `MetaWebhookSignatureValidator` + middleware.
- 🤖 `MetaIncomingMessageMapper` que convierte payload Meta → `IncomingMessage` neutral.
- 🤖 `WhatsAppWebhookController` con `GET` (challenge) y `POST` (eventos).
- 🤖 `IMediaTranscriber` (Whisper) + `IImageDescriber` (GPT-4o vision) en infra.
- 🤖 Test de integración del webhook con un payload real de Meta capturado en sandbox.
- 🧑 Configurar el callback en Meta Developer Console (URL, verify token).
- 🧑 Probar verificación de webhook (`GET`) con un mensaje real.

### Fase 3 — Orquestador del agente

- 🤖 `ConversationRepository` con consulta por ventana (`memoryWindow` desde `AgentConfig`).
- 🤖 `OpenAiLlmProvider` con tool calling y prompt caching habilitado.
- 🤖 `IIntentClassifier` (`OpenAiIntentClassifier`) que usa el `AgentConfig` `cursos_intent`.
- 🤖 `IAgent` + `GeneralAgent` (loop tool-calling estándar) + `PaymentAgent` (gated).
- 🤖 Tools del §1 (Application/Agents/Tools/) que invocan los handlers ya escritos en Fase 1.
- 🤖 `AgentRouter` con la regla del §4 (flag=false → siempre GeneralAgent).
- 🤖 `ProcessIncomingMessageHandler`: normaliza media → carga memoria → clasifica intent → resuelve agente → ejecuta turno → persiste mensajes → envía respuesta por `IMessagingChannel`.
- 🤖 Conectar `WhatsAppWebhookController.Post` al handler.
- 🤖 Test E2E con un mensaje simulado: "quiero info del curso de IA" → debe disparar `get_catalog_item_tool` y responder.
- 🧑 Smoke test con número real de WhatsApp en sandbox de Meta.

### Fase 4 — Cierre y despliegue

- 🤖 Ajustar `appsettings.json` con `PaymentsEnabled = false` y todas las opciones tipadas.
- 🤖 Verificar que Swagger en producción no muestra rutas de pagos.
- 🤖 Documentar en `README.md` cómo activar pagos cuando llegue el momento.
- 🧑 Aplicar migraciones en BD de producción en ventana de mantenimiento.
- 🧑 Despliegue en VPS (Traefik + proxy-network ya existen).
- 🧑 Cutover desde n8n: redirigir el webhook de Meta de n8n a la nueva URL .NET. Mantener n8n encendido un tiempo como backup.

---

## 6. Riesgos y decisiones abiertas

| # | Tema | Decisión sugerida | Bloqueo si no se resuelve |
|---|---|---|---|
| 1 | ¿La solución .NET ya tiene MediatR / FluentValidation? | Inspeccionar antes de Fase 1a y alinearse con lo que haya | Re-trabajo si difiere |
| 2 | ¿`leads_enrollments` ya tiene datos en producción? | Asumir sí; la migración debe ser aditiva (`ADD COLUMN ... NULL`) | Migración rota si destruye datos |
| 3 | Procesamiento del webhook: síncrono vs queue | Empezar síncrono, instrumentar latencia, mover a `Channel<T>` si supera ~2s | Timeouts de Meta (debe responder <20s) |
| 4 | ¿Notificación al humano de validación de pago? | En Fase 1c solo log + insert. Agregar email/Slack en Fase 4+ | Dueño humano no se entera de pendientes |
| 5 | Auth de los endpoints | API key estática compartida en esta fase. JWT/OAuth en fase posterior | OK para MVP server-to-server |
| 6 | Idioma de prompts en `AgentConfig` | Español (es-PE) — heredar de los workflows tal cual | Cliente actual lo requiere |

---

## 7. Resumen ejecutivo

- **Producto, no proyecto:** Domain habla `CatalogItem`, no `Course`. La capa API/config lo presenta como "curso" para Datapath.
- **`AgentConfig` desde día uno:** prompts, modelo, temperatura y memoria viven en BD. El hub multi-agente futuro hereda esto sin refactor.
- **Pagos: completos en código, invisibles en runtime.** Cuatro puntos de aplicación del flag (router, registro de tools, Swagger, gate de endpoints). Activación = cambiar un boolean.
- **Webhook Meta nativo,** no Evolution API. `IMessagingChannel` aísla el canal.
- **Validación humana asíncrona** vía `payment_validations` + endpoint de decisión separado. Adiós a `sendAndWait`.
- **Migración aditiva:** una sola migración EF agrega columna faltante y tres tablas nuevas. Cero `DROP`.
- **Orden:** Dominio + persistencia → endpoints activos → endpoints gated → webhook → orquestador. Cada fase es entregable y demostrable por separado.
