using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductCatalog.AcceptanceTests.Support;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    // Set by CI and by the Docker setup in the README; LocalDB is the default on Windows.
    private const string ConnectionStringVariable = "ACCEPTANCE_TESTS_CONNECTION_STRING";

    private const string LocalDbConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=ProductCatalog_AcceptanceTests;Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) is { Length: > 0 } fromEnvironment ? fromEnvironment : LocalDbConnectionString;

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Logging:LogLevel:Default", "Error");
    }
}
