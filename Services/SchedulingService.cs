using Hl7Gateway.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Hl7Gateway.Services
{
    public class SchedulingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SchedulingService> _logger;

        public SchedulingService(HttpClient httpClient, ILogger<SchedulingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<long?> CreateAppointmentFromOrderAsync(AppointmentCreateDto appointment)
        {
            try
            {
                _logger.LogInformation("Creando appointment desde orden: PatientId={PatientId}, DoctorId={DoctorId}, StartTime={StartTime}",
                    appointment.PatientId, appointment.DoctorId, appointment.StartTime);

                var request = new
                {
                    DoctorId = appointment.DoctorId,
                    PatientId = appointment.PatientId,
                    StartTime = appointment.StartTime,
                    EndTime = appointment.EndTime,
                    Reason = appointment.Reason
                };

                var response = await _httpClient.PostAsJsonAsync("/api/v1/appointments", request);

                if (response.IsSuccessStatusCode)
                {
                    var createdAppointment = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
                    _logger.LogInformation("Appointment creado exitosamente, ID: {AppointmentId}", createdAppointment?.AppointmentId);
                    return createdAppointment?.AppointmentId;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error creando appointment: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción en CreateAppointmentFromOrderAsync");
                return null;
            }
        }
    }
}

