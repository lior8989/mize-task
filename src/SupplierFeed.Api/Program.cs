using System.Reflection;
using Microsoft.OpenApi.Models;
using SupplierFeed.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Supplier Feed Throttle & Dedup API",
        Version = "v1",
        Description = "Ingests supplier reservation updates with deduplication and per-supplier throttling."
    });

    // Surface the XML documentation comments in the Swagger UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

var app = builder.Build();

await app.Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync(CancellationToken.None);

// Swagger is enabled in every environment on purpose: this is a scoped exercise
// and reviewers should be able to try the API without extra configuration.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

/// <summary>
/// Entry point marker, exposed so integration tests can host the app with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
