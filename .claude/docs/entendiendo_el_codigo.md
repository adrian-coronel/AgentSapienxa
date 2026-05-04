# Entendiendo el Código — AgentSapienxa

> Guía de lectura del código. Sigue el orden de abajo hacia arriba: Domain → Application → Infrastructure → API.
> Cada sección explica el *por qué* antes de mostrar el *qué*.

---

## 1. La arquitectura en una imagen

```
┌─────────────────────────────────────────────────────────┐
│  API  (HTTP / WebhookController / Filters)              │ ← capa más externa
│    depende de ↓                                         │
├─────────────────────────────────────────────────────────┤
│  Infrastructure  (EF Core / OpenAI / Meta HTTP)         │
│    implementa interfaces de ↓                           │
├─────────────────────────────────────────────────────────┤
│  Application  (MediatR Handlers / Interfaces / Commands) │
│    usa entidades de ↓                                   │
├─────────────────────────────────────────────────────────┤
│  Domain  (Entities / Value Objects / State Machines)    │ ← núcleo puro
└─────────────────────────────────────────────────────────┘
```

**Regla clave:** las capas de abajo no conocen a las de arriba.
`Domain` no importa nada de `Infrastructure`.
`Application` nunca importa `Microsoft.EntityFrameworkCore`.

¿Para qué sirve esto? Si mañana cambias de PostgreSQL a MongoDB, o de OpenAI a Gemini, solo tocas `Infrastructure`. El resto del código queda intacto.

---

## 2. Domain — El núcleo del negocio

### 2.1 La clase base `Entity`

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    // Equals y GetHashCode por Id, no por referencia
}
```

Todas las entidades de negocio heredan de `Entity`. La igualdad se define por `Id`, no por si son el mismo objeto en memoria. Así, dos objetos `Lead` con el mismo `Guid` son "el mismo lead" aunque hayan sido cargados por separado.

---

### 2.2 El patrón `Result<T>`

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string error) => new(error);
}
```

En vez de lanzar excepciones para errores de negocio (ej: "transición de estado inválida"), devolvemos un `Result`. Las excepciones quedan para errores verdaderamente inesperados (bug de programación, fallo de red).

**Cuándo lo verás:** en `Enrollment.Transition()` y `PaymentValidation.Resolve()`.

---

### 2.3 `Lead` — El prospecto

```
Lead
├── PhoneNumber   ← identificador único externo (viene de WhatsApp)
├── LeadName
├── Email
├── ContactMethod  (WhatsApp, Instagram, etc.)
├── Status         (New / Active / Closed)
└── SalesAgentId   ← FK al agente de ventas asignado (nullable)
```

Métodos de negocio:
- `Create(phone, name?, email?, method?)` — factory, encapsula el constructor privado
- `UpdateInfo(name, email)` — solo actualiza si el valor es diferente (no genera un UPDATE innecesario a la BD)
- `AssignAgent(agentId)` — asigna el agente responsable

> **Por qué el constructor es `private`?**
> Para forzar que nadie cree un Lead "a medias". El factory `Create()` garantiza que siempre tenga `PhoneNumber` desde el inicio.

---

### 2.4 `Enrollment` + Máquina de estados

Un `Enrollment` es el interés de un `Lead` en un `CatalogItem` (curso).

```
Enrollment
├── LeadId
├── CatalogItemId
├── Status          ← controlado por la máquina de estados
├── TotalCost
├── PaymentMethodId
├── Voucher         ← URL del comprobante de pago
└── SaleAgentId     ← quién gestionó el cierre / escalamiento
```

La máquina de estados vive en `EnrollmentStatus`:

```
Interesado ──→ PendientePago ──→ Pagado
     │               │
     └──→ EscaladoAHumano ──→ Inactivo
     └──→ Inactivo
```

El código que la implementa es un diccionario de transiciones permitidas:

```csharp
private static readonly Dictionary<string, string[]> AllowedTransitions = new()
{
    [Interesado]       = [PendientePago, EscaladoAHumano, Inactivo],
    [PendientePago]    = [Pagado, EscaladoAHumano, Inactivo],
    [Pagado]           = [],          // estado final
    [EscaladoAHumano]  = [Inactivo],
    [Inactivo]         = []           // estado final
};
```

Cuando llamas `enrollment.Transition("Pagado")`, el método consulta este diccionario y devuelve `Result.Fail(...)` si la transición no está permitida. **El agente de IA nunca puede saltar estados.**

---

### 2.5 `AgentConfig` — El agente como dato

