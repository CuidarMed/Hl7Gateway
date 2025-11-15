using Hl7Gateway.DTOs;
using NHapi.Model.V23.Segment;

namespace Hl7Gateway.Mappers
{
    public static class Hl7ToDtoMapper
    {
        public static PatientUpsertDto MapPidToPatientUpsert(PID pid)
        {
            var dto = new PatientUpsertDto();

            // PID-3: Patient Identifier List (DNI/Historia)
            try
            {
                var identifier = pid.GetPatientIdentifierList(0);
                if (identifier?.IDNumber?.Value != null)
                {
                    if (int.TryParse(identifier.IDNumber.Value, out var dni))
                    {
                        dto.Dni = dni;
                    }
                }
            }
            catch { }

            // PID-5: Patient Name
            try
            {
                var patientName = pid.GetPatientName(0);
                if (patientName != null)
                {
                    dto.FirstName = patientName.GivenName?.Value;
                    dto.LastName = patientName.FamilyName?.Value;
                }
            }
            catch { }

            // PID-7: Date/Time of Birth
            try
            {
                var dateTimeOfBirth = pid.GetDateTimeOfBirth();
                if (dateTimeOfBirth?.TimeOfAnEvent?.Value != null)
                {
                    var birthDateStr = dateTimeOfBirth.TimeOfAnEvent.Value;
                    if (birthDateStr.Length >= 8)
                    {
                        var year = int.Parse(birthDateStr.Substring(0, 4));
                        var month = int.Parse(birthDateStr.Substring(4, 2));
                        var day = int.Parse(birthDateStr.Substring(6, 2));
                        dto.DateOfBirth = new DateOnly(year, month, day);
                    }
                }
            }
            catch { }

            // PID-13: Phone Number - Home
            try
            {
                var phone = pid.GetPhoneNumberHome(0);
                if (phone?.TelephoneNumber?.Value != null)
                {
                    dto.Phone = phone.TelephoneNumber.Value;
                }
            }
            catch { }

            // PID-11: Patient Address (opcional)
            try
            {
                var address = pid.GetPatientAddress(0);
                if (address != null)
                {
                    var addressParts = new List<string>();
                    if (!string.IsNullOrEmpty(address.StreetAddress?.Value))
                        addressParts.Add(address.StreetAddress.Value);
                    if (!string.IsNullOrEmpty(address.City?.Value))
                        addressParts.Add(address.City.Value);
                    if (!string.IsNullOrEmpty(address.StateOrProvince?.Value))
                        addressParts.Add(address.StateOrProvince.Value);
                    
                    dto.Adress = string.Join(", ", addressParts);
                }
            }
            catch { }

            return dto;
        }

        public static AppointmentCreateDto MapOrcObrToAppointmentCreate(
            ORC orc, 
            OBR obr, 
            long patientId)
        {
            var dto = new AppointmentCreateDto
            {
                PatientId = patientId
            };

            // OBR-4: Universal Service ID (código de estudio)
            try
            {
                var universalServiceId = obr.GetUniversalServiceID();
                var serviceId = universalServiceId?.Identifier?.Value;
                if (!string.IsNullOrEmpty(serviceId))
                {
                    dto.Reason = $"Estudio: {serviceId}";
                }
            }
            catch { }

            // OBR-7: Observation Date/Time (fecha/hora del estudio)
            try
            {
                var observationDateTime = obr.GetObservationDateTime();
                var dateTimeStr = observationDateTime?.TimeOfAnEvent?.Value;
                if (!string.IsNullOrEmpty(dateTimeStr) && dateTimeStr.Length >= 14)
                {
                    var year = int.Parse(dateTimeStr.Substring(0, 4));
                    var month = int.Parse(dateTimeStr.Substring(4, 2));
                    var day = int.Parse(dateTimeStr.Substring(6, 2));
                    var hour = int.Parse(dateTimeStr.Substring(8, 2));
                    var minute = int.Parse(dateTimeStr.Substring(10, 2));
                    var second = int.Parse(dateTimeStr.Substring(12, 2));

                    var startTime = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
                    dto.StartTime = startTime;
                    dto.EndTime = startTime.AddHours(1);
                }
                else
                {
                    var defaultTime = DateTimeOffset.UtcNow.AddDays(1);
                    dto.StartTime = defaultTime;
                    dto.EndTime = defaultTime.AddHours(1);
                }
            }
            catch
            {
                var defaultTime = DateTimeOffset.UtcNow.AddDays(1);
                dto.StartTime = defaultTime;
                dto.EndTime = defaultTime.AddHours(1);
            }

            // DoctorId: usar 1 como placeholder (debería venir del sistema o ORC-12)
            dto.DoctorId = 1;

            return dto;
        }
    }
}
