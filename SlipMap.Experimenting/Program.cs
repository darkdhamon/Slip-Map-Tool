// See https://aka.ms/new-console-template for more information

using SlipMap.Domain.Generators.Data;
using SlipMap.GalaxyImageGenerator.Windows;
using SlipMap.Model.MapElements;

var start = DateTime.Now;

Console.WriteLine("Beginning Experimentation");

var galaxy = GalaxyGenerator.GenerateGalaxy(maxHeight:200, maxRadius:2000, initialNumberOfStarSystems: 10000);
Console.WriteLine("Galaxy data created");
Console.WriteLine("Galaxy has: ");
Console.WriteLine(
    @$"
 - Black holes     : {galaxy.StarSystems.Count(system=>system.SpectralType==SpectralType.BlackHole)}
 - Neutron Stars   : {galaxy.StarSystems.Count(system=>system.SpectralType == SpectralType.NeutronStar)}
 - Proto Stars     : {galaxy.StarSystems.Count(system=>system.SpectralType == SpectralType.ProtoStar)}
 - Main Sequence   : {galaxy.StarSystems.Count(system=>system.SpectralType.HasFlag(SpectralType.Size_MainSequence))}
 - Dwarf Star      : {galaxy.StarSystems.Count(system=>system.SpectralType.HasFlag(SpectralType.Size_Dwarf))}
 - Giant Star      : {galaxy.StarSystems.Count(system=>system.SpectralType.HasFlag(SpectralType.Size_GiantStar))}
 - Super Giant Star: {galaxy.StarSystems.Count(system=>system.SpectralType.HasFlag(SpectralType.Size_SuperGiantStar))}
");
WindowsGalaxyImageGenerator galaxyImageGenerator = new WindowsGalaxyImageGenerator();
var stream = galaxyImageGenerator.TopView(galaxy, 6);
File.WriteAllBytes($"{galaxy.Name}-TopView.jpg", stream.ToArray());
Console.WriteLine("Galaxy Map Top View generated");
stream = galaxyImageGenerator.SideView(galaxy, 6);
File.WriteAllBytes($"{galaxy.Name}-SideView.jpg", stream.ToArray());
Console.WriteLine("Galaxy Map Side View generated");

var checkedSystems = new List<StarSystem>();
double? shortestDistance = null;
double? longestDistance = null;
StarSystem? system1 = null;
StarSystem? system2 = null;
foreach (var currentSystem in galaxy.StarSystems)
{
    checkedSystems.Add(currentSystem);
    foreach (var targetSystem in galaxy.StarSystems.Except(checkedSystems))
    {
        var distance = currentSystem.Coordinates.DistanceFrom(targetSystem.Coordinates);
        if (shortestDistance==null || distance < shortestDistance)
        {
            shortestDistance = distance;
            system1 = currentSystem;
            system2 = targetSystem;
        }
        if (longestDistance == null || distance > longestDistance)
        {
            longestDistance = distance;
        }
    }
}

Console.WriteLine($"{system1} and {system2} are the closest together with a distance of {shortestDistance} LY between them.");
Console.WriteLine($"{longestDistance} LY separate the most distant systems.");

var end = DateTime.Now;

Console.WriteLine($"{(end-start).TotalMilliseconds} ms");
Console.ReadLine();