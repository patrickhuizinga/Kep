namespace Kep.Runner.Problem;

public enum BloodType
{
    O,
    A,
    B,
    AB,
}

public static class BloodTypeExtensions
{
    public static bool CanDonateTo(this BloodType giver, BloodType receiver)
    {
        // O can donate to {O,A,B,AB}
        // AB can receive from {O,A,B,AB}
        // A gives to {A,AB}, B gives to {B,AB}
        
        return giver == BloodType.O
            || receiver == BloodType.AB
            || giver == receiver;
    }
}