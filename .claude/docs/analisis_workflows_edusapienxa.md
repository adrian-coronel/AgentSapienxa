# Análisis de Workflows n8n — EduSapienxa
> Documento base para migración a .NET 8 | Generado para Claude Code

---

## 1. Mapa general de workflows

```
[WhatsApp] → WebhookWhatsApp
                  ↓
              [Cursos] ← workflow orquestador principal
              ├── capture_lead_tool         → [CaptureLeads]
              ├── course_enrollment_tool    → [LeadEnrollment]
              ├── generate_checkout         → [Checkout]
              ├── payment_validation_tool   → [Payment Validation Agent] (webhook HTTP)
              └── escalate_to_human_tool    → [Escale To Human - Enrollment]
```

Los 6 workflows forman **un solo sistema cohesionado**. `Cursos` es el orquestador; los demás son sub-herramientas invocadas por los agentes IA.

---

## 2. Análisis por workflow

---

### 2.1 `Cursos` — Orquestador principal

**Trigger:** Webhook HTTP POST desde Evolution API (WhatsApp)

**Flujo principal:**
1. Recibe mensaje → extrae variables (`vars`): `instance`, `sender`, `type`, `message`, `message_base64`, `mimetype`
2. Evalúa tipo de mensaje (`Evaluar` switch):
   - `conversation` → texto directo
   - `audioMessage` → descarga audio → transcribe con Whisper (OpenAI) → texto
   - `imageMessage` → descarga imagen en base64 → analiza con GPT-4o → descripción + sube a Google Drive
3. Unifica en `inputMessage` + `url` → pasa a `Edit Fields`
4. Recupera historial de conversación (`Get Chat History` con PostgresChatMemory)
5. Carga datos del usuario desde tabla `leads` (`User Data`)
6. **Analizer** (GPT-4.1-mini): analiza el último mensaje y devuelve estructura:
   - `sentimiento`: neutral/satisfecho/insatisfecho
   - `intent`: `pagar` | `general`
   - `terminar`: true/false
   - `rephrase`: reformulación independiente del mensaje
7. **Switch por intent:**
   - `pagar` → consulta enrollments pendientes → `Agente de pagos`
   - `general` (fallback) → `Agente general`
8. Ambos agentes devuelven `{ response, links }` → `Prepare Response` → envía por Evolution API

**Agente general (DatapathBot):** GPT-4.1
- Herramientas: `capture_lead_tool`, `course_enrollment_tool`, `get_course_tool`, `get_instructor_tool`
- Objetivo: obtener nombre, email, registrar interés en cursos
- Memoria: PostgresChatMemory (ventana 10 mensajes)

**Agente de pagos (PaymentBot):** GPT-4.1
- Herramientas: `get_payment_methods_tool`, `generate_checkout`, `payment_validation_tool`, `escalate_to_human_agent_tool`
- Objetivo: guiar proceso de pago, validar voucher, escalar si hay error
- Memoria: PostgresChatMemory

**Tablas consultadas directamente:**
- `leads` → datos del usuario
- `leads_enrollments` → enrollments con status NOT IN ('Inactivo', 'Pagado')
- `courses` → catálogo
- `payment_methods` → métodos disponibles
- `instructors` → datos del instructor

---

### 2.2 `CaptureLeads` — Captura y actualización de lead

**Trigger:** Sub-workflow invocado por `capture_lead_tool`

**Inputs:** `phone_number`, `name`, `email`, `contact_method`

**Lógica:**
1. Busca todos los registros en `leads`
2. Si el lead **ya existe** (`id` no vacío):
   - Actualiza `lead_name` y `email` solo si el nuevo valor difiere del existente
3. Si el lead **no existe**:
   - Obtiene lista de `sales_agents` → toma el primero (round-robin básico)
   - Crea nuevo lead con status `New` y `sales_agent` asignado
4. Retorna `{ status, message }`

**Tablas:**
- `leads` (select, insert, update)
- `sales_agents` (select)

---

### 2.3 `LeadEnrollment` — Registro de interés en curso

**Trigger:** Sub-workflow invocado por `course_enrollment_tool`

**Inputs:** `phone_number`, `course_interested` (UUID del curso)

