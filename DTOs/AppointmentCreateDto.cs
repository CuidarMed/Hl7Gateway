namespace Hl7Gateway.DTOs
{
    public class AppointmentCreateDto
    {
        public long DoctorId { get; set; }
        public long PatientId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string? Reason { get; set; }
    }

    public class AppointmentResponse
    {
        public long AppointmentId { get; set; }
        public long DoctorId { get; set; }
        public long PatientId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}

