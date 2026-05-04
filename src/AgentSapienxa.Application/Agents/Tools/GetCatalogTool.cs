using AgentSapienxa.Application.Catalog.Repositories;
using AgentSapienxa.Application.Common.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentSapienxa.Application.Agents.Tools;

public class GetCatalogTool : IAgentTool
{
    private readonly ICatalogRepository _catalog;
    public GetCatalogTool(ICatalogRepository catalog) => _catalog = catalog;

    public string Name => "get_catalog";
    public string Description => "Lista los cursos disponibles. Úsala cuando el usuario pregunte qué cursos hay o pida opciones.";
    public string JsonSchema => """
        {
          "type": "object",
          "properties": {
            "only_available": {
              "type": "boolean",
              "description": "Si true, solo retorna cursos con plazas disponibles"
            }
          }
        }
        """;

    public async Task<string> ExecuteAsync(string arguments, AgentContext context, CancellationToken ct)
    {
        bool onlyAvailable = false;
        try
        {
            var args = JsonSerializer.Deserialize<Args>(arguments);
            onlyAvailable = args?.OnlyAvailable ?? false;
        }
        catch { }

        var items = await _catalog.GetAllAsync(onlyAvailable, ct);
        if (items.Count == 0) return "No hay cursos disponibles en este momento.";

        var list = items.Select(i => new
        {
            id = i.Id,
            titulo = i.Title,
            costo = i.Cost,
            plazas_disponibles = i.AvailablePlaces,
            inicio = i.StartDate?.ToString("dd/MM/yyyy") ?? "Por confirmar"
        });

        return JsonSerializer.Serialize(list);
    }

    private record Args([property: JsonPropertyName("only_available")] bool OnlyAvailable = false);
}