**Lógica:**
1. Busca lead por `phone_number` → si no existe, retorna error indicando que primero debe usarse `capture_lead_tool`
2. Busca en `leads_enrollments` si ya existe combinación `(lead.id, course_interested)`
3. Si ya existe: retorna "ya registrado, no es necesario volver a registrar"
4. Si no existe: crea registro con `status = 'Interesado'`
5. Valida inserción → retorna `{ status, message }`

**Tablas:**
- `leads` (select)
- `leads_enrollments` (select, insert)

---

### 2.4 `Checkout` — Generación de datos de pago

**Trigger:** Sub-workflow invocado por `generate_checkout`

**Inputs:** `phone_number`, `course_to_pay` (UUID), `payment_method` (nombre)

**Lógica:**
1. Busca lead por `phone_number` → si no existe, retorna error
2. Busca enrollment activo: `lead.id + course_to_pay` con status NOT IN ('Inactivo', 'Pagado')
3. Valida que el enrollment corresponde al `course_to_pay`
4. Consulta detalle del curso (`courses`) y método de pago (`payment_methods`)
5. Convierte montos a numérico y compara `course.cost < payment_method.limit_amount`
6. Si el costo **excede** el límite: retorna error sugiriendo otro método
7. Si es válido: actualiza enrollment → `status = 'Pendiente Pago'`, guarda `payment_method.id`
8. Retorna: título del curso, monto, descripción e imagen del método de pago

**Tablas:**
- `leads` (select)
- `leads_enrollments` (select, update)
- `courses` (select)
- `payment_methods` (select)

---

### 2.5 `Payment Validation Agent` — Validación de voucher

**Trigger:** Webhook HTTP POST en `/webhook/validate/payment` (llamado por `payment_validation_tool`)

**Inputs (body):** `session_id`, `user_id`, `enrollment_id`, `voucher_detail`, `voucher_file`, `phone_number`

**Lógica:**
1. Busca lead por `phone_number` → valida existencia
2. Busca enrollment por `enrollment_id`
3. Consulta método de pago del enrollment → datos del agente de ventas asignado
4. Consulta detalle del curso
5. **Mueve el archivo voucher** de carpeta temporal (`Voucher`) a carpeta definitiva (`Pagos`) en Google Drive
6. **Envía email al agente de ventas** vía Gmail con `sendAndWait`:
   - Incluye: nombre del usuario, curso, monto, método de pago, hora, link al comprobante
   - Formulario de respuesta: `Validación` (Aprobado/Inválido) + `Observación` (textarea)
   - Timeout: 10 minutos
7. Responde inmediatamente al webhook con "validación en proceso"
8. Cuando el agente responde el email:
   - Si `Validación == 'Aprobado'`: actualiza enrollment → `status = 'Pagado'`
   - Si `Inválido`: no hace nada (continúa al agente de validación)
9. **Agente de validación** (GPT-4.1): notifica resultado al usuario vía WhatsApp

**Tablas:**
- `leads` (select)
- `leads_enrollments` (select, update)
- `courses` (select)
- `payment_methods` (select)
- `sales_agents` (select)

**Servicios externos:**
- Gmail (sendAndWait)
- Google Drive (move file)
- Evolution API (enviar mensaje)

---

### 2.6 `Escale To Human - Enrollment` — Escalamiento a humano

**Trigger:** Sub-workflow invocado por `escalate_to_human_agent_tool`

**Inputs:** `user_id`, `number_phone`, `enrollment_id`, `observation`

**Lógica:**
1. Busca lead por `phone_number`
2. Busca enrollment por `enrollment_id`
3. Verifica si el lead tiene agente asignado (`sales_agent`):
   - Si tiene: obtiene datos del agente asignado
   - Si no tiene: selecciona el primer agente disponible y lo asigna al lead
4. Actualiza enrollment:
   - `status = 'Escalado a Humano'`
   - `sale_agent = agente.id`
   - `observation = motivo del escalamiento`
5. Retorna confirmación de escalamiento

**Tablas:**
- `leads` (select, update — asignar agente si no tiene)
- `leads_enrollments` (select, update)
- `sales_agents` (select)

---

## 3. Modelo de datos real (Database.sql)

