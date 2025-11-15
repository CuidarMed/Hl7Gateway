using Hl7Gateway.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Hl7Gateway.Services
{
    public class DirectoryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DirectoryService> _logger;

        public DirectoryService(HttpClient httpClient, ILogger<DirectoryService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<long?> UpsertPatientAsync(PatientUpsertDto patient)
        {
            try
            {
                _logger.LogInformation("Intentando upsert de paciente: DNI={Dni}, Nombre={FirstName} {LastName}",
                    patient.Dni, patient.FirstName, patient.LastName);

                // Buscar paciente existente por DNI
                var existingPatient = await FindPatientByDniAsync(patient.Dni);
                
                if (existingPatient != null)
                {
                    _logger.LogInformation("Paciente existente encontrado, ID: {PatientId}, actualizando...", existingPatient.PatientId);
                    
                    // Actualizar paciente existente
                    var updateRequest = new
                    {
                        Name = patient.FirstName,
                        LastName = patient.LastName,
                        Dni = patient.Dni,
                        Adress = patient.Adress,
                        Phone = patient.Phone,
                        DateOfBirth = patient.DateOfBirth,
                        HealthPlan = patient.HealthPlan,
                        MembershipNumber = patient.MembershipNumber
                    };

                    var updateResponse = await _httpClient.PutAsJsonAsync(
                        $"/api/v1/Patient/{existingPatient.PatientId}", 
                        updateRequest);

                    if (updateResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Paciente actualizado exitosamente, ID: {PatientId}", existingPatient.PatientId);
                        return existingPatient.PatientId;
                    }
                    else
                    {
                        var errorContent = await updateResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Error actualizando paciente: {StatusCode} - {Error}", 
                            updateResponse.StatusCode, errorContent);
                        return null;
                    }
                }
                else
                {
                    _logger.LogInformation("Paciente no existe, creando nuevo...");
                    
                    // Crear nuevo paciente
                    // Nota: UserId es requerido, usaremos 0 como placeholder si no viene
                    var createRequest = new
                    {
                        Dni = patient.Dni,
                        FirstName = patient.FirstName,
                        LastName = patient.LastName,
                        Adress = patient.Adress,
                        Phone = patient.Phone,
                        DateOfBirth = patient.DateOfBirth,
                        HealthPlan = patient.HealthPlan ?? "Pendiente",
                        MembershipNumber = patient.MembershipNumber ?? $"HL7-{patient.Dni}",
                        UserId = patient.UserId ?? 0 // Placeholder, idealmente debería venir del sistema
                    };

                    var createResponse = await _httpClient.PostAsJsonAsync("/api/v1/Patient", createRequest);

                    if (createResponse.IsSuccessStatusCode)
                    {
                        var createdPatient = await createResponse.Content.ReadFromJsonAsync<PatientResponse>();
                        _logger.LogInformation("Paciente creado exitosamente, ID: {PatientId}", createdPatient?.PatientId);
                        return createdPatient?.PatientId;
                    }
                    else
                    {
                        var errorContent = await createResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Error creando paciente: {StatusCode} - {Error}", 
                            createResponse.StatusCode, errorContent);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción en UpsertPatientAsync");
                return null;
            }
        }

        public async Task<long?> UpsertPractitionerAsync(PractitionerUpsertDto practitioner)
        {
            try
            {
                _logger.LogInformation("Intentando upsert de practitioner: Nombre={FirstName} {LastName}, License={LicenseNumber}",
                    practitioner.FirstName, practitioner.LastName, practitioner.LicenseNumber);

                // Buscar practitioner existente por LicenseNumber
                var existingPractitioner = await FindPractitionerByLicenseAsync(practitioner.LicenseNumber);
                
                if (existingPractitioner != null)
                {
                    _logger.LogInformation("Practitioner existente encontrado, ID: {DoctorId}, actualizando...", existingPractitioner.DoctorId);
                    
                    // Actualizar practitioner existente
                    var updateRequest = new
                    {
                        FirstName = practitioner.FirstName,
                        LastName = practitioner.LastName,
                        LicenseNumber = practitioner.LicenseNumber,
                        Biography = practitioner.Biography,
                        Specialty = practitioner.Specialty ?? "Clinico",
                        Phone = practitioner.Phone
                    };

                    var updateResponse = await _httpClient.PutAsJsonAsync(
                        $"/api/v1/Doctor/{existingPractitioner.DoctorId}", 
                        updateRequest);

                    if (updateResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Practitioner actualizado exitosamente, ID: {DoctorId}", existingPractitioner.DoctorId);
                        return existingPractitioner.DoctorId;
                    }
                    else
                    {
                        var errorContent = await updateResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Error actualizando practitioner: {StatusCode} - {Error}", 
                            updateResponse.StatusCode, errorContent);
                        return null;
                    }
                }
                else
                {
                    _logger.LogInformation("Practitioner no existe, creando nuevo...");
                    
                    // Crear nuevo practitioner
                    var createRequest = new
                    {
                        FirstName = practitioner.FirstName,
                        LastName = practitioner.LastName,
                        LicenseNumber = practitioner.LicenseNumber ?? "PENDING",
                        Biography = practitioner.Biography,
                        Specialty = practitioner.Specialty ?? "Clinico",
                        Phone = practitioner.Phone,
                        UserId = practitioner.UserId ?? 0 // Placeholder
                    };

                    var createResponse = await _httpClient.PostAsJsonAsync("/api/v1/Doctor", createRequest);

                    if (createResponse.IsSuccessStatusCode)
                    {
                        var createdPractitioner = await createResponse.Content.ReadFromJsonAsync<DoctorResponse>();
                        _logger.LogInformation("Practitioner creado exitosamente, ID: {DoctorId}", createdPractitioner?.DoctorId);
                        return createdPractitioner?.DoctorId;
                    }
                    else
                    {
                        var errorContent = await createResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Error creando practitioner: {StatusCode} - {Error}", 
                            createResponse.StatusCode, errorContent);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción en UpsertPractitionerAsync");
                return null;
            }
        }

        private async Task<PatientResponse?> FindPatientByDniAsync(int? dni)
        {
            if (dni == null || dni == 0)
                return null;

            try
            {
                // Buscar todos los pacientes y filtrar por DNI
                // Nota: Esto no es óptimo, pero el API actual no tiene búsqueda por DNI
                var response = await _httpClient.GetAsync("/api/v1/Patient/all");
                if (response.IsSuccessStatusCode)
                {
                    var patients = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
                    return patients?.FirstOrDefault(p => p.Dni == dni);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error buscando paciente por DNI: {Dni}", dni);
            }

            return null;
        }

        private async Task<DoctorResponse?> FindPractitionerByLicenseAsync(string? licenseNumber)
        {
            if (string.IsNullOrEmpty(licenseNumber))
                return null;

            try
            {
                // Buscar todos los doctores y filtrar por LicenseNumber
                var response = await _httpClient.GetAsync("/api/v1/Doctor");
                if (response.IsSuccessStatusCode)
                {
                    var doctors = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();
                    return doctors?.FirstOrDefault(d => d.LicenseNumber == licenseNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error buscando practitioner por LicenseNumber: {LicenseNumber}", licenseNumber);
            }

            return null;
        }
    }
}

