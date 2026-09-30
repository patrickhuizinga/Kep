namespace Kep.Runner.Problem;

public record PatientDonorPair(
    BloodType BloodTypePatient,
    BloodType BloodTypeDonor,
    bool IsWifePatient,
    double PatientCpra)
{
    /// <summary>
    /// Returns the pair CPRA, which will be increased if the patient is the wife of the donor.
    /// </summary>
    public double PairCpra =>
        IsWifePatient
            ? 1.0 - SaidmanPoolGenerator.PrSpousalPraCompatibility*(1.0 - PatientCpra)
            : PatientCpra;
    
    public bool CanDonateTo(PatientDonorPair patient) => BloodTypeDonor.CanDonateTo(patient.BloodTypePatient);
}