```sql
-- Tabla: sales_agents
CREATE TABLE sales_agents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    agent_name TEXT,
    email TEXT,
    phone_number TEXT,
    lead_classification_summary TEXT   -- perfil del agente para el LLM
);

-- Tabla: instructors
CREATE TABLE instructors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    instructor_name TEXT,
    email TEXT,
    phone_number TEXT,
    profile_picture TEXT,              -- URL Google Drive
    expertise TEXT,                    -- ej: 'Mathematics,Science'
    instructor_summary TEXT            -- descripción para el agente
);

-- Tabla: courses
CREATE TABLE courses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT,                         -- ej: 'DATAENG', 'AIENG'
    title TEXT,
    short_description TEXT,
    features TEXT,                     -- nivel de habilidad requerido
    details TEXT,                      -- descripción larga
    syllabus TEXT,                     -- contenido del curso (texto largo)
    projects TEXT,                     -- proyectos incluidos
    link TEXT,                         -- URL de checkout en datapath.ai
    instructors UUID REFERENCES instructors(id) ON DELETE CASCADE,
    cost MONEY,                        -- tipo MONEY de PostgreSQL
    places TEXT,                       -- cupos totales
    available_places TEXT,             -- cupos disponibles
    start_date DATE
);

-- Tabla: payment_methods
CREATE TABLE payment_methods (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT,                         -- 'Yape', 'Plin', 'BCP', 'Interbank'
    description TEXT,                  -- instrucciones de pago (número, cuenta, nombre)
    image TEXT,                        -- URL QR o logo (Google Drive)
    limit_amount MONEY                 -- tipo MONEY de PostgreSQL
);

-- Tabla: leads
CREATE TABLE leads (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lead_name TEXT,
    email TEXT,
    phone_number TEXT,
    contact_method TEXT,               -- 'WhatsApp'
    status TEXT,                       -- 'New', ...
    sales_agent UUID REFERENCES sales_agents(id) ON DELETE CASCADE
);

-- Tabla: leads_enrollments
CREATE TABLE leads_enrollments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lead UUID REFERENCES leads(id) ON DELETE CASCADE,
    course UUID REFERENCES courses(id) ON DELETE CASCADE,
    status TEXT,    -- 'Interesado' | 'Pendiente Pago' | 'Pagado' | 'Escalado a Humano' | 'Inactivo'
    observation TEXT,
    total_cost MONEY,
    payment_method UUID REFERENCES payment_methods(id) ON DELETE CASCADE,
    voucher TEXT                       -- URL del comprobante
);

-- Tabla de memoria conversacional (gestionada actualmente por n8n, migrar a .NET)
-- session_id = phone_number del usuario
-- Se reemplazará por tabla propia: conversation_history
```

### Observaciones importantes sobre el esquema real

- **`cost` y `limit_amount` son tipo `MONEY`** de PostgreSQL, no VARCHAR. El workflow de n8n tenía un nodo `Convertir` que parseaba strings con `S/` — eso era un workaround por inconsistencia de datos. En .NET se debe mapear directamente como `decimal`.
- **`leads_enrollments` no tiene columna `sale_agent`** en el SQL real — el workflow de escalamiento la referencia pero no existe en el DDL. Será necesaria una migración para agregarla.
- **`courses.instructors`** es singular (FK a un instructor), no array.
- **`leads`** no tiene los campos `lead_classification`, `lead_engagement_score`, `lead_follow_up_suggestion` que el workflow `Agente` usaba — probablemente eran campos calculados en memoria por el LLM, no persistidos.

---

## 4. Lógica de negocio crítica a preservar

### Detección de intent
El `Analizer` usa GPT para clasificar cada mensaje en:
- `pagar`: usuario quiere pagar, preguntar cómo pagar, o ya pagó
- `general`: todo lo demás (consultas, inscripción, datos personales, cursos)

Esta lógica debe implementarse como un paso de pre-procesamiento en .NET antes de rutear al agente correcto.

### Estado del enrollment (ciclo de vida)
```
Interesado → Pendiente Pago → Pagado
                           ↘ Escalado a Humano
                           ↘ Inactivo (cancelado)
```

### Asignación de agente de ventas
- Al crear un lead nuevo: se asigna el primer agente disponible (sin round-robin sofisticado)
- Al escalar: si el lead ya tiene agente, se usa ese; si no, se asigna el primero disponible

