using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProductCatalog.AcceptanceTests.Support;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=ProductCatalog_AcceptanceTests;Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Logging:LogLevel:Default", "Error");
    }
}