Esta entidad es el corazón del diseño "producto, no proyecto". En vez de tener agentes hardcodeados, cada agente es una fila en base de datos:

```
AgentConfig
├── AgentKey      ← "cursos.intent" / "cursos.general" / "cursos.pagos"
├── Name
├── SystemPrompt  ← el prompt completo que define el comportamiento
├── Model         ← "gpt-4.1" o "gpt-4.1-mini"
├── Temperature
├── MemoryWindow  ← cuántos mensajes anteriores ve el LLM
└── IsActive
```

¿Para qué? Mañana puedes cambiar el prompt de producción sin redeploy: editas la fila y reinicias. También puedes agregar un nuevo agente ("cobranzas", "soporte") sin tocar el código.

---

### 2.6 `ConversationMessage` — La memoria de la conversación

```
ConversationMessage
├── SessionId   ← número de teléfono del usuario (ej: "51987654321")
├── Role        ← "user" / "assistant" / "system" / "tool"
├── Content     ← texto del mensaje
├── ToolCalls   ← JSON con las llamadas a herramientas (si las hubo)
└── CreatedAt
```

Cada vez que el bot responde, guarda el mensaje del usuario y la respuesta del asistente. Al siguiente turno, carga los últimos `MemoryWindow` mensajes para dárselos al LLM como contexto. Así el bot "recuerda" la conversación.

---

### 2.7 `PaymentValidation` — Pago asíncrono

Reemplaza el `Gmail sendAndWait` de n8n. El flujo:

1. El bot recibe un voucher → crea una `PaymentValidation` con `Status = "Pendiente"`
2. El equipo de ventas recibe una notificación (pendiente implementar en Fase 3)
3. El asesor llama `POST /api/payments/validate/decision` → el handler llama `validation.Resolve()`
4. Si `Aprobado` → el `Enrollment` transiciona a `Pagado` y el bot manda un mensaje al usuario

```csharp
public Result Resolve(string decision, string resolvedBy, string? observation)
{
    if (Status != PaymentValidationStatus.Pendiente)
        return Result.Fail("Esta validación ya fue resuelta.");
    // ...
    Status = decision;
    ResolvedAt = DateTime.UtcNow;
    return Result.Ok();
}
```

---

## 3. Application — Los casos de uso

### 3.1 El patrón CQRS con MediatR

Cada operación del sistema es un **Command** (escribe) o una **Query** (lee). El controller nunca llama directamente a un repositorio; envía un mensaje y MediatR lo enruta al handler correcto.

```
Controller → IMediator.Send(comando) → Handler → Repositorio/Servicio → BD
```

Ventaja: el controller no sabe *cómo* se ejecuta la operación. Solo dice "quiero capturar un lead". El handler decide los detalles.

---

### 3.2 `CaptureLeadHandler` — Upsert de lead

```csharp
// ¿El teléfono ya existe en BD?
var existing = await _leads.GetByPhoneNumberAsync(cmd.PhoneNumber, ct);

if (existing is not null)
{
    existing.UpdateInfo(cmd.Name, cmd.Email);  // actualiza solo si difiere
    await _leads.UpdateAsync(existing, ct);
    return new CaptureLeadResult(existing.Id, IsNew: false, "Lead actualizado.");
}

// Lead nuevo: crear y asignar agente disponible
var lead = Lead.Create(cmd.PhoneNumber, cmd.Name, cmd.Email, cmd.ContactMethod);
var agent = await _agents.GetFirstAvailableAsync(ct);
if (agent is not null) lead.AssignAgent(agent.Id);

await _leads.AddAsync(lead, ct);
return new CaptureLeadResult(lead.Id, IsNew: true, "Lead capturado.");
```

El flujo es exactamente el workflow `CaptureLeads` de n8n, ahora en código tipado y testeable.

---

### 3.3 `CreateEnrollmentHandler` — Idempotencia

```csharp
// Primero verificar que el lead exista
var lead = await _leads.GetByPhoneNumberAsync(cmd.PhoneNumber, ct)
    ?? throw new InvalidOperationException("Lead no encontrado.");

// ¿Ya hay una inscripción activa para este curso?
var existing = await _enrollments.GetActiveAsync(lead.Id, cmd.CatalogItemId, ct);
if (existing is not null)
    return new CreateEnrollmentResult(existing.Id, IsIdempotent: true, "Ya estás inscrito.");

// Crear nueva
var enrollment = Enrollment.Create(lead.Id, cmd.CatalogItemId);
await _enrollments.AddAsync(enrollment, ct);
```