### Validación de pago (flujo humano en el loop)
El proceso actual requiere intervención humana vía email (Gmail sendAndWait). En .NET esto debe modelarse como un flujo asíncrono:
1. El agente recibe el voucher → dispara validación → responde "en proceso"
2. Un endpoint separado recibe la decisión del agente humano (aprobado/rechazado)
3. El sistema notifica al usuario por WhatsApp

### Límite de método de pago
Antes de confirmar el checkout, se valida que `course.cost < payment_method.limit_amount`. Los montos vienen como strings con prefijo "S/" y deben parsearse.

---

## 5. Decisión de scope — Módulo de pagos

> ⚠️ **El módulo de pagos se implementa pero NO se activa.**

El código de todo lo relacionado a pagos debe estar completo y correcto, pero deshabilitado para uso en producción:

| Componente | Qué hacer |
|---|---|
| `POST /api/checkout` | Implementar, pero marcar con `[ApiExplorerSettings(IgnoreApi = true)]` o equivalente. No exponer en Swagger. |
| `POST /api/payments/validate` | Ídem — implementar completo, no exponer. |
| `POST /api/enrollments/escalate` | Ídem. |
| **Agente de pagos** | Implementar la clase/servicio completo, pero el router de intent nunca debe rutear a él. Solo rutear a `Agente general`. |
| `get_payment_methods_tool` | Implementar el servicio, pero no registrarlo como tool activo del agente. |
| `generate_checkout tool` | Implementar el servicio, no registrar como tool. |

**Patrón recomendado:** usar un feature flag en `appsettings.json`:
```json
"Features": {
  "PaymentsEnabled": false
}
```
Todos los endpoints y el router de intent deben chequear este flag. Cuando sea `true` en el futuro, el módulo completo se activa sin tocar código.

---

## 5b. Lo que NO debe migrarse (excluir de scope completamente)

| Componente | Razón |
|---|---|
| Google Drive (vouchers temporales) | Acoplado al flujo de validación manual; se rediseñará |
| Gmail sendAndWait | Patrón n8n-específico; en .NET se implementa con notificaciones push / polling |
| Evolution API | Se reemplaza con WhatsApp Business API (Meta) |
| `get_course_tool_old` (Airtable) | Ya deshabilitado en n8n, no migrar |
| `get_payment_methods_tool_old` (Airtable) | Ídem |

---

## 6. Prompt para Claude Code

---

