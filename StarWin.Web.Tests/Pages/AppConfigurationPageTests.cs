using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class AppConfigurationPageTests : BunitContext
{
    [Fact]
    public void RendersDangerResetActionAndNavigationLink()
    {
        ConfigureServices();

        var cut = Render<AppConfiguration>();
        var button = cut.Find("button.danger-action");

        Assert.Contains("Reset database", button.TextContent);
        Assert.Contains("rebuild it from the current application schema", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResetConfirmationModalAppearsBeforeResetRuns()
    {
        var resetService = new FakeAppConfigurationService();
        ConfigureServices(resetService);

        var cut = Render<AppConfiguration>();
        cut.Find("button.danger-action").Click();

        Assert.Contains("Reset the Starforged Atlas database?", cut.Markup);
        Assert.Equal(0, resetService.ResetCallCount);
    }

    [Fact]
    public void ConfirmResetInvokesResetServiceAndReloadsWorkspace()
    {
        var resetService = new FakeAppConfigurationService();
        var workspace = new FakeWorkspace();
        ConfigureServices(resetService, workspace);

        var cut = Render<AppConfiguration>();
        cut.Find("button.danger-action").Click();
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Confirm reset", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, resetService.ResetCallCount);
            Assert.Equal(1, workspace.ReloadCallCount);
            Assert.Contains("Database reset complete", cut.Markup);
            Assert.DoesNotContain("Reset the Starforged Atlas database?", cut.Markup);
        });
    }

    private void ConfigureServices(FakeAppConfigurationService? resetService = null, FakeWorkspace? workspace = null)
    {
        Services.AddSingleton<IStarWinAppConfigurationService>(resetService ?? new FakeAppConfigurationService());
        Services.AddSingleton<IStarWinWorkspace>(workspace ?? new FakeWorkspace());
    }

    private sealed class FakeAppConfigurationService : IStarWinAppConfigurationService
    {
        public int ResetCallCount { get; private set; }

        public Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
        {
            ResetCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWorkspace : IStarWinWorkspace
    {
        public int ReloadCallCount { get; private set; }

        public bool IsLoaded => true;

        public IReadOnlyList<StarWinSector> Sectors => [];

        public StarWinSector CurrentSector { get; } = new();

        public IReadOnlyList<AlienRace> AlienRaces => [];

        public IReadOnlyList<Empire> Empires => [];

        public IReadOnlyList<EmpireContact> EmpireContacts => [];

        public CivilizationGeneratorSettings CivilizationSettings { get; } = new();

        public ArmyGeneratorSettings ArmySettings { get; } = new();

        public GurpsTemplate PreviewGurpsTemplate { get; } = new();

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            ReloadCallCount++;
            return Task.CompletedTask;
        }
    }
}