Si el agente de IA llama "register_enrollment" dos veces seguidas (error del LLM), no crea duplicados. Devuelve el enrollment existente. Esto se llama **idempotencia**.

---

### 3.4 Flujo de pago asíncrono (dos handlers)

**Handler 1 — RequestPaymentValidation:**
```
1. Verificar que lead y enrollment existen y coinciden
2. Crear PaymentValidation(Status="Pendiente")
3. Guardar en BD
4. Responder 202 Accepted al bot → el bot avisa al usuario "estamos revisando tu pago"
```

**Handler 2 — ResolvePaymentValidation:**
```
1. Cargar la validación pendiente
2. validation.Resolve(decision, ...)   ← lógica en el dominio
3. Si Aprobado: enrollment.Transition("Pagado") + mensaje WhatsApp al usuario
4. Si Inválido: mensaje WhatsApp explicando el problema
```

El asesor humano resuelve el pago en su tiempo, sin bloquear el servidor.

---

### 3.5 Las interfaces como "contratos"

En `Application/Common/Abstractions/` viven las interfaces que `Infrastructure` debe implementar:

| Interfaz | ¿Qué hace? | Implementación |
|---|---|---|
| `IMessagingChannel` | Envía mensajes de texto | `WhatsAppMessagingChannel` |
| `ILlmProvider` | Llama al LLM | `OpenAiLlmProvider` |
| `IMediaTranscriber` | Transcribe audio | `WhisperTranscriber` |
| `IImageDescriber` | Describe imágenes | `OpenAiImageDescriber` |
| `IFeatureFlags` | Feature flags | `ConfigurationFeatureFlags` |
| `IClock` | Hora UTC | `SystemClock` |

`Application` solo importa estas interfaces. Nunca importa `OpenAI`, `Npgsql`, ni `HttpClient`. Eso lo hace testeable: en los tests puedes pasar un `Mock<IMessagingChannel>()` sin necesitar WhatsApp real.

---

### 3.6 `ILlmProvider` — El contrato con el LLM

```csharp
public interface ILlmProvider
{
    Task<LlmResponse> CompleteAsync(
        string model,
        decimal temperature,
        string systemPrompt,
        IReadOnlyList<LlmMessage> messages,    // historial de conversación
        IReadOnlyList<LlmToolDefinition>? tools, // herramientas disponibles (tools del agente)
        CancellationToken ct = default);
}
```

Los tipos que entran y salen son records del dominio de la aplicación, no tipos de OpenAI:

```csharp
// Entrada
public record LlmMessage(string Role, string Content, string? ToolCalls = null);
public record LlmToolDefinition(string Name, string Description, string JsonSchema);

// Salida
public record LlmResponse(
    string Content,
    IReadOnlyList<LlmToolCall> ToolCalls,  // si el LLM quiere ejecutar una tool
    int? TokensIn, int? TokensOut, string? Model);
```

---

## 4. Infrastructure — Los detalles técnicos

### 4.1 Cómo la BD mapea entidades genéricas a tablas legacy

La tabla en PostgreSQL se llama `courses`, pero la entidad se llama `CatalogItem`. EF Core resuelve esto con configuración Fluent:

```csharp
// CatalogItemConfiguration.cs
builder.ToTable("courses");
builder.Property(x => x.InstructorId).HasColumnName("instructors");
```

La entidad no sabe nada del nombre de la tabla. Si la BD hubiera usado `products`, solo cambiaría esta línea.

---

### 4.2 El flujo de un mensaje WhatsApp entrante

```
WhatsApp (Meta)
    │
    ▼  POST /api/webhooks/whatsapp
MetaWebhookSignatureMiddleware
    │  ← valida HMAC-SHA256 con X-Hub-Signature-256
    │  ← si firma inválida → 401, stop
    ▼
WhatsAppWebhookController.Receive()
    │
    ▼
MetaIncomingMessageMapper.MapAsync(payload)
    │  ← itera entry[].changes[].value.messages[]
    │  ← si es audio/imagen → llama MetaWhatsAppClient.DownloadMediaAsync()
    │       → GET graph.facebook.com/{media_id}  (resuelve URL temporal)
    │       → GET {url_temporal}                 (descarga bytes)
    ▼
IncomingMessage (modelo neutro)
    │  SessionId = número de teléfono del usuario
    │  Type = Text | Audio | Image | Document
    │  MediaStream = bytes del archivo (si aplica)
    ▼
Controller: si Audio → WhisperTranscriber.TranscribeAsync()
            si Image → OpenAiImageDescriber.DescribeAsync()
    ▼
msg.Text = texto resultante
    │
    ▼  (Fase 3) ProcessIncomingMessageCommand → orquestador del agente
```