```
# CONTEXTO DEL PROYECTO

Eres Claude Code. Vas a ayudarme a migrar la lógica de negocio de 6 workflows de n8n a una API .NET 8 con Clean Architecture. El proyecto se llama **EduSapienxa**.

## Stack actual (ya existe en producción)
- Backend: .NET 8, C#, Clean Architecture (EduSapienxa.API + Application + Domain + Infrastructure)
- Base de datos: PostgreSQL (ya existe con tablas en uso)
- Repo en VPS: ~/servicios/edusapienxa
- Docker: sí, con Traefik + proxy-network

## Lo que hacen los workflows actuales (n8n)

El sistema tiene un agente conversacional de WhatsApp para venta de cursos educativos (empresa: Datapath).

### Flujo principal (`Cursos`):
1. Recibe webhook de WhatsApp (texto, audio o imagen)
2. Normaliza el mensaje (audio → Whisper, imagen → GPT-4o describe)
3. Recupera historial de conversación (PostgresChatMemory, session_id = phone_number)
4. Carga datos del usuario desde tabla `leads`
5. Clasifica la intención del mensaje: `pagar` | `general`
6. Rutea a dos agentes IA distintos según intención
7. Responde al usuario por WhatsApp

### Sub-workflows (tools del agente):
- **CaptureLeads**: upsert de lead por phone_number. Si nuevo, asigna agente de ventas. Inputs: phone_number, name, email, contact_method
- **LeadEnrollment**: registra interés en un curso. Evita duplicados. Inputs: phone_number, course_id
- **Checkout**: valida enrollment activo, verifica que costo del curso no supere límite del método de pago, actualiza status a 'Pendiente Pago'. Inputs: phone_number, course_id, payment_method_name
- **Payment Validation Agent**: recibe voucher (descripción + URL), notifica a agente humano, espera respuesta, actualiza status a 'Pagado' o escala
- **Escale To Human - Enrollment**: asigna agente al lead si no tiene, actualiza enrollment a 'Escalado a Humano'

### Modelo de datos (tablas PostgreSQL existentes — esquema real verificado):
- `sales_agents`: id, agent_name, email, phone_number, lead_classification_summary
- `instructors`: id, instructor_name, email, phone_number, profile_picture, expertise, instructor_summary
- `courses`: id, code, title, short_description, features, details, syllabus, projects, link, instructors (FK → instructors.id), cost (MONEY), places, available_places, start_date
- `payment_methods`: id, name, description, image, limit_amount (MONEY)
- `leads`: id, lead_name, email, phone_number, contact_method, status, sales_agent (FK → sales_agents.id)
- `leads_enrollments`: id, lead (FK), course (FK), status, observation, total_cost (MONEY), payment_method (FK), voucher

⚠️ **Columna faltante detectada:** `leads_enrollments` no tiene columna `sale_agent`. El workflow de escalamiento la referencia. Deberá agregarse via migración EF Core.

⚠️ **Tipo MONEY:** `cost` y `limit_amount` son tipo `MONEY` de PostgreSQL. Mapear como `decimal` en C#. No parsear strings con "S/".

⚠️ **Memoria conversacional:** actualmente en tabla interna de n8n. Crear tabla propia `conversation_history (id, session_id, role, content, created_at)` donde `session_id = phone_number`.

### Estados del enrollment:
`Interesado` → `Pendiente Pago` → `Pagado`
                              ↘ `Escalado a Humano`
                              ↘ `Inactivo`

## Lo que necesito ahora

**PRIMERO genera solo el plan en markdown. No escribas código todavía.**

El plan debe cubrir:

### Decisión importante sobre el módulo de pagos
**El módulo de pagos debe implementarse completo pero NO activarse.** Usar un feature flag:
```json
// appsettings.json
"Features": {
  "PaymentsEnabled": false
}
```
- Los endpoints de pagos deben existir en código pero no exponerse en Swagger ni ser accesibles mientras `PaymentsEnabled = false`
- El agente de pagos debe implementarse como clase completa, pero el router de intent **siempre rutea a `Agente general`** mientras el flag esté en false
- Los tools de pago (`get_payment_methods`, `generate_checkout`, `escalate_to_human`) deben implementarse como servicios pero no registrarse como tools activos del agente

### Fase 1 — Endpoints de negocio (equivalentes a los sub-workflows)

**Activos desde el inicio:**
1. `POST /api/leads/capture` → lógica de CaptureLeads
2. `POST /api/enrollments` → lógica de LeadEnrollment
3. `GET /api/courses` → listar cursos (ya existe o crear)
4. `GET /api/instructors/{id}` → detalle instructor (ya existe o crear)

**Implementar pero desactivar con feature flag `PaymentsEnabled`:**
5. `POST /api/checkout` → lógica de Checkout
6. `POST /api/payments/validate` → lógica de Payment Validation (asíncrono)
7. `POST /api/enrollments/escalate` → lógica de Escale To Human
8. `GET /api/payment-methods` → listar métodos de pago

### Fase 2 — Webhook de WhatsApp (Meta Business API — NO Evolution API)
Crear el endpoint que recibe mensajes desde WhatsApp Business API de Meta directamente:
- `POST /api/webhooks/whatsapp` — recibe eventos de Meta (formato `entry[].changes[].value`)
- `GET /api/webhooks/whatsapp` — verificación de webhook (Meta requiere responder `hub.challenge`)
- Normalización de tipos de mensaje entrante: `text`, `audio`, `image`
- **NO usar Evolution API** — el formato del webhook es completamente diferente

> ⚠️ **Pagos deshabilitados:** El agente de pagos, los endpoints de checkout/validación/escalamiento y los tools de pago deben implementarse completos pero controlados por feature flag `Features:PaymentsEnabled = false`. Mientras sea false: el router de intent siempre rutea a Agente general, los endpoints de pago no se exponen en Swagger, los tools de pago no se registran en el agente.

### Fase 3 — Orquestador del agente
Implementar la lógica del agente conversacional en .NET:
- Clasificación de intent (llamada a OpenAI con el mismo prompt del Analizer)
- Agente general (tool calling con OpenAI)
- Agente de pagos (tool calling con OpenAI)
- Memoria conversacional en PostgreSQL

## Restricciones de diseño importantes

1. **Arquitectura de producto, no de proyecto a medida**: Este sistema debe construirse como un producto reusable. El objetivo es que cuando se adapte a un nuevo cliente, el esfuerzo sea mayormente de configuración, no de desarrollo. Para lograrlo:
   - Separar el **núcleo genérico** (motor del agente, lógica de leads, enrollments, webhook) de la **capa de personalización** (prompts, nombre de empresa, tono, reglas de negocio).
   - Todo lo que puede variar por cliente debe vivir en base de datos, no en código. Esto incluye: prompts del sistema, nombre y descripción del agente, modelo LLM, temperatura, reglas de intent.
   - El catálogo no debe acoplarse al concepto "cursos" en las capas de dominio y aplicación. Nombrar las abstracciones de forma genérica (`CatalogItem`, `Product`) y que la capa de presentación/config lo llame "curso" para este cliente.
   - La tabla `AgentConfig` es el corazón de esta separación. Debe diseñarse desde el inicio, aunque en esta fase se pueble con valores fijos.

2. **Gestor de agentes futuro**: Un hub centralizado donde usuarios autenticados acceden desde su celular a distintos agentes especializados (cursos, ventas, cobranzas, soporte, etc.) según su rol. El agente de cursos es el primero de muchos. Por tanto:
   - `AgentConfig` debe ser una entidad de primera clase, no solo una tabla de configuración.
   - Los agentes deben ser registrables e intercambiables sin tocar código.
   - Ninguna decisión de diseño debe acoplar el sistema a "un solo agente".

3. **Canal de mensajería**: El webhook es para WhatsApp Business API de Meta, no Evolution API. El formato del webhook es diferente.

4. **Validación de pago asíncrona**: El flujo actual usa Gmail sendAndWait (síncrono en n8n). En .NET esto debe ser asíncrono: el endpoint responde inmediatamente y hay un endpoint separado para recibir la decisión del humano.

5. **No implementar en esta fase**: UI Angular, pagos automáticos, Google Drive, notificaciones push al agente humano (solo logging por ahora).

## Skills disponibles (ya instaladas en el proyecto)
- `obra/superpowers`: executing-plans, verification-before-completion, systematic-debugging
- `wshobson/agents`: api-design-principles
- `anthropics/skills`: webapp-testing

## Entregable esperado
Un archivo `plan_agente_whatsapp.md` con:
- Estructura de carpetas/archivos a crear dentro de Clean Architecture
- Lista de entidades de dominio a agregar (si faltan)
- Lista de endpoints con descripción, método HTTP, request/response schema
- Orden de implementación (qué va primero)
- Marcadores 🧑 (paso manual) y 🤖 (paso Claude Code)
```

