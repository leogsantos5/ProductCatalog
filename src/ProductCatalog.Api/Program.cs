using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Middleware;
using ProductCatalog.Application;
using ProductCatalog.Infrastructure;
using ProductCatalog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Product Catalog API",
        Version = "v1"
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Auto-apply migrations + seed on startup in dev, so `dotnet run` is enough to get a
    // working database — deliberately not done this way in production (see README).
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
