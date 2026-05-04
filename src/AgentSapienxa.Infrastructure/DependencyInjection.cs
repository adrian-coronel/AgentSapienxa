using AgentSapienxa.Application.Agents.Repositories;
using AgentSapienxa.Application.Catalog.Repositories;
using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Conversations.Repositories;
using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Application.Payments.Repositories;
using AgentSapienxa.Infrastructure.FeatureFlags;
using AgentSapienxa.Infrastructure.Llm;
using AgentSapienxa.Infrastructure.Messaging.WhatsApp;
using AgentSapienxa.Infrastructure.Persistence;
using AgentSapienxa.Infrastructure.Persistence.Repositories;
using AgentSapienxa.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using System.ClientModel;

namespace AgentSapienxa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.Configure<FeaturesOptions>(opts => config.GetSection(FeaturesOptions.Section).Bind(opts));
        services.AddSingleton<IFeatureFlags, ConfigurationFeatureFlags>();
        services.AddSingleton<IClock, SystemClock>();

        // WhatsApp Meta client
        services.Configure<MetaWhatsAppOptions>(opts => config.GetSection(MetaWhatsAppOptions.Section).Bind(opts));
        services.AddHttpClient<MetaWhatsAppClient>();
        services.AddScoped<MetaIncomingMessageMapper>();
        services.AddScoped<IMessagingChannel, WhatsAppMessagingChannel>();

        // OpenAI / Groq client (shared singleton)
        services.AddSingleton(sp =>
        {
            var apiKey = config["OpenAI:ApiKey"] ?? string.Empty;
            var baseUrl = config["OpenAI:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
                return new OpenAIClient(
                    new ApiKeyCredential(apiKey),
                    new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });
            return new OpenAIClient(new ApiKeyCredential(apiKey));
        });

        // LLM providers
        services.AddScoped<ILlmProvider, OpenAiLlmProvider>();
        services.AddScoped<IMediaTranscriber, WhisperTranscriber>();
        services.AddScoped<IImageDescriber, OpenAiImageDescriber>();
        services.AddScoped<IIntentClassifier, OpenAiIntentClassifier>();

        // Repositories
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<ISalesAgentRepository, SalesAgentRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IPaymentValidationRepository, PaymentValidationRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IAgentConfigRepository, AgentConfigRepository>();

        return services;
    }
}
