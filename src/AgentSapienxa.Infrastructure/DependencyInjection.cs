using AgentSapienxa.Application.Common.Abstractions;
using AgentSapienxa.Application.Companies.Repositories;
using AgentSapienxa.Application.Documents.Repositories;
using AgentSapienxa.Application.Agents.Repositories;
using AgentSapienxa.Application.Catalog.Repositories;
using AgentSapienxa.Application.Conversations.Repositories;
using AgentSapienxa.Application.Enrollments.Repositories;
using AgentSapienxa.Application.Leads.Repositories;
using AgentSapienxa.Application.Payments.Repositories;
using AgentSapienxa.Infrastructure.CurrentCompany;
using AgentSapienxa.Infrastructure.Documents.Chunking;
using AgentSapienxa.Infrastructure.Documents.Conversion;
using AgentSapienxa.Infrastructure.Documents.Embedding;
using AgentSapienxa.Infrastructure.Documents.Processing;
using AgentSapienxa.Infrastructure.Documents.Storage;
using AgentSapienxa.Infrastructure.FeatureFlags;
using AgentSapienxa.Infrastructure.Llm;
using AgentSapienxa.Infrastructure.Messaging;
using AgentSapienxa.Infrastructure.Messaging.WhatsApp;
using AgentSapienxa.Infrastructure.Persistence;
using AgentSapienxa.Infrastructure.Persistence.Repositories;
using AgentSapienxa.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using System.ClientModel;

namespace AgentSapienxa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        // Current company accessor — scoped so HTTP requests and background scopes each get their own instance
        services.AddScoped<ICurrentCompanyAccessor, ScopedCurrentCompanyAccessor>();

        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                o => o.UseVector()));

        services.Configure<FeaturesOptions>(opts => config.GetSection(FeaturesOptions.Section).Bind(opts));
        services.AddSingleton<IFeatureFlags, ConfigurationFeatureFlags>();
        services.AddSingleton<IClock, SystemClock>();

        // WhatsApp Meta client
        services.Configure<MetaWhatsAppOptions>(opts => config.GetSection(MetaWhatsAppOptions.Section).Bind(opts));
        services.AddHttpClient<MetaWhatsAppClient>();
        services.AddScoped<MetaIncomingMessageMapper>();

        if (env.IsDevelopment())
            services.AddScoped<IMessagingChannel, SimulationMessagingChannel>();
        else
            services.AddScoped<IMessagingChannel, SimulationMessagingChannel>();

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

        // Document services
        services.AddScoped<IDocumentToMarkdownConverter, PureDotNetMarkdownConverter>();
        services.AddScoped<IEmbeddingProvider, OpenAiEmbeddingProvider>();
        services.AddScoped<ISemanticChunker, HybridSemanticChunker>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        // Background worker
        services.AddHostedService<DocumentProcessingWorker>();

        // Repositories
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IDocumentUploadRepository, DocumentUploadRepository>();
        services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
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
