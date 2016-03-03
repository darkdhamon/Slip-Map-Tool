using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

namespace StellarDomain.Model
{
  class Ship
  {
    public string Name { get; set; }
    public int Hull { get; set; }
    public int MaxHull { get; set; }
    public int BaseMass { get; set; }
    public int Energy { get; set; }
    public int MaxEnergy { get; set; }

    public Armor Armor { get; set; }
    public Sheild Sheild { get; set; }

    public List<IActionSystem> ActionSystems { get; set; }
    public List<Weapon> Weapons { get; set; }
  }

  internal class Weapon
  {
    public string Name { get; set; }
    public int DamageLow { get; set; }
    public int DamageHigh { get; set; }
    public int Reload { get; set; }
  }

  internal class Armor
  {
    public double MaxArmorValue { get; set; }
    public double ArmorValue { get; set; }
  }

  internal class Sheild : Armor, IActionSystem
  {
    public double RechargeRate { get; set; }

    public void Act()
    {
      RechargeShield();
    }

    private void RechargeShield()
    {
      if (ArmorValue < MaxArmorValue)
      {
        ArmorValue += RechargeRate;
      }
      if (ArmorValue > MaxArmorValue)
      {
        ArmorValue = MaxArmorValue;
      }
    }
  }

  internal interface IActionSystem
  {
    void Act();
  }
}
