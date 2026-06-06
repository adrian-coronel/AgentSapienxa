# Prompt para Claude Code: Plan de Implementación RAG Multi-Empresa

## Contexto del Proyecto

### AgentSapienxa
- **Tipo:** Platform de agentes IA con webhook de WhatsApp
- **Stack:** .NET 8 Clean Architecture, PostgreSQL, n8n, OpenAI APIs
- **Deployment:** Docker + Traefik en VPS (31.97.243.179)
- **Actual:** Chatbot para cursos con información hardcodeada
- **Objetivo:** Hacer el chatbot flexible para leer documentos de la empresa

### Base de Datos Actual
- PostgreSQL con tablas: `courses`, `leads`, `instructors`, `payment_methods`, `agent_config`, `conversation_history`
- Extensión pgvector disponible para embeddings vectoriales
- Migraciones con EF Core

### Stack Disponible
- `.NET 8` con Entity Framework Core
- `OpenAI APIs` (ya tenemos key de Groq y OpenAI)
- `PostgreSQL` con pgvector
- `Angular 21` para frontend
- `Tailwind CSS v4` para estilos

---

## Requisitos Capturados

### 1. Alcance
- **Por Empresa:** Se podrá cargar documentos por empresa
- **Tipos de Documento:** PDF y Word (.docx)
- **Contexto Actual:** Chatbot es global pero se planea migrar a multi-empresa

### 2. Frecuencia de Actualizaciones
- **Medianas:** No es diaria, pero tampoco infrecuente
- Implicación: No requiere invalidación de cache sofisticada, pero sí versionado

### 3. UX de Upload
- **Asincrónico:** No bloqueante
- **Cola de procesamiento:** Encolado de uploads
- **Progreso Visual:** Barra con porcentaje en UI
- **Sin bloqueador:** Usuario puede seguir usando el app mientras procesa

---

## Tu Tarea: Generar Plan de Implementación

**Crea un plan detallado (markdown estructurado) que incluya:**

### 1. Arquitectura de Datos
- Tablas SQL necesarias (document_uploads, document_chunks, upload_queue, etc.)
- Índices para búsqueda rápida
- Relaciones con empresa/usuario
- Consideraciones de escalabilidad

### 2. Componentes Backend (.NET 8 Clean Architecture)
- Entities en Domain layer
- Repositories en Infrastructure
- Commands/Queries en Application
- Services (DocumentProcessing, Embedding, FileStorage, Parser)
- Background jobs (Hangfire o equivalent)
- API endpoints con su contrato (request/response)

### 3. Componentes Frontend (Angular 21 + Tailwind)
- Componentes necesarios
- Estado management (si es necesario)
- Integración con API
- Polling/WebSocket para progreso
- Pantallas/Vistas

### 4. Flujos de Datos
- **Flujo Upload:** Usuario → API → Queue → Background Job → DB → Frontend Poll
- **Flujo RAG Query:** Pregunta → Embedding → Vector Search → Chunks → LLM → Respuesta
- Incluye diagramas ASCII si es útil

### 5. Consideraciones Técnicas
- Estrategia de chunking (tamaño, overlap)
- Servicio de embeddings (OpenAI vs local)
- Límites de upload (tamaño máximo, cantidad)
- Manejo de errores
- Seguridad (validación de archivos, aislamiento por empresa)

### 6. Pasos de Implementación
- Separar en pasos 🧑 (manuales) y 🤖 (Claude Code)
- Incluir orden lógico de desarrollo
- Estimación de esfuerzo por componente

### 7. Próximas Fases (Post-MVP)
- Mejoras que no son MVP pero son importantes

---

## Contexto Técnico Adicional

### Skills Disponibles en Claude Code
- `obra/superpowers` - Execution and verification
- `wshobson/agents` - API design, .NET patterns
- `anthropics/skills` - Frontend design

### Convenciones del Proyecto
- Clean Architecture: Domain → Application → Infrastructure → API
- EF Core para data access
- FluentValidation para validaciones
- MediatR para Commands/Queries
- Async/await en todo
- Spanish en documentación y comentarios

### Restricciones
- No agregar dependencias externas innecesarias
- Reutilizar patterns ya usados en AgentSapienxa
- Mantener compatibilidad con Docker (el deployment es containerizado)
- pgvector ya está instalado en PostgreSQL

---

## Formato Esperado

Genera el plan como **markdown estructurado con:**
- Títulos claros (# ## ###)
- Listas y sublistas
- Bloques de código (sql, csharp, typescript)
- Tablas donde sea útil
- Diagramas ASCII para flujos complejos
- Estimaciones claras

**Archivo de salida:** `plan_rag_implementation.md`

---

## Preguntas Guía (para tu análisis)

1. ¿Cuál es la estructura de tablas SQL mínima necesaria?
2. ¿Cómo dividiré responsabilidades entre componentes .NET?
3. ¿Qué endpoints API son necesarios y cuál es su contrato?
4. ¿Cómo manejo el upload asincrónico sin bloquear?
5. ¿Cómo integro RAG en el flujo actual del chatbot sin romper nada?
6. ¿Cuál es el orden lógico para implementar (que depende de qué)?
7. ¿Qué consideraciones de seguridad necesito (validación, aislamiento)?

---

## Comenzar

Analiza el contexto anterior y **genera un plan ejecutable** que Claude Code (u otro dev) pueda seguir paso a paso para implementar RAG en AgentSapienxa.

El plan debe ser lo suficientemente detallado para que alguien desconocido del proyecto pueda implementarlo, pero sin ser tan granular que se pierda en detalles.