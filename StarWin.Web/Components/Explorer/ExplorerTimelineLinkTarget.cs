namespace StarWin.Web.Components.Explorer;

public sealed record ExplorerTimelineLinkTarget(
    int SystemId,
    int WorldId = 0,
    int ColonyId = 0);
