using SlipMap.Model.MapElements;

namespace SlipMap.Domain.BusinessRules;

public class JumpDriveLogic
{
    public JumpDriveLogic(StarShip starShip)
    {
        StarShip = starShip;
    }

    public StarShip StarShip { get; set; }

    public void BlindJump(bool IsBlindComputerNavigationJump)
    {
        var origin = StarShip.CurrentStarSystem;
        var destination = origin;
        while (origin == destination)
        {
            if (IsBlindComputerNavigationJump)
            {
                throw new NotImplementedException("Implement Blind Jump Logic");
            }
        }
    }
}