using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
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
}
