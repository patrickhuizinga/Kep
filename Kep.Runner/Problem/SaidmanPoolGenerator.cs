namespace Kep.Runner.Problem;

// https://github.com/JohnDickerson/KidneyExchange/blob/master/src/edu/cmu/cs/dickerson/kpd/structure/generator/PoolGenerator.java
// 

/// <summary>
/// Compatibility graph generator based on the following paper:
/// <i>Increasing the Opportunity of Live Kidney Donation by Matching for Two and Three Way Exchanges.</i>
/// S. L. Saidman, Alvin Roth, Tayfun Sonmez, Utku Unver, Frank Delmonico.
/// <b>Transplantation</b>.  Volume 81, Number 5, March 15, 2006.
/// 
/// This is known colloquially as the "Saidman Generator".
///
/// original author John P. Dickerson
/// converted to C# by Patrick Huizinga 
/// </summary>
public class SaidmanPoolGenerator(Random rng)
{
	// Numbers taken from Saidman et al.'s 2006 paper "Increasing
	// the Opportunity of Live Kidney Donation..."
	private const double PrFemale = 0.4090;

	private const double PrSpousalDonor = 0.4897;

	private const double PrLowPra = 0.7019;
	private const double PrMedPra = 0.2;

	private const double PrLowPraIncompatibility = 0.05;
	private const double PrMedPraIncompatibility = 0.45;
	private const double PrHighPraIncompatibility = 0.90;

	public const double PrSpousalPraCompatibility = 0.75;

	private const double PrBloodTypeO = 0.4814;
	private const double PrBloodTypeA = 0.3373;
	private const double PrBloodTypeB = 0.1428;

	/// <summary>
	/// Draws a random blood type from the US distribution.
	/// </summary>
	private BloodType DrawBloodType()
	{
		double r = rng.NextDouble();

		switch (r)
		{
			case <= PrBloodTypeO:
				return BloodType.O;
			case <= PrBloodTypeO + PrBloodTypeA:
				return BloodType.A;
			case <= PrBloodTypeO + PrBloodTypeA + PrBloodTypeB:
				return BloodType.B;
			default:
				return BloodType.AB;
		}
	}

	/// <summary>
	/// Draws a random gender from the US waitlist distribution
	/// </summary>
	private bool DrawIsPatientFemale() => rng.NextDouble() <= PrFemale;

	/// <summary>
	/// Draws a random spousal relationship between donor and patient
	/// </summary>
	private bool DrawIsDonorSpouse() => rng.NextDouble() <= PrSpousalDonor;

	/// <summary>
	/// Random roll to see if a patient and donor are crossmatch compatible
	/// </summary>
	/// <param name="prPraIncompatibility">probability of a PRA-based incompatibility</param>
	/// <returns>true if simulated positive crossmatch, false otherwise</returns>
	private bool DrawIsPositiveCrossmatch(double prPraIncompatibility)
	{
		return rng.NextDouble() <= prPraIncompatibility;
	}

	private double DrawPraIncompatibility()
	{
		double r = rng.NextDouble();
		switch (r)
		{
			case <= PrLowPra:
				return PrLowPraIncompatibility;
			case <= PrLowPra + PrMedPra:
				return PrMedPraIncompatibility;
			default:
				return PrHighPraIncompatibility;
		}
	}

	/// <summary>
	/// Randomly rolls a patient/donor pair (possibly compatible or incompatible)
	/// </summary>
	/// <returns>A patient/donor pair</returns>
	private PatientDonorPair GeneratePair()
	{
		// Draw blood types for patient and donor, along with spousal details and probability of PositiveXM
		var bloodTypePatient = DrawBloodType();
		var bloodTypeDonor = DrawBloodType();
		var isWifePatient = DrawIsPatientFemale() && DrawIsDonorSpouse();
		var patientCpra = DrawPraIncompatibility();

		return new PatientDonorPair(bloodTypePatient, bloodTypeDonor, isWifePatient, patientCpra);
	}
	
	public bool DrawIsCompatible(PatientDonorPair donor, PatientDonorPair patient)
	{
		return DrawIsCompatible(donor, patient, patient.PatientCpra);
	}

	private bool DrawIsCompatible(PatientDonorPair donor, PatientDonorPair patient, double patientCpra)
	{
		// Donor must at least be blood type compatible with patient
		return donor.CanDonateTo(patient)
			&& !DrawIsPositiveCrossmatch(patientCpra);
	}

	public bool DrawIsCompatible(PatientDonorPair pair)
	{
		return DrawIsCompatible(pair, pair, pair.PairCpra);
	}

	/// <summary>
	/// Generates a pool with <paramref name="n"/> patient/donor pairs
	/// </summary>
	/// <param name="n">The number of patient/donor pairs to generate</param>
	public Pool GeneratePool(int n)
	{
		var pairs = new List<PatientDonorPair>();
		
		// Generate an incompatible patient/donor pair
		while (pairs.Count < n)
		{
			var pair = GeneratePair();
			var isPairCompatible = DrawIsCompatible(pair);
			if (!isPairCompatible)
				pairs.Add(pair);
		}

		return new Pool(pairs);
	}

	/// <summary>
	/// Generates a compatibility graph with <paramref name="n"/> patient/donor pairs
	/// </summary>
	/// <param name="n">The number of patient/donor pairs to generate</param>
	/// <returns>a <paramref name="n"/> x <paramref name="n"/> compatibility matrix.</returns>
	public bool[,] Generate(int n)
	{
		bool[,] A = new bool[n, n];

		var p = GeneratePool(n);
		
		// Add edges between compatible donors and other patients
		for (var u = 0; u < n; u++)
		{
			for (var v = 0; v < u; v++)
			{
				A[u, v] = DrawIsCompatible(p.Pairs[u], p.Pairs[v]);
				A[v, u] = DrawIsCompatible(p.Pairs[v], p.Pairs[u]);
			}
		}

		return A;
	}
}