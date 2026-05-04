using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AgentSapienxa.Infrastructure.Persistence.Conversions;

public class MoneyConverter : ValueConverter<decimal, string>
{
    public MoneyConverter() : base(
        v => v.ToString("F2"),
        v => ParseMoney(v))
    { }

    private static decimal ParseMoney(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;
        var clean = raw.Replace("$", "").Replace("S/", "").Replace(",", "").Trim();
        return decimal.TryParse(clean, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0m;
    }
}
