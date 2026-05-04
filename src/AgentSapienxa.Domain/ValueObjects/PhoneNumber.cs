using AgentSapienxa.Domain.Common;
using System.Text.RegularExpressions;

namespace AgentSapienxa.Domain.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    public string Value { get; }

    private static readonly Regex PhonePattern = new(@"^\+?[1-9]\d{6,14}$", RegexOptions.Compiled);

    private PhoneNumber(string value) => Value = value;

    public static Result<PhoneNumber> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<PhoneNumber>.Fail("El número de teléfono es requerido.");

        var normalized = raw.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (!normalized.StartsWith('+'))
            normalized = "+" + normalized;

        if (!PhonePattern.IsMatch(normalized))
            return Result<PhoneNumber>.Fail($"Formato de teléfono inválido: {raw}");

        return Result<PhoneNumber>.Ok(new PhoneNumber(normalized));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
