namespace SlipMap.Model.MapElements;

/// <summary>
/// Roll 1D10,000,000
/// </summary>
[Flags]
public enum SpectralType
{
    None = 0,
    Size_MainSequence = 1,
    Size_GiantStar = 2,
    Size_SuperGiantStar = 4,
    Size_Dwarf = 8,
    Special = 16,
    Color_DarkBlue = 32,
    Color_Blue = 64,
    Color_BlueWhite = 128,
    Color_White = 256,
    Color_Yellow = 512,
    Color_Orange = 1024,
    Color_Red = 2048,
    // RGBA 207,255, 255,
    O_Main = Color_DarkBlue|Size_MainSequence,
    O_Super = Color_DarkBlue|Size_SuperGiantStar,
    // RGBA 225,255,255
    B_Main = Color_Blue|Size_MainSequence,
    B_Super = Color_Blue| Size_SuperGiantStar,
    // RGBA 255,255,255
    A_Main = Color_BlueWhite|Size_MainSequence,
    A_Super = Color_BlueWhite|Size_SuperGiantStar,
    // RGBA 255,255,204
    F_Main = Color_White| Size_MainSequence,
    F_Super = Color_White|Size_SuperGiantStar,
    // RGBA 250,249,105
    G_Main = Color_Yellow|Size_MainSequence,
    G_Giant = Color_Yellow| Size_GiantStar,
    G_Super = Color_Yellow| Size_SuperGiantStar,
    // RGBA 255,120,5
    K_Main = Color_Orange|Size_MainSequence,
    K_Giant = Color_Orange|Size_GiantStar,
    K_Super = Color_Orange|Size_SuperGiantStar,
    // RGBA 255,46,0
    M_Main = Color_Red|Size_MainSequence,
    M_Giant = Color_Red|Size_GiantStar,
    M_Super = Color_Red|Size_SuperGiantStar,
    // rgbA 255,255,255
    WhiteDwarf = Color_White|Size_Dwarf,
    // RGBA 0,0,0
    BlackHole = Size_SuperGiantStar|Special,
    // RGBA 255,0,255
    NeutronStar = Size_MainSequence|Special,
    // RGBA 100,100,100
    ProtoStar = Color_Orange|Size_Dwarf|Special,
    // RGBA 100,100,100
    BrownDwarf = Color_Red|Size_Dwarf,

}