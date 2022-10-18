namespace SlipMap.Domain.BusinessRules
{
    public static class DiceRoller
    {
        static readonly Random Gen = Random.Shared;

        public static int RollNSidedDie(int numSides)
        {
            
            return Gen.Next(1, numSides + 1);
        }

        public static bool CoinToss()
        {
            return RollNSidedDie(2)==1;
        }

        public static int RollD4()
        {
            return RollNSidedDie(4);
        }

        public static int RollD6()
        {
            return RollNSidedDie(6);
        }

        public static int RollD8()
        {
            return RollNSidedDie(8);
        }
        public static int RollD10()
        {
            return RollNSidedDie(10);
        }
        public static int Roll12()
        {
            return RollNSidedDie(12);
        }
        public static int RollD20()
        {
            return RollNSidedDie(20);
        }
        public static int RollPercentile()
        {
            return RollNSidedDie(100);
        }

        public static int Roll3D6()
        {
            return RollD6() + RollD6() + RollD6();
        }
    }
}