**¿Por qué el modelo neutro `IncomingMessage`?**
El orquestador del agente (Fase 3) no sabe si el mensaje vino de WhatsApp, Telegram, o SMS. Solo ve un `IncomingMessage` con `SessionId` y `Text`. Si mañana agregas Telegram, solo cambias el mapper; el agente no cambia.

---

### 4.3 `OpenAiLlmProvider` — Tool calls y memoria

El método `ToChatMessage()` reconstruye la conversación histórica para enviársela al LLM:

```csharp
private static ChatMessage ToChatMessage(LlmMessage msg)
{
    if (msg.Role == "user")
        return new UserChatMessage(msg.Content);

    if (msg.Role == "tool")
    {
        // Cuando role="tool", ToolCalls contiene el tool_call_id
        return new ToolChatMessage(toolCallId: msg.ToolCalls, content: msg.Content);
    }

    if (msg.Role == "assistant")
    {
        if (string.IsNullOrEmpty(msg.ToolCalls))
            return new AssistantChatMessage(msg.Content);  // respuesta normal

        // Si hubo tool calls: reconstruir desde JSON guardado en BD
        // formato: [{"Id":"call_xxx","Name":"get_catalog","Arguments":"{...}"}]
        var parsed = JsonSerializer.Deserialize<List<StoredToolCall>>(msg.ToolCalls);
        var chatToolCalls = parsed.Select(tc =>
            ChatToolCall.CreateFunctionToolCall(tc.Id, tc.Name, BinaryData.FromString(tc.Arguments))
        ).ToList();
        return new AssistantChatMessage(chatToolCalls);
    }
}
```

¿Por qué guardar los tool calls en JSON en la BD?
Porque el LLM necesita ver el historial completo: "el asistente llamó `get_catalog(id=5)`, obtuviste este resultado, luego respondiste…". Si no guardas los tool calls, el LLM pierde el contexto de qué acciones ya tomó.

---

## 5. API — La capa de entrada HTTP

### 5.1 El feature flag de pagos (4 capas de protección)

```
appsettings.json:  "Features": { "PaymentsEnabled": false }
                          ↓
ConfigurationFeatureFlags.PaymentsEnabled = false
                          ↓
┌──────────────────────────────────────────────────────────────┐
│ Punto 1 — Fase 3: AgentRouter siempre retorna GeneralAgent   │
│ Punto 2 — Fase 3: tools de pago no se incluyen en el schema  │
│ Punto 3 — PaymentsFeatureSwaggerFilter oculta los endpoints  │
│ Punto 4 — PaymentsFeatureGate devuelve 404 en runtime        │
└──────────────────────────────────────────────────────────────┘
```

`PaymentsFeatureGate` es un `IAsyncActionFilter`:
```csharp
public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
{
    if (!_flags.PaymentsEnabled)
    {
        ctx.Result = new NotFoundResult();  // ← 404 antes de ejecutar el controller
        return;
    }
    await next();
}
```

Se aplica con el atributo `[ServiceFilter(typeof(PaymentsFeatureGate))]` en los controllers de pagos.

Para activar pagos: cambiar `"PaymentsEnabled": true` en `appsettings.json` y reiniciar. Sin cambiar código.

---

### 5.2 El flujo de un request normal

Ejemplo: `POST /api/leads/capture`

```
HTTP Request
    ↓
LeadsController.Capture([FromBody] req)
    ↓ crea
CaptureLeadCommand(PhoneNumber, Name, Email, ContactMethod)
    ↓ IMediator.Send()
CaptureLeadValidator.Validate()   ← FluentValidation, revisa PhoneNumber.Create().IsSuccess
    ↓ si válido
CaptureLeadHandler.Handle()
    ↓ consulta
ILeadRepository.GetByPhoneNumberAsync()   ← interfaz
    ↓ implementada por
LeadRepository → ApplicationDbContext → PostgreSQL
    ↓
CaptureLeadResult → 200 OK { leadId, isNew, message }
```

---

## 6. Lo que viene en Fase 3

El webhook actualmente termina con un comentario:
```csharp
// Fase 3: await _mediator.Send(new ProcessIncomingMessageCommand(msg), ct);
```

