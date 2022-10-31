// See https://aka.ms/new-console-template for more information

using System.Drawing;
using SlipMap.Domain.BusinessRules;
using SlipMap.Domain.Generators.Data;
using SlipMap.Domain.Generators.Images;
using SlipMap.GalaxyImageGenerator.Windows;

var start = DateTime.Now;

Console.WriteLine("Beginning Experimentation");

var galaxy = GalaxyGenerator.GenerateGalaxy(maxHeight:200, maxRadius:2000, initialNumberOfStarSystems: 10000);
Console.WriteLine("Galaxy data created");

WindowsGalaxyImageGenerator galaxyImageGenerator = new WindowsGalaxyImageGenerator();
var stream = galaxyImageGenerator.TopView(galaxy);
File.WriteAllBytes($"{galaxy.Name}-TOPTest.jpg", stream.ToArray());
var galaxyMap = new Bitmap(2002, 2002);
using (var graphics = Graphics.FromImage(galaxyMap))
{
    var backgroundPen = new Pen(Color.Black);
    var axisPen = new Pen(Color.Red);
    graphics.DrawRectangle(backgroundPen, 0, 0, 2002, 2002);
    foreach (var system in galaxy.StarSystems)
    {
        var starPen = new Pen(MapColorVariables.ColorFromSpectralType(system.SpectralType));
        graphics.DrawEllipse(
            starPen,
            (int)system.Coordinates.X + 1000,
            (int)system.Coordinates.Y + 1000,
            1,
            1);
    }
    graphics.DrawLine(axisPen, 1001, 0, 1001, 2001);
    graphics.DrawLine(axisPen, 0, 1001, 2001, 1001);
    galaxyMap.Save($"{galaxy.Name}-TOP.jpg");
}
galaxyMap = new Bitmap(2002, 200);
using (var graphics = Graphics.FromImage(galaxyMap))
{
    var backgroundPen = new Pen(Color.Black);
    var axisPen = new Pen(Color.Red);
    graphics.DrawRectangle(backgroundPen, 0, 0, 2002, 2002);
    foreach (var system in galaxy.StarSystems)
    {
        var starPen = new Pen(MapColorVariables.ColorFromSpectralType(system.SpectralType));
        graphics.DrawEllipse(
            starPen,
            (int)system.Coordinates.X + 1000,
            (int)system.Coordinates.Z + 100,
            1,
            1);
    }
    graphics.DrawLine(axisPen, 1001, 0, 1001, 2001);
    graphics.DrawLine(axisPen, 0, 100, 2001, 100);
    galaxyMap.Save($"{galaxy.Name}-side.jpg");
}

Console.WriteLine("Galaxy Map generated");

var end = DateTime.Now;

Console.WriteLine($"{(end-start).TotalMilliseconds} ms");
Console.ReadLine();