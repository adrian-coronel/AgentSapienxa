namespace AgentSapienxa.Domain.Enrollments;

public static class EnrollmentStatus
{
    public const string Interesado = "Interesado";
    public const string PendientePago = "Pendiente Pago";
    public const string Pagado = "Pagado";
    public const string EscaladoAHumano = "Escalado a Humano";
    public const string Inactivo = "Inactivo";

    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        [Interesado] = [PendientePago, EscaladoAHumano, Inactivo],
        [PendientePago] = [Pagado, EscaladoAHumano, Inactivo],
        [Pagado] = [],
        [EscaladoAHumano] = [Inactivo],
        [Inactivo] = []
    };

    public static bool CanTransitionTo(string current, string next) =>
        AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);

    public static readonly string[] ActiveStatuses = [Interesado, PendientePago, EscaladoAHumano];
}
