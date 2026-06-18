using GenAI.AI;
using GenAI.Api.Extensions;
using GenAI.Api.Middleware;
using GenAI.Application;
using GenAI.Infrastructure;
using GenAI.Infrastructure.Persistence;
using GenAI.RAG;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddAI(builder.Configuration)
    .AddRAG(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting GenAI.Api");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "GenAI.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
