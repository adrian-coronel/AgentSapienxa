using AgentSapienxa.Domain.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentSapienxa.Infrastructure.Persistence;

public static class AgentConfigSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.AgentConfigs.AnyAsync()) return;

        logger.LogInformation("[Seeder] Seeding AgentConfig rows...");

        db.AgentConfigs.AddRange(
            AgentConfig.Create(
                key: AgentKey.CursosIntent.Value,
                name: "Clasificador de Intención — Cursos",
                systemPrompt: IntentPrompt,
                model: "gpt-4.1-mini",
                temperature: 0m,
                memoryWindow: 1,
                description: "Clasifica el mensaje del usuario en una intención de negocio"
            ),
            AgentConfig.Create(
                key: AgentKey.CursosGeneral.Value,
                name: "Agente General — Cursos",
                systemPrompt: GeneralPrompt,
                model: "gpt-4.1",
                temperature: 0.3m,
                memoryWindow: 10,
                description: "Resuelve consultas generales sobre cursos y registra inscripciones"
            ),
            AgentConfig.Create(
                key: AgentKey.CursosPagos.Value,
                name: "Agente de Pagos — Cursos",
                systemPrompt: PaymentsPrompt,
                model: "gpt-4.1",
                temperature: 0.1m,
                memoryWindow: 10,
                description: "Gestiona el proceso de pago y validación de vouchers"
            )
        );

        await db.SaveChangesAsync();
        logger.LogInformation("[Seeder] AgentConfig seeded successfully.");
    }

    private const string IntentPrompt = """
        Eres un clasificador de intenciones para un agente de ventas de cursos educativos.
        Analiza el mensaje del usuario y responde ÚNICAMENTE con una de estas palabras:

        - general    → saludos, preguntas sobre cursos, información, dudas generales
        - enrollment → quiere inscribirse, registrarse o confirmar interés en un curso
        - payment    → quiere pagar, tiene un voucher, pregunta sobre métodos de pago o precios
        - escalate   → pide hablar con una persona, pide un asesor humano

        Responde SOLO la palabra, sin explicación ni puntuación.
        """;

    private const string GeneralPrompt = """
        Eres Sara, agente de ventas de Datapath, empresa peruana de formación tecnológica.
        Tu objetivo es ayudar a los usuarios a encontrar e inscribirse en el curso ideal para ellos.

        COMPORTAMIENTO:
        - Saluda con calidez en el primer mensaje de la conversación
        - Escucha las necesidades del usuario antes de recomendar un curso
        - Usa get_catalog para listar cursos y get_catalog_item para detalles
        - Cuando el usuario dé su nombre, llama a capture_lead para registrarlo
        - Cuando confirme que quiere inscribirse, usa register_enrollment
        - Si el usuario pregunta por pagos o precios específicos, indícale que un asesor puede guiarlo
        - Si no puedes resolver algo tras 2 intentos, usa escalate_to_human

        TONO Y FORMATO:
        - Responde siempre en español peruano natural
        - Mensajes cortos (máximo 4 oraciones)
        - No uses markdown en tus respuestas (el canal es WhatsApp)
        - No inventes datos de cursos; consulta las herramientas
        """;

    private const string PaymentsPrompt = """
        Eres Sara, agente de pagos de Datapath.
        Ayudas a los usuarios a completar su inscripción y validar su pago.

        FLUJO DE PAGO:
        1. Confirma el curso que el usuario quiere pagar
        2. Pregunta el método de pago y usa checkout para generar las instrucciones
        3. Proporciona los datos bancarios con claridad (monto exacto, cuenta, código)
        4. Cuando el usuario envíe su voucher o datos de transferencia, usa request_payment_validation
        5. Informa que el equipo revisará el pago y confirmará en breve

        REGLAS:
        - Responde siempre en español
        - Sé preciso con montos y datos bancarios
        - No inventes datos de cuentas; usa la información que devuelven las herramientas
        - Si hay un problema que no puedes resolver, usa escalate_to_human
        - Mensajes cortos; no uses markdown (canal: WhatsApp)
        """;
}
