using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
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
        Assert.Contains("Coming soon", cut.Markup);
        Assert.DoesNotContain("StarWin JSON", cut.Markup);
        Assert.DoesNotContain("legacy export packages", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Collect all files for the single StarWin 2 sector you want to import", cut.Markup);
        Assert.Contains("All files for the sector should use the same base file name", cut.Markup);
        Assert.Contains(".sun", cut.Markup);
        Assert.Contains(".pln", cut.Markup);
        Assert.Contains(".mon", cut.Markup);
        Assert.Contains(".aln", cut.Markup);
        Assert.Contains(".col", cut.Markup);
        Assert.Contains(".con", cut.Markup);
        Assert.Contains(".emp", cut.Markup);
        Assert.Contains(".his", cut.Markup);
        Assert.Contains(".nam", cut.Markup);
    }

    [Fact]
    public void SlipMapJsonSourceIsDisabledUntilImplemented()
    {
        ConfigureServices();

        var cut = Render<Import>();
        var slipMapButton = cut.FindAll("button").Single(button => button.TextContent.Contains("Slip Map JSON", StringComparison.Ordinal));

        Assert.True(slipMapButton.HasAttribute("disabled"));
        Assert.Contains("Coming soon", slipMapButton.TextContent);
        Assert.Contains("class=\"import-source-badge\"", cut.Markup);
        Assert.Contains("<span class=\"import-source-label\">StarWin 2 files</span>", cut.Markup);
        Assert.Contains("accept=\".zip,application/zip,application/x-zip-compressed\"", cut.Markup);
    }

    [Fact]
    public void ImportSuccessDestinationTargetsSectorExplorerOverview()
    {
        var destinationBuilder = typeof(Import).GetMethod("BuildExplorerImportDestination", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(destinationBuilder);
        Assert.Equal(
            "/sector-explorer?sectorId=42",
            destinationBuilder!.Invoke(null, [42]));
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
