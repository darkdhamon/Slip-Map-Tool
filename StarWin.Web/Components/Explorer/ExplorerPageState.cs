using Microsoft.JSInterop;
using StarWin.Domain.Model.Entity.StarMap;

namespace StarWin.Web.Components.Explorer;

internal static class ExplorerPageState
{
    internal const string ExplorerSessionStorageKey = "starforgedAtlas.explorerSelection";

    internal static int ResolveSelectedSystemId(StarWinSector sector, int? requestedSystemId, int currentSelectedSystemId)
    {
        if (requestedSystemId is int requestedId && sector.Systems.Any(system => system.Id == requestedId))
        {
            return requestedId;
        }

        if (currentSelectedSystemId > 0 && sector.Systems.Any(system => system.Id == currentSelectedSystemId))
        {
            return currentSelectedSystemId;
        }

        return sector.Systems.FirstOrDefault()?.Id ?? 0;
    }

    internal static async Task<ExplorerSessionSelection?> RestoreSelectionAsync(IJSRuntime js, int? requestedSectorId)
    {
        if (requestedSectorId is not null)
        {
            return null;
        }

        string? storedValue;
        try
        {
            storedValue = await js.InvokeAsync<string?>("sessionStorage.getItem", ExplorerSessionStorageKey);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ExplorerSessionSelection>(storedValue);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    internal static async Task PersistSelectionAsync(IJSRuntime js, bool browserSessionReady, ExplorerSessionSelection selection)
    {
        if (!browserSessionReady)
        {
            return;
        }

        var value = System.Text.Json.JsonSerializer.Serialize(selection);
        try
        {
            await js.InvokeVoidAsync("sessionStorage.setItem", ExplorerSessionStorageKey, value);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSException)
        {
        }
    }
}

internal sealed record ExplorerSessionSelection(int SectorId, int SystemId, bool AutoLoadSectorMap = false, string? SectionSlug = null);
