using DarkDhamon.Common.Extensions;

namespace SlipMap.Model.MapElements
{
    public class StarSystem
    {
        private List<StarSystem> _connectedSystems = null!;
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? Notes { get; set; }

        public override string ToString()
        {
            return $"{Name.IfNullOrWhitespace("Unnamed System")} ({ID})";
        }
    }
}
