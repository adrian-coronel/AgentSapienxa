using AgentSapienxa.API.Filters;
using AgentSapienxa.API.Middleware;
using AgentSapienxa.Application;
using AgentSapienxa.Infrastructure;
using AgentSapienxa.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddScoped<PaymentsFeatureGate>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "AgentSapienxa API", Version = "v1" });
    opts.DocumentFilter<PaymentsFeatureSwaggerFilter>();
});

var app = builder.Build();

// Seed AgentConfig rows on startup (no-op if already populated)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await AgentConfigSeeder.SeedAsync(db, seederLogger);
}

app.UseSwagger();
app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v1/swagger.json", "AgentSapienxa v1"));

app.UseMiddleware<MetaWebhookSignatureMiddleware>();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
