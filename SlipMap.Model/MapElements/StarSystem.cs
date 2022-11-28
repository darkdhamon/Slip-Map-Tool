using System.ComponentModel.DataAnnotations.Schema;
using DarkDhamon.Common.EntityFramework.Model;
using DarkDhamon.Common.Extensions;

namespace SlipMap.Model.MapElements
{
    public class StarSystem: IEntity<int>
    {
        private List<Planet>? _planets;
        public string? Name { get; set; }
        public string? Notes { get; set; }

        [NotMapped]
        public RadialMapCoordinates Coordinates { get; set; }

        public string GalacticCoordinates
        {
            get => Coordinates.ToJson();
            set => Coordinates.FromJson(value);
        }

        public SpectralType SpectralType { get; set; }
        public bool PlanetsGenerationCompleted { get; set; }

        public List<Planet> Planets
        {
            get => _planets??=new List<Planet>();
            set => _planets = value;
        }

        public override string ToString()
        {
            return $"{Name!.IfNullOrWhitespace("Unnamed System")} ({Id}) [{Coordinates}]";
        }

        public int Id { get; set; }
    }
}
