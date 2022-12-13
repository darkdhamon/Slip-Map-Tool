using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlipMap.Model.Enums;

namespace SlipMap.Domain.Generators.Images
{
    public static class MapColorVariables
    {
        public static Color ColorFromSpectralType(SpectralType spectralType)
        {
            var color = new Color();
            switch (spectralType)
            {
                case SpectralType.O_Main:
                    color = Color.FromArgb(255, 207, 255, 255);
                    break;
                case SpectralType.O_Super:
                    color = Color.FromArgb(255, 207, 255, 255);
                    break;
                case SpectralType.B_Main:
                    color = Color.FromArgb(255, 225, 255, 255);
                    break;
                case SpectralType.B_Super:
                    color = Color.FromArgb(255, 225, 255, 255);
                    break;
                case SpectralType.A_Main:
                    color = Color.FromArgb(255, 255, 255, 255);
                    break;
                case SpectralType.A_Super:
                    color = Color.FromArgb(255, 255, 255, 255);
                    break;
                case SpectralType.F_Main:
                    color = Color.FromArgb(255, 255, 255, 204);
                    break;
                case SpectralType.F_Super:
                    color = Color.FromArgb(255, 255, 255, 204);
                    break;
                case SpectralType.G_Main:
                    color = Color.FromArgb(255, 250, 249, 105);
                    break;
                case SpectralType.G_Giant:
                    color = Color.FromArgb(255, 250, 249, 105);
                    break;
                case SpectralType.G_Super:
                    color = Color.FromArgb(255, 250, 249, 105);
                    break;
                case SpectralType.K_Main:
                    color = Color.FromArgb(255, 255, 120, 5);
                    break;
                case SpectralType.K_Giant:
                    color = Color.FromArgb(255, 255, 120, 5);
                    break;
                case SpectralType.K_Super:
                    color = Color.FromArgb(255, 255, 120, 5);
                    break;
                case SpectralType.M_Main:
                    color = Color.FromArgb(255, 255, 46, 0);
                    break;
                case SpectralType.M_Giant:
                    color = Color.FromArgb(255, 255, 46, 0);
                    break;
                case SpectralType.M_Super:
                    color = Color.FromArgb(255, 255, 46, 0);
                    break;
                case SpectralType.WhiteDwarf:
                    color = Color.FromArgb(255, 255, 255, 255);
                    break;
                case SpectralType.BlackHole:
                    color = Color.FromArgb(255, 255, 0, 100);
                    break;
                case SpectralType.NeutronStar:
                    color = Color.FromArgb(255, 100, 0, 255);
                    break;
                case SpectralType.ProtoStar:
                    color = Color.FromArgb(255, 0, 200, 0);
                    break;
                case SpectralType.BrownDwarf:
                    color = Color.FromArgb(255, 255, 150, 100);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spectralType), spectralType, null);
            }

            return color;
        }
    }
}
