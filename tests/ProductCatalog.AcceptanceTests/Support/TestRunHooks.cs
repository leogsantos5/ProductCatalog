using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Infrastructure.Persistence;
using Reqnroll;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ProductCatalog.AcceptanceTests.Support;

[Binding]
public class TestRunHooks
{
    public static TestApiFactory Factory { get; private set; } = null!;

    [BeforeTestRun]
    public static async Task StartApiWithFreshDatabase()
    {
        Factory = new TestApiFactory();

        await WithDbContext(async db =>
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        });
    }

    [BeforeScenario]
    public static Task StartFromAnEmptyCatalogue() => WithDbContext(db => db.Products.ExecuteDeleteAsync());

    [AfterTestRun]
    public static async Task DropDatabaseAndStopApi()
    {
        await WithDbContext(db => db.Database.EnsureDeletedAsync());
        await Factory.DisposeAsync();
    }

    private static async Task WithDbContext(Func<AppDbContext, Task> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
