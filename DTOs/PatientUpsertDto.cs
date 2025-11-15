namespace Hl7Gateway.DTOs
{
    public class PatientUpsertDto
    {
        public int? Dni { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Adress { get; set; }
        public string? Phone { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? HealthPlan { get; set; }
        public string? MembershipNumber { get; set; }
        public long? UserId { get; set; }
    }

    public class PatientResponse
    {
        public long PatientId { get; set; }
        public int? Dni { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}

