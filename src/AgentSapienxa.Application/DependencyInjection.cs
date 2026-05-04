using AgentSapienxa.Application.Agents;
using AgentSapienxa.Application.Agents.Tools;
using AgentSapienxa.Application.Common.Abstractions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AgentSapienxa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Tools — todos registrados como IAgentTool; cada agente filtra por nombre
        services.AddScoped<IAgentTool, GetCatalogTool>();
        services.AddScoped<IAgentTool, GetCatalogItemTool>();
        services.AddScoped<IAgentTool, CaptureLeadTool>();
        services.AddScoped<IAgentTool, RegisterEnrollmentTool>();
        services.AddScoped<IAgentTool, CheckoutTool>();
        services.AddScoped<IAgentTool, RequestPaymentValidationTool>();
        services.AddScoped<IAgentTool, EscalateToHumanTool>();

        // Agentes — registrados como tipos concretos (AgentRouter los inyecta directamente)
        services.AddScoped<GeneralAgent>();
        services.AddScoped<PaymentAgent>();
        services.AddScoped<IAgentRouter, AgentRouter>();

        return services;
    }
}
