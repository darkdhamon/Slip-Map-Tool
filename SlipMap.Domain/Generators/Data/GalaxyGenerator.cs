using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMap.Domain.BusinessRules;
using SlipMap.Model.MapElements;

namespace SlipMap.Domain.Generators.Data
{
    public class GalaxyGenerator
    {
        public static Galaxy GenerateGalaxy(string? name = null, double maxRadius = 80000, double maxHeight = 1300, int initialNumberOfStarSystems = 500, bool gaia = false)
        {
            var generatedGalaxy = new Galaxy() { Name = name ?? $"GAL-{DateTime.Now:yyyyMMddHHmmss}" };
            for (var i = 0; i < initialNumberOfStarSystems; i++)
            {
                var generatedStarSystem = GeneratedStarSystem(maxRadius, maxHeight, gaia, gaia);
                generatedGalaxy.StarSystems.Add(generatedStarSystem);
            }
            return generatedGalaxy;
        }

        private static StarSystem GeneratedStarSystem(double maxRadius, double maxHeight, bool gaia, bool generatePlanets = false)
        {
            var system = new StarSystem
            {
                Name = string.Empty,
                Coordinates = GenerateGalacticCoordinates(maxRadius, maxHeight),
                SpectralType = GenerateSpectralType(),
                PlanetsGenerationCompleted = generatePlanets
            };

            if (generatePlanets)
            {
                GeneratePlanets(system, gaia);
            }

            return system;
        }

        private static void GeneratePlanets(StarSystem system, bool gaia)
        {
            var numberOfDice = DiceRoller.RollD4() - 1;
            var numberOfPlanets = DiceRoller.RollD6(numberOfDice);
            for (var i = 0; i < numberOfPlanets; i++)
            {
                var planet = new Planet()
                {
                    Name = string.Empty,
                    Class = PlanetClass.A
                };
                
                system.Planets.Add(planet);
            }
        }

        private static RadialMapCoordinates GenerateGalacticCoordinates(double maxRadius, double maxHeight)
        {
            return new RadialMapCoordinates()
            {
                Radius = DiceRoller.RollNSidedDie((int)maxRadius / 10, 10) - maxRadius / 3,
                AngleAsDegrees = DiceRoller.RollNSidedDie(360 * 100) / 100d,
                Offset = DiceRoller.RollNSidedDie((int)maxHeight / 10, 10) - maxHeight / 2
            };
        }

        private static SpectralType GenerateSpectralType()
        {
            var roll = DiceRoller.RollNSidedDie(10000000);
            switch (roll)
            {
                case 1: return SpectralType.O_Main;
                case <= 10001: return SpectralType.B_Main;
                case <= 80001: return SpectralType.A_Main;
                case <= 280001: return SpectralType.F_Main;
                case <= 630001: return SpectralType.G_Main;
                case <= 1430001: return SpectralType.K_Main;
                case <= 9430001: return SpectralType.M_Main;
                case <= 9431531: return SpectralType.G_Giant;
                case <= 9435028: return SpectralType.K_Giant;
                case <= 9470001: return SpectralType.M_Giant;
                case <= 9470002: return SpectralType.O_Super;
                case <= 9470003: return SpectralType.B_Super;
                case <= 9470004: return SpectralType.A_Super;
                case <= 9470006: return SpectralType.F_Super;
                case <= 9470010: return SpectralType.G_Super;
                case <= 9470016: return SpectralType.K_Super;
                case <= 9470096: return SpectralType.M_Super;
                case <= 9970096: return SpectralType.WhiteDwarf;
                case <= 9973087: return SpectralType.BlackHole;
                case <= 9979068: return SpectralType.NeutronStar;
                case <= 9988039: return SpectralType.ProtoStar;
                case <= 10000000: return SpectralType.BrownDwarf;
                default: return SpectralType.M_Main;
            }
        }
    }

    public enum PlanetClass
    {
        
        Terrestrial,
        Eden,

    }
}
