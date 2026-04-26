namespace StarWin.Web.Components.Layout;

public sealed class SectorExplorerLayoutStateStore
{
    public SectorExplorerLayoutState? State { get; private set; }

    public event Action? Changed;

    public void Update(SectorExplorerLayoutState state)
    {
        State = state;
        Changed?.Invoke();
    }
}
