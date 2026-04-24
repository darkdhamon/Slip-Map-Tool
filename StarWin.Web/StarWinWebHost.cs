using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.Configuration;
using StarWin.Application.Services;
using StarWin.Infrastructure;
using StarWin.Web.Components;

namespace StarWin.Web;

public static class StarWinWebHost
{
    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        return WebApplication.CreateBuilder(args);
    }

    public static WebApplicationBuilder CreateBuilder(WebApplicationOptions options)
    {
        return WebApplication.CreateBuilder(options);
    }

    public static WebApplication Build(WebApplicationBuilder builder)
    {
        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
        ApplyDevelopmentDatabaseDefaults(builder);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddStarWinInfrastructure(builder.Configuration);

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();
        app.UseStaticFiles();

        app.MapGet("/desktop/health", () => Results.Ok(new
        {
            status = "ok"
        }));
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }

    public static async Task InitializeAsync(WebApplication app)
    {
        if (ShouldApplyMigrationsOnStartup(app.Configuration))
        {
            await app.Services.MigrateStarWinDatabaseAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            await app.Services.SeedStarWinDevelopmentDataAsync();
        }

    }

    private static bool ShouldApplyMigrationsOnStartup(IConfiguration configuration)
    {
        var configuredValue = configuration.GetValue<bool?>("StarWin:ApplyMigrationsOnStartup");
        if (configuredValue.HasValue)
        {
            return configuredValue.Value;
        }

        var provider = configuration["StarWin:DatabaseProvider"] ?? "SqlServer";
        return provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyDevelopmentDatabaseDefaults(WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            return;
        }

        var hostKind = builder.Configuration["StarforgedAtlas:HostKind"];
        if (string.Equals(hostKind, "Desktop", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var provider = builder.Configuration["StarWin:DatabaseProvider"] ?? "SqlServer";
        var connectionString = builder.Configuration.GetConnectionString("StarWin");
        if (!provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(connectionString, DefaultLocalDbConnectionString, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sharedDesktopDatabasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Starforged Atlas",
            "starforged-atlas.db");

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["StarWin:DatabaseProvider"] = "Sqlite",
            ["ConnectionStrings:StarWin"] = $"Data Source={sharedDesktopDatabasePath}"
        });
    }

    private const string DefaultLocalDbConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=StarWin;Trusted_Connection=True;MultipleActiveResultSets=true";
}