---

## 7. Notas adicionales para el plan

### Sobre el campo `limit_amount` en `payment_methods`
Actualmente está almacenado como string con formato "S/ 150.00". En .NET deberá parsearse o mejor aún, migrar la columna a `NUMERIC` en BD.

### Sobre la memoria conversacional
n8n usa su propia tabla interna para `PostgresChatMemory`. Al migrar a .NET, la memoria debe reimplementarse. Opciones:
- Tabla propia `conversation_history (session_id, role, content, created_at)`
- Librería `Microsoft.SemanticKernel` que tiene abstracciones para memoria
- `Betalgo.OpenAI` o `OpenAI` SDK oficial con manejo manual del historial

### Sobre el `AgentConfig` (gestor de agentes futuro)
```sql
-- Tabla sugerida para cuando llegue el gestor
agent_config (
  id UUID PK,
  agent_key VARCHAR UNIQUE,     -- 'cursos_general', 'cursos_pagos'
  name VARCHAR,
  system_prompt TEXT,
  model VARCHAR,                -- 'gpt-4.1', 'gpt-4.1-mini'
  temperature DECIMAL,
  max_tokens INT,
  is_active BOOLEAN,
  created_at TIMESTAMP,
  updated_at TIMESTAMP
)
```
Aunque no se implementa ahora, los prompts deberían mapearse a esta estructura para facilitar la migración futura.
```
