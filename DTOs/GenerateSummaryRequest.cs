namespace Hl7Gateway.DTOs
{
    public class GenerateSummaryRequest
    {
        public int EncounterId { get; set; }
        public long PatientId { get; set; }
        public long DoctorId { get; set; }
        public long AppointmentId { get; set; }
        public string? PatientDni { get; set; }
        public string? PatientFirstName { get; set; }
        public string? PatientLastName { get; set; }
        public DateTime? PatientDateOfBirth { get; set; }
        public string? PatientPhone { get; set; }
        public string? PatientAddress { get; set; }
        public string? DoctorFirstName { get; set; }
        public string? DoctorLastName { get; set; }
        public string? DoctorSpecialty { get; set; }
        public DateTime? AppointmentStartTime { get; set; }
        public DateTime? AppointmentEndTime { get; set; }
        public string? AppointmentReason { get; set; }
        public string? EncounterReasons { get; set; }
        public string? EncounterAssessment { get; set; }
        public DateTime EncounterDate { get; set; }
    }
}