En Fase 3 se implementará el orquestador completo:

```
IncomingMessage
    ↓
ProcessIncomingMessageHandler
    ├── Cargar historial de ConversationMessages (memoria)
    ├── IIntentClassifier.ClassifyAsync(msg.Text)
    │       → llama gpt-4.1-mini con AgentConfig["cursos.intent"]
    │       → retorna "general" | "pago" | "info_curso" | etc.
    ├── IAgentRouter.Route(intent)
    │       → si PaymentsEnabled=false → siempre GeneralAgent
    │       → si intent="pago" → PaymentAgent
    │       → si no → GeneralAgent
    ├── Agent.RunAsync(context)
    │       → loop: LLM.CompleteAsync() → si hay ToolCalls → ejecutar tools → repetir
    │       → tools disponibles: GetCatalog, RegisterEnrollment, RequestCheckout, etc.
    ├── Guardar ConversationMessages en BD
    └── IMessagingChannel.SendTextAsync(sessionId, respuesta)
              → WhatsAppMessagingChannel → Meta Graph API
```

---

## 7. Resumen de archivos clave

| Archivo | Responsabilidad |
|---|---|
| [Domain/Common/Entity.cs](../../src/AgentSapienxa.Domain/Common/Entity.cs) | Base de todas las entidades |
| [Domain/Common/Result.cs](../../src/AgentSapienxa.Domain/Common/Result.cs) | Errores de negocio sin excepciones |
| [Domain/Enrollments/EnrollmentStatus.cs](../../src/AgentSapienxa.Domain/Enrollments/EnrollmentStatus.cs) | Máquina de estados del enrollment |
| [Domain/Agents/AgentConfig.cs](../../src/AgentSapienxa.Domain/Agents/AgentConfig.cs) | Configuración del agente como entidad |
| [Application/Leads/Commands/CaptureLead/CaptureLeadHandler.cs](../../src/AgentSapienxa.Application/Leads/Commands/CaptureLead/CaptureLeadHandler.cs) | Lógica upsert de lead |
| [Application/Common/Abstractions/ILlmProvider.cs](../../src/AgentSapienxa.Application/Common/Abstractions/ILlmProvider.cs) | Contrato con el LLM |
| [Infrastructure/Messaging/WhatsApp/MetaIncomingMessageMapper.cs](../../src/AgentSapienxa.Infrastructure/Messaging/WhatsApp/MetaIncomingMessageMapper.cs) | Traduce payload Meta → modelo neutro |
| [Infrastructure/Llm/OpenAiLlmProvider.cs](../../src/AgentSapienxa.Infrastructure/Llm/OpenAiLlmProvider.cs) | Implementa ILlmProvider con OpenAI SDK 2.x |
| [API/Controllers/Webhooks/WhatsAppWebhookController.cs](../../src/AgentSapienxa.API/Controllers/Webhooks/WhatsAppWebhookController.cs) | Punto de entrada del mensaje WhatsApp |
| [API/Filters/PaymentsFeatureGate.cs](../../src/AgentSapienxa.API/Filters/PaymentsFeatureGate.cs) | Bloquea endpoints de pago en runtime |
| [Infrastructure/FeatureFlags/ConfigurationFeatureFlags.cs](../../src/AgentSapienxa.Infrastructure/FeatureFlags/ConfigurationFeatureFlags.cs) | Lee feature flags de appsettings.json |

---

## 8. Conceptos de C# que aparecen en el código

| Patrón | Dónde verlo | Qué hace |
|---|---|---|
| `private` constructor + factory `Create()` | `Lead`, `Enrollment`, `AgentConfig` | Garantiza que el objeto siempre nazca en estado válido |
| `record` | `LlmMessage`, `LlmToolCall`, `LlmResponse` | Tipo inmutable con igualdad por valor, ideal para DTOs |
| `IRequestHandler<TCommand, TResult>` | Todos los handlers | Contrato MediatR; el controller no conoce el handler |
| `IAsyncActionFilter` | `PaymentsFeatureGate` | Intercepta requests HTTP antes de llegar al action del controller |
| `IDocumentFilter` | `PaymentsFeatureSwaggerFilter` | Modifica el JSON de Swagger antes de servirlo |
| `ValueConverter<T, U>` | `MoneyConverter` | Traduce decimal ↔ string "S/ 150.00" al leer/escribir en PostgreSQL |
| `AddHttpClient<T>()` | `DependencyInjection.cs` | Pool de conexiones HTTP gestionado por `IHttpClientFactory` |
