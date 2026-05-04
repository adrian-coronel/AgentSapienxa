# AgentSapienxa — WhatsApp AI Sales Agent

> Arquitectura del sistema basada en Clean Architecture de 4 capas.

---

## Flujo completo de un mensaje entrante

```
WhatsApp User
      │
      ▼
Meta Webhook POST
      │
      ▼
HMAC Signature ──── firma inválida ──→ 401 Unauthorized
      │ firma válida
      ▼
Message Mapper
      │
      ├── Texto ──────────────────────────────────────┐
      │                                               │
      ├── Audio ──→ Whisper STT (transcripción) ──────┤
      │                                               │
      └── Imagen ─→ GPT-4o Vision (descripción) ──────┘
                                                      │
                                                      ▼
                                          IncomingMessage (modelo neutro)
                                                      │
                                             - - - - - - - - -
                                            |     Fase 3      |
                                             - - - - - - - - -
                                                      │
                                                      ▼
                                           Agent Orchestrator
```

---

## Agent Orchestrator (Fase 3)

```
IncomingMessage
      │
      ▼
Intent Classifier
      │  clasifica el texto del usuario
      ▼
Agent Router
      │
      ├──→ GeneralAgent (cyan)
      │         │ maneja info de cursos e inscripciones
      │         │
      └──→ PaymentAgent (orange) ← bloqueado por feature flag OFF
                │
                ▼
          Tool Calling Loop ◄──────────────┐
                │                          │
                ▼                          │
        Ejecutar Tool ──── resultado ──────┘
                │  (cuando el LLM ya no pide más tools)
                ▼
        IMessagingChannel
                │
                ▼
        WhatsApp (respuesta al usuario)
```

### Feature Flag — Pagos

| Estado | Comportamiento |
|--------|---------------|
| `PaymentsEnabled: false` (default) | `PaymentAgent` bloqueado · endpoints devuelven `404` · oculto en Swagger |
| `PaymentsEnabled: true` | `PaymentAgent` activo · endpoints visibles y funcionales |

> Para activar: cambiar `"PaymentsEnabled": true` en `appsettings.json` y reiniciar. Sin redeploy.

---

## Clean Architecture — 4 Capas

Las dependencias fluyen **solo hacia abajo**. Las capas superiores no son conocidas por las inferiores.

```
┌─────────────────────────────────────────────────────────────────┐
│  4 — API                                                        │
│      REST endpoints · Swagger · PaymentsFeatureGate             │
│                        depends on ↓                             │
├─────────────────────────────────────────────────────────────────┤
│  3 — INFRASTRUCTURE                                             │
│      PostgreSQL · OpenAI · Meta (WhatsApp) · HTTP client        │
│                        depends on ↓                             │
├─────────────────────────────────────────────────────────────────┤
│  2 — APPLICATION                                                │
│      MediatR handlers · ILlmProvider interface                  │
│      Repository interfaces                                      │
│                        depends on ↓                             │
├─────────────────────────────────────────────────────────────────┤
│  1 — DOMAIN                                                     │
│      Lead · Enrollment state machine · AgentConfig              │
│      PaymentValidation                                          │
└─────────────────────────────────────────────────────────────────┘
```

### Capa 1 — Domain

| Componente | Descripción |
|---|---|
| `Lead` | Prospecto identificado por número de teléfono |
| `Enrollment` | Interés de un Lead en un CatalogItem (curso) |
| `EnrollmentStatus` | Máquina de estados: `Interesado → PendientePago → Pagado` |
| `AgentConfig` | Configuración del agente (prompt, modelo, temperatura) como entidad de BD |
| `PaymentValidation` | Registro de validación de pago asíncrona (reemplaza Gmail sendAndWait) |

**Estado machine del Enrollment:**

```
Interesado ──→ PendientePago ──→ Pagado (final)
     │               │
     └──→ EscaladoAHumano ──→ Inactivo (final)
     └──→ Inactivo (final)
```

### Capa 2 — Application

| Componente | Descripción |
|---|---|
| `CaptureLeadHandler` | Upsert de lead por teléfono; asigna agente de ventas disponible |
| `CreateEnrollmentHandler` | Registra interés; idempotente (no duplica) |
| `CheckoutHandler` | Valida monto vs. límite del método de pago |
| `RequestPaymentValidationHandler` | Crea `PaymentValidation` pendiente → 202 Accepted |
| `ResolvePaymentValidationHandler` | Asesor aprueba/rechaza → notifica al usuario por WhatsApp |
| `ILlmProvider` | Interfaz hacia el LLM (OpenAI, intercambiable) |
| `IMessagingChannel` | Interfaz de envío de mensajes (WhatsApp, intercambiable) |

### Capa 3 — Infrastructure

| Componente | Tecnología |
|---|---|
| Repositorios EF Core | PostgreSQL via Npgsql |
| `OpenAiLlmProvider` | OpenAI SDK 2.x — `ChatClient` con tool calls |
| `WhisperTranscriber` | OpenAI Whisper-1 — audio → texto |
| `OpenAiImageDescriber` | GPT-4o vision — imagen → descripción |
| `MetaWhatsAppClient` | Meta Graph API — envío + descarga de media |
| `WhatsAppMessagingChannel` | Implementa `IMessagingChannel` via Meta |
| `MetaIncomingMessageMapper` | Payload Meta → `IncomingMessage` neutro |
| `ConfigurationFeatureFlags` | Lee `appsettings.json:Features:PaymentsEnabled` |

### Capa 4 — API

| Componente | Ruta | Estado |
|---|---|---|
| `LeadsController` | `POST /api/leads/capture` | ✅ Activo |
| `EnrollmentsController` | `POST /api/enrollments` | ✅ Activo |
| `CoursesController` | `GET /api/courses` · `GET /api/courses/{id}` | ✅ Activo |
| `InstructorsController` | `GET /api/instructors/{id}` | ✅ Activo |
| `CheckoutController` | `POST /api/checkout` | 🔒 Gated |
| `PaymentMethodsController` | `GET /api/payment-methods` | 🔒 Gated |
| `PaymentsController` | `POST /api/payments/validate` · `/decision` | 🔒 Gated |
| `WhatsAppWebhookController` | `GET /api/webhooks/whatsapp` · `POST` | ✅ Activo |

---

## Estado de implementación

| Fase | Descripción | Estado |
|---|---|---|
| 0 — Setup | Solución .NET 9, NuGet, estructura de proyectos | ✅ Completo |
| 1a — Domain + Persistencia | Entidades, EF configs, migración aditiva | ✅ Completo |
| 1b — Endpoints activos | Leads, Enrollments, Courses, Instructors | ✅ Completo |
| 1c — Endpoints gated | Checkout, Payments, Escalate + feature flag | ✅ Completo |
| 2 — Canal WhatsApp | Webhook, mapper, media, Whisper, GPT-4o vision | ✅ Completo |
| **3 — Orquestador** | **Intent classifier, agentes, tools, loop** | 🔴 Pendiente |
| 4 — Despliegue | Docker, migraciones prod, cutover desde n8n | ⏳ Pendiente |

> **Pendiente manual del usuario:**
> - `dotnet ef database update` para aplicar la migración a la BD local
> - Agregar claves reales en `appsettings.Development.json` (OpenAI, Meta)
> - Registrar la URL del webhook en Meta Business Console
