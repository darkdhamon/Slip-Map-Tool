using SlipMap.Domain.Generators.Images;
using SlipMap.Domain.Generators.Images.Abstract;
using SlipMap.Model.MapElements;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace SlipMap.GalaxyImageGenerator.Windows;

public class WindowsGalaxyImageGenerator : IGalaxyImageGenerator
{
    public MemoryStream TopView(Galaxy galaxy)
    {
        var maxX = galaxy.StarSystems.Max(system => system.Coordinates.X);
        var minX = galaxy.StarSystems.Min(system => system.Coordinates.X);
        var maxY = galaxy.StarSystems.Max(system => system.Coordinates.Y);
        var minY = galaxy.StarSystems.Min(system => system.Coordinates.Y);
        var imageWidth = 0d;
        var imageHeight = 0d;
        if (minX < 0)
        {
            imageWidth = maxX - minX + 10;
        }
        if (minY < 0)
        {
            imageHeight = maxY - minY + 10;
        }

        var memoryStream = new MemoryStream();
        var galaxyImage = new Bitmap((int)imageWidth+1, (int)imageHeight+1);
        using var canvas = Graphics.FromImage(galaxyImage);
        var backgroundPen = new Pen(Color.Black);
        var axisPen = new Pen(Color.Red);
        canvas.DrawRectangle(backgroundPen, 0, 0, (int)imageWidth, (int)imageHeight);
        foreach (var system in galaxy.StarSystems.OrderBy(system=>system.Coordinates.Z))
        {
            var starPen = new Pen(MapColorVariables.ColorFromSpectralType(system.SpectralType));
            var starRadius = 0;

            if (system.SpectralType.HasFlag(SpectralType.Size_Dwarf))
            {
                starRadius = 1;
            }
            if (system.SpectralType.HasFlag(SpectralType.Size_MainSequence))
            {
                starRadius = 2;
            }
            if (system.SpectralType.HasFlag(SpectralType.Size_SuperGiantStar))
            {
                starRadius = 3;
            }
            if (system.SpectralType.HasFlag(SpectralType.Size_SuperGiantStar))
            {
                starRadius = 4;
            }

            var starCordX =(int) (system.Coordinates.X + imageWidth / 2+5);
            var starCordY =(int) (system.Coordinates.Y + imageHeight / 2+5);

            
            if (system.SpectralType is not SpectralType.BlackHole and not SpectralType.None)
            {
                if (!system.SpectralType.HasFlag(SpectralType.Size_Dwarf))
                {
                    canvas.DrawLine(starPen,
                        starCordX - starRadius,
                        starCordY,
                        starCordX + starRadius,
                        starCordY);
                    canvas.DrawLine(starPen,
                        starCordX,
                        starCordY - starRadius,
                        starCordX,
                        starCordY + starRadius);
                }
                canvas.DrawEllipse(
                    starPen,
                    (int)(starCordX - starRadius / 2d ),
                    (int)(starCordY - starRadius / 2d ),
                    starRadius,
                    starRadius);
            }
            else if (system.SpectralType is SpectralType.BlackHole)
            {
                canvas.FillEllipse(
                    Brushes.Black,
                    (int)(starCordX - starRadius / 2d),
                    (int)(starCordY - starRadius / 2d),
                    starRadius,
                    starRadius);
                canvas.DrawEllipse(
                    starPen,
                    (int)(starCordX - starRadius / 2d),
                    (int)(starCordY - starRadius / 2d),
                    starRadius,
                    starRadius);
            }
            else
            {
                canvas.DrawEllipse(
                    starPen,
                    (int)(starCordX - starRadius / 2d),
                    (int)(starCordY - starRadius / 2d),
                    starRadius,
                    starRadius);
            }

            
        }

        // Draw vertical Line
        canvas.DrawLine(axisPen, (int)Math.Abs(minX), 0, (int)(Math.Abs(minX)), (int)imageHeight);
        // Draw Horizontal Line
        canvas.DrawLine(axisPen, 0, (int)Math.Abs(minY), (int)imageWidth, (int)Math.Abs(minY));
        
        // Save and return Drawing
        galaxyImage.Save(memoryStream, ImageFormat.Jpeg);
        return memoryStream;
    }

    public MemoryStream SideView(Galaxy galaxy)
    {
        throw new NotImplementedException();
    }
}