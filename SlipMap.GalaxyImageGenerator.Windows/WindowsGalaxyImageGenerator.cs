using SlipMap.Domain.Generators.Images;
using SlipMap.Domain.Generators.Images.Abstract;
using SlipMap.Model.MapElements;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace SlipMap.GalaxyImageGenerator.Windows;

public class WindowsGalaxyImageGenerator : IGalaxyImageGenerator
{
    private enum Axis
    {
        X,
        Y,
        Z
    }
    public MemoryStream TopView(Galaxy galaxy, double scale = 1)
    {
        GetImageDimensions(galaxy, Axis.Y, Axis.X, out var minX, out var minY, out var imageWidth, out var imageHeight, scale);

        var memoryStream = new MemoryStream();
        var galaxyImage = new Bitmap((int)imageWidth+1, (int)imageHeight+1);
        using var canvas = Graphics.FromImage(galaxyImage);
        var backgroundPen = new Pen(Color.Black);
        var axisPen = new Pen(Color.Red);
        canvas.DrawRectangle(backgroundPen, 0, 0, (int)imageWidth, (int)imageHeight);
        foreach (var system in galaxy.StarSystems.OrderBy(system=>system.Coordinates.Z))
        {
            var starPen = new Pen(MapColorVariables.ColorFromSpectralType(system.SpectralType));
            var starRadius = GetStarRadius(system);
            var (starCordX, starCordY) = GetStarPosition(system, imageWidth, imageHeight, scale);
            var maxRadiusScale = scale < 3 ? scale : 3;
            if (system.SpectralType is not SpectralType.BlackHole and not SpectralType.None)
            {
                if (!system.SpectralType.HasFlag(SpectralType.Size_Dwarf))
                {
                    canvas.DrawLine(starPen,
                        (int)(starCordX - (starRadius+2) * maxRadiusScale),
                        starCordY,
                        (int)(starCordX + (starRadius + 2) * maxRadiusScale),
                        starCordY);
                    canvas.DrawLine(starPen,
                        starCordX,
                        (int)(starCordY - (starRadius + 2) * maxRadiusScale),
                        starCordX,
                        (int)(starCordY + (starRadius + 2) * maxRadiusScale));
                }
                canvas.DrawEllipse(
                    starPen,
                    (int)(starCordX - starRadius* maxRadiusScale),
                    (int)(starCordY - starRadius* maxRadiusScale),
                    (int)(starRadius*maxRadiusScale*2),
                    (int)(starRadius*maxRadiusScale*2));
            }
            else if (system.SpectralType is SpectralType.BlackHole)
            {
                canvas.FillEllipse(
                    Brushes.Black,
                    (int)(starCordX - starRadius* maxRadiusScale/2),
                    (int)(starCordY - starRadius * maxRadiusScale/2),
                    (int)(starRadius * maxRadiusScale),
                    (int)(starRadius * maxRadiusScale));

                starPen.Color = Color.White;
                canvas.DrawLine(starPen,
                    (int)(starCordX - starRadius * maxRadiusScale),
                    starCordY,
                    (int)(starCordX + starRadius * maxRadiusScale),
                    starCordY);
                canvas.DrawEllipse(
                    starPen,
                    (int)(starCordX - starRadius * maxRadiusScale / 2),
                    (int)(starCordY - starRadius * maxRadiusScale / 2),
                    (int)(starRadius * maxRadiusScale),
                    (int)(starRadius * maxRadiusScale));
            }
            else
            {
                canvas.DrawEllipse(
                    starPen,
                    (int)(starCordX - starRadius * maxRadiusScale),
                    (int)(starCordY - starRadius * maxRadiusScale),
                    (int)(starRadius * maxRadiusScale) * 2,
                    (int)(starRadius * maxRadiusScale) * 2);
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

    private static (int starCordX, int starCordY) GetStarPosition(StarSystem system, double imageWidth, double imageHeight, double scale)
    {
        var margin = 5 * scale;
        var starCordX = (int)(system.Coordinates.X*scale + imageWidth / 2 + margin);
        var starCordY = (int)(system.Coordinates.Y*scale + imageHeight / 2 + margin);
        return (starCordX, starCordY);
    }

    private static int GetStarRadius(StarSystem system)
    {
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

        return starRadius;
    }

    private static void GetImageDimensions(Galaxy galaxy, Axis VerticalAxis, Axis HorizontalAxis, out double minX,
        out double minY, out double imageWidth, out double imageHeight, double scale)
    {
        var max3dX = galaxy.StarSystems.Max(system => system.Coordinates.X);
        var min3dX = galaxy.StarSystems.Min(system => system.Coordinates.X);
        var max3dY = galaxy.StarSystems.Max(system => system.Coordinates.Y);
        var min3dY = galaxy.StarSystems.Min(system => system.Coordinates.Y);
        var max3dZ = galaxy.StarSystems.Max(system => system.Coordinates.Z);
        var min3dZ = galaxy.StarSystems.Min(system => system.Coordinates.Z);

        minX = 0d;
        minY = 0d;
        var maxX = 0d;
        var maxY = 0d;
        imageWidth = 0d;
        imageHeight = 0d;

        switch (VerticalAxis)
        {
            case Axis.X:
                minY = min3dX;
                maxY = max3dX;
                break;
            case Axis.Y:
                minY = min3dY;
                maxY = max3dY;
                break;
            case Axis.Z:
                minY = min3dZ;
                maxY = max3dZ;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (HorizontalAxis)
        {
            case Axis.X:
                minX = min3dX;
                maxX = max3dX;
                break;
            case Axis.Y:
                minX = min3dY;
                maxX = max3dY;
                break;
            case Axis.Z:
                minX = min3dZ;
                maxX = max3dZ;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        if (minX < 0)
        {
            imageWidth = maxX - minX + 10;
        }

        if (minY < 0)
        {
            imageHeight = maxY - minY + 10;
        }

        minY *= scale;
        minX *= scale;
        imageHeight *= scale;
        imageWidth *= scale;

    }

    public MemoryStream SideView(Galaxy galaxy, double scale)
    {
        throw new NotImplementedException();
    }
}