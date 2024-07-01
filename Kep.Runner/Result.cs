namespace Kep.Runner;

/// <summary>
/// Represents the result of a single instance of solving the KEP.
/// </summary>
public record Result(double Objective, TimeSpan SetupTime, TimeSpan RunningTime, double ObjectiveGap);