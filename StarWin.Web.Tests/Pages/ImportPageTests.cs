using Bunit;
using Microsoft.Extensions.DependencyInjection;
using StarWin.Application.Services;
using StarWin.Application.Services.LegacyImport;
using StarWin.Domain.Model.Entity.Civilization;
using StarWin.Domain.Model.Entity.StarMap;
using StarWin.Domain.Model.ViewModel;
using StarWin.Web.Components.Pages;

namespace StarWin.Web.Tests.Pages;

public sealed class ImportPageTests : BunitContext
{
    [Fact]
    public void RendersSupportedImportSourcesWithoutStarWinJson()
    {
        ConfigureServices();

        var cut = Render<Import>();

        Assert.Contains("StarWin 2 files", cut.Markup);
        Assert.Contains("Slip Map JSON", cut.Markup);
        Assert.DoesNotContain("StarWin JSON", cut.Markup);
        Assert.DoesNotContain("legacy export packages", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectingSlipMapJsonUpdatesAcceptedFileTypes()
    {
        ConfigureServices();

        var cut = Render<Import>();
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Slip Map JSON").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("<span>Slip Map JSON</span>", cut.Markup);
            Assert.Contains("accept=\".json,application/json\"", cut.Markup);
        });
    }

    private void ConfigureServices()
    {
        Services.AddSingleton<IStarWinLegacyImportService>(new FakeLegacyImportService());
        Services.AddSingleton<IStarWinWorkspace>(new FakeWorkspace());
    }

    private sealed class FakeLegacyImportService : IStarWinLegacyImportService
    {
        public Task<StarWinLegacyImportPreview> PreviewStarWinZipAsync(
            Stream zipPackage,
            string packageName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StarWinLegacyImportPreview(packageName, [], [], []));
        }

        public Task<StarWinLegacyImportResult> ImportStarWinZipAsync(
            Stream zipPackage,
            string packageName,
            string targetSectorName,
            IProgress<StarWinLegacyImportProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StarWinLegacyImportResult(
                false,
                null,
                new StarWinLegacyImportPreview(packageName, [], [], []),
                ["Not implemented in test."]));
        }
    }

    private sealed class FakeWorkspace : IStarWinWorkspace
    {
        public bool IsLoaded => true;

        public IReadOnlyList<StarWinSector> Sectors => [];

        public StarWinSector CurrentSector { get; } = new();

        public IReadOnlyList<AlienRace> AlienRaces => [];

        public IReadOnlyList<Empire> Empires => [];

        public IReadOnlyList<EmpireContact> EmpireContacts => [];

        public CivilizationGeneratorSettings CivilizationSettings { get; } = new();

        public ArmyGeneratorSettings ArmySettings { get; } = new();

        public GurpsTemplate PreviewGurpsTemplate { get; } = new();

        public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
