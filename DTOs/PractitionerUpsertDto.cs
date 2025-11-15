namespace Hl7Gateway.DTOs
{
    public class PractitionerUpsertDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Biography { get; set; }
        public string? Specialty { get; set; }
        public string? Phone { get; set; }
        public long? UserId { get; set; }
    }

    public class DoctorResponse
    {
        public long DoctorId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? LicenseNumber { get; set; }
    }
}

