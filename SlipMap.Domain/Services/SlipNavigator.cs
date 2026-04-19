using SlipMap.Domain.Exceptions;
using SlipMap.Domain.Model.Entity;
using SlipMap.Domain.Model.ViewModel;
using SlipMap.Domain.Services.Abstract;
using SlipMapModel = SlipMap.Domain.Model.Entity.SlipMap;

namespace SlipMap.Domain.Services;

public sealed class SlipNavigator
{
    private readonly IDiceRoller _diceRoller;
    private readonly IRandomNumberGenerator _random;

    public SlipNavigator(IDiceRoller? diceRoller = null, IRandomNumberGenerator? random = null)
    {
        _random = random ?? new SystemRandomNumberGenerator();
        _diceRoller = diceRoller ?? new RandomDiceRoller(_random);
    }

    public NavigationJumpResult BlindJump(SlipMapModel map, StarShip ship, bool computerNavigation, int pilotSkillLevel)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(ship);

        if (ship.CurrentSystemId is null)
        {
            ship.MoveTo(map.CurrentSystemId);
        }

        return computerNavigation
            ? ExecuteBlindJump(map, ship, computerNavigation, null, null)
            : ResolveOrganicBlindJump(map, ship, pilotSkillLevel);
    }

    public NavigationJumpResult BlindJump(SlipMapModel map, StarShip ship, Pilot pilot)
    {
        ArgumentNullException.ThrowIfNull(pilot);
        return BlindJump(map, ship, computerNavigation: false, pilot.SkillLevel);
    }

    public NavigationJumpResult NavigateKnownRoute(SlipMapModel map, StarShip ship, int destinationSystemId, bool computerNavigation, int pilotSkillLevel)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(ship);

        var originSystemId = ship.CurrentSystemId ?? map.CurrentSystemId;
        if (!ship.KnowsRoute(originSystemId, destinationSystemId))
        {
            throw new RouteNotFoundException(originSystemId, destinationSystemId);
        }

        return computerNavigation
            ? ResolveComputerNavigationJump(map, ship, originSystemId, destinationSystemId)
            : ResolveOrganicNavigationJump(map, ship, originSystemId, destinationSystemId, pilotSkillLevel);
    }

    public NavigationJumpResult NavigateKnownRoute(SlipMapModel map, StarShip ship, Pilot pilot, int destinationSystemId)
    {
        ArgumentNullException.ThrowIfNull(pilot);
        return NavigateKnownRoute(map, ship, destinationSystemId, computerNavigation: false, pilot.SkillLevel);
    }

    private NavigationJumpResult ResolveOrganicBlindJump(SlipMapModel map, StarShip ship, int pilotSkillLevel)
    {
        var originSystemId = ship.CurrentSystemId ?? map.CurrentSystemId;
        var skillRoll = _diceRoller.Roll(6, 3);
        var message = $"Rolled {skillRoll.Total} vs. {pilotSkillLevel} with dice {string.Join(", ", skillRoll.Dice)}.";

        if (skillRoll.Total == 18 || skillRoll.Total - pilotSkillLevel > 9)
        {
            return new NavigationJumpResult(
                NavigationJumpOutcome.CriticalFailure,
                originSystemId,
                originSystemId,
                originSystemId,
                "Critical failure: " + message + " The ship did not move and the grav lens is damaged.",
                skillRoll);
        }

        if (skillRoll.Total == 17)
        {
            return new NavigationJumpResult(
                NavigationJumpOutcome.Failed,
                originSystemId,
                originSystemId,
                originSystemId,
                "Automatic failure: " + message + " The slip portal failed to open.",
                skillRoll);
        }

        var timeRoll = _diceRoller.Roll(6, 3);
        var hours = Math.Max(0, timeRoll.Total - (pilotSkillLevel - skillRoll.Total));
        var result = ExecuteBlindJump(map, ship, computerNavigation: false, skillRoll, hours);

        var pilotMessage = skillRoll.Total > pilotSkillLevel
            ? "Blind jump succeeded, but the pilot was knocked unconscious or offline."
            : "Blind jump succeeded.";

        return result with
        {
            Message = $"{message} {pilotMessage} It took {hours} hours.",
            TravelHours = hours
        };
    }

    private NavigationJumpResult ResolveOrganicNavigationJump(SlipMapModel map, StarShip ship, int originSystemId, int destinationSystemId, int pilotSkillLevel)
    {
        var skillRoll = _diceRoller.Roll(6, 3);
        var message = $"Rolled {skillRoll.Total} vs. {pilotSkillLevel} with dice {string.Join(", ", skillRoll.Dice)}.";

        if (skillRoll.Total == 3 || pilotSkillLevel - skillRoll.Total > 9)
        {
            return ArriveAtDestination(map, ship, originSystemId, destinationSystemId, "Critical success: " + message);
        }

        if (skillRoll.Total == 18 || skillRoll.Total - pilotSkillLevel > 9)
        {
            return new NavigationJumpResult(
                NavigationJumpOutcome.CriticalFailure,
                originSystemId,
                destinationSystemId,
                originSystemId,
                "Critical failure: " + message + " The ship did not move and the grav lens is damaged.",
                skillRoll);
        }

        if (skillRoll.Total == 17)
        {
            return new NavigationJumpResult(
                NavigationJumpOutcome.Failed,
                originSystemId,
                destinationSystemId,
                originSystemId,
                "Automatic failure: " + message + " The slip portal failed to open.",
                skillRoll);
        }

        if (skillRoll.Total == 4)
        {
            return ResolveNavigationSuccess(map, ship, originSystemId, destinationSystemId, skillRoll, message, "Automatic success", blindJumpChancePercent: 5);
        }

        if (skillRoll.Total < pilotSkillLevel)
        {
            return ResolveNavigationSuccess(map, ship, originSystemId, destinationSystemId, skillRoll, message, "Success", blindJumpChancePercent: 10);
        }

        return ResolveNavigationSuccess(map, ship, originSystemId, destinationSystemId, skillRoll, message, "Failure", blindJumpChancePercent: 50);
    }

    private NavigationJumpResult ResolveNavigationSuccess(
        SlipMapModel map,
        StarShip ship,
        int originSystemId,
        int destinationSystemId,
        DiceRoll skillRoll,
        string rollMessage,
        string successType,
        int blindJumpChancePercent)
    {
        if (_random.Next(0, 100) >= blindJumpChancePercent)
        {
            return ArriveAtDestination(
                map,
                ship,
                originSystemId,
                destinationSystemId,
                $"{successType}: {rollMessage} The ship arrived at the desired system.") with
            {
                SkillRoll = skillRoll
            };
        }

        var blindJump = ExecuteBlindJump(map, ship, computerNavigation: false, skillRoll, null);
        return blindJump with
        {
            Outcome = NavigationJumpOutcome.BlindJumped,
            DestinationSystemId = destinationSystemId,
            Message = $"{successType}: {rollMessage} The ship was pulled into a blind jump instead. {blindJump.Message}",
            SkillRoll = skillRoll
        };
    }

    private NavigationJumpResult ResolveComputerNavigationJump(SlipMapModel map, StarShip ship, int originSystemId, int destinationSystemId)
    {
        if (_random.Next(0, 2) == 0)
        {
            var blindJump = ExecuteBlindJump(map, ship, computerNavigation: true, null, null);
            return blindJump with
            {
                Outcome = NavigationJumpOutcome.BlindJumped,
                DestinationSystemId = destinationSystemId,
                Message = "The computer lost its way in the slipstream network. " + blindJump.Message
            };
        }

        return ArriveAtDestination(map, ship, originSystemId, destinationSystemId, "The computer navigated to the desired system.");
    }

    private NavigationJumpResult ExecuteBlindJump(SlipMapModel map, StarShip ship, bool computerNavigation, DiceRoll? skillRoll, int? travelHours)
    {
        if (map.TotalSystemCount < 2)
        {
            throw new InvalidOperationException("A blind jump requires at least two systems in the sector.");
        }

        var originSystemId = ship.CurrentSystemId ?? map.CurrentSystemId;
        var destinationSystemId = PickBlindJumpDestination(map, originSystemId, computerNavigation);

        map.TryAddRoute(originSystemId, destinationSystemId, out var route);
        map.SetCurrentSystem(destinationSystemId);
        ship.MoveTo(destinationSystemId);
        ship.LearnRoute(route);

        return new NavigationJumpResult(
            NavigationJumpOutcome.BlindJumped,
            originSystemId,
            destinationSystemId,
            destinationSystemId,
            $"Blind jump completed from system {originSystemId} to system {destinationSystemId}.",
            skillRoll,
            travelHours);
    }

    private int PickBlindJumpDestination(SlipMapModel map, int originSystemId, bool computerNavigation)
    {
        while (true)
        {
            if (computerNavigation || _random.Next(0, 3) != 0 || map.VisitedSystems.Count == 1)
            {
                var randomSystemId = _random.Next(0, map.TotalSystemCount);
                if (randomSystemId != originSystemId)
                {
                    return randomSystemId;
                }
            }
            else
            {
                var visitedSystems = map.VisitedSystems.Where(system => system.Id != originSystemId).ToList();
                if (visitedSystems.Count > 0)
                {
                    return visitedSystems[_random.Next(0, visitedSystems.Count)].Id;
                }
            }
        }
    }

    private static NavigationJumpResult ArriveAtDestination(SlipMapModel map, StarShip ship, int originSystemId, int destinationSystemId, string message)
    {
        map.SetCurrentSystem(destinationSystemId);
        ship.MoveTo(destinationSystemId);

        return new NavigationJumpResult(
            NavigationJumpOutcome.Arrived,
            originSystemId,
            destinationSystemId,
            destinationSystemId,
            message);
    }
}
