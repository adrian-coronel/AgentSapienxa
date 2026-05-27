using AgentSapienxa.Application.Catalog.Repositories;
using AgentSapienxa.Application.Common.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentSapienxa.Application.Agents.Tools;

public class GetCatalogItemTool : IAgentTool
{
    private readonly ICatalogRepository _catalog;
    public GetCatalogItemTool(ICatalogRepository catalog) => _catalog = catalog;

    public string Name => "get_catalog_item";
    public string Description => "Obtiene el detalle completo de un curso: descripción, temario, proyectos, costo e instructor. Úsala cuando el usuario quiera saber más de un curso específico.";
    public string JsonSchema => """
        {
          "type": "object",
          "properties": {
            "catalog_item_id": {
              "type": "string",
              "description": "UUID del curso obtenido del campo 'id' que devuelve get_catalog. Nunca uses el nombre del curso como ID."
            }
          },
          "required": ["catalog_item_id"]
        }
        """;

    public async Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct)
    {
        Args? args;
        try { args = JsonSerializer.Deserialize<Args>(arguments); }
        catch { return "{\"error\": \"Argumentos inválidos\"}"; }

        if (!Guid.TryParse(args?.CatalogItemId, out var id))
            return "{\"error\": \"catalog_item_id inválido\"}";

        var item = await _catalog.GetByIdAsync(id, ct);
        if (item is null) return "{\"error\": \"Curso no encontrado\"}";

        return JsonSerializer.Serialize(new
        {
            id = item.Id,
            titulo = item.Title,
            descripcion_corta = item.ShortDescription,
            detalles = item.Details,
            temario = item.Syllabus,
            proyectos = item.Projects,
            costo = item.Cost,
            plazas_disponibles = item.AvailablePlaces,
            inicio = item.StartDate?.ToString("dd/MM/yyyy") ?? "Por confirmar",
            link = item.Link
        });
    }

    private record Args([property: JsonPropertyName("catalog_item_id")] string? CatalogItemId);
}
