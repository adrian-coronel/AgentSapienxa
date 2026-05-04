using AgentSapienxa.Application.Common.Abstractions;
using Microsoft.Extensions.Options;

namespace AgentSapienxa.Infrastructure.FeatureFlags;

public class FeaturesOptions
{
    public const string Section = "Features";
    public bool PaymentsEnabled { get; set; } = false;
}

public class ConfigurationFeatureFlags : IFeatureFlags
{
    private readonly FeaturesOptions _opts;
    public ConfigurationFeatureFlags(IOptions<FeaturesOptions> opts) => _opts = opts.Value;

    public bool PaymentsEnabled => _opts.PaymentsEnabled;
}
