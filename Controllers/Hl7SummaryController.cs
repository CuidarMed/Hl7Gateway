using Hl7Gateway.DTOs;
using Hl7Gateway.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Linq;

namespace Hl7Gateway.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class Hl7SummaryController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Hl7SummaryController> _logger;
        private readonly Hl7MessageLogger _messageLogger;

        public Hl7SummaryController(IConfiguration configuration, ILogger<Hl7SummaryController> logger, Hl7MessageLogger messageLogger)
        {
            _configuration = configuration;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        /// <summary>
        /// Descarga el resumen HL7 por appointmentId
        /// </summary>
        [HttpGet("by-appointment/{appointmentId}")]
        public IActionResult GetSummaryByAppointmentId(long appointmentId)
        {
            try
            {
                var logDirectory = _configuration.GetValue<string>("Logging:LogDirectory", "logs");
                
                if (!Directory.Exists(logDirectory))
                {
                    return NotFound(new { message = "No se encontraron archivos de resumen" });
                }

                // Buscar archivos de resumen que contengan el appointmentId
                var summaryFiles = Directory.GetFiles(logDirectory, "RESUMEN_*.txt")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToList();

                if (!summaryFiles.Any())
                {
                    _logger.LogWarning("No se encontraron archivos de resumen en {LogDirectory}", logDirectory);
                    return NotFound(new { message = "No se encontró resumen HL7 para este appointment" });
                }

                // Buscar el archivo que contenga el appointmentId en su contenido
                string? targetFilePath = null;
                foreach (var filePath in summaryFiles)
                {
                    try
                    {
                        var content = System.IO.File.ReadAllText(filePath);
                        if (content.Contains($"AppointmentId: {appointmentId}") || 
                            content.Contains($"AppointmentId:{appointmentId}") ||
                            content.Contains($"{appointmentId}"))
                        {
                            targetFilePath = filePath;
                            _logger.LogInformation("Resumen encontrado para appointmentId {AppointmentId}: {FilePath}", appointmentId, filePath);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error leyendo archivo {FilePath}", filePath);
                    }
                }

                // Si no se encontró uno específico
                if (targetFilePath == null)
                {
                    _logger.LogWarning("No se encontró resumen específico para appointmentId {AppointmentId}", appointmentId);
                    
                    // Intentar obtener el más reciente que pueda estar relacionado
                    if (summaryFiles.Any())
                    {
                        _logger.LogInformation("Devolviendo el resumen más reciente disponible");
                        targetFilePath = summaryFiles.First();
                    }
                    else
                    {
                        return NotFound(new { 
                            message = $"No se encontró resumen HL7 para el appointmentId {appointmentId}. El resumen se genera automáticamente cuando se crea una consulta. Si la consulta fue creada antes de que el Gateway estuviera corriendo, el resumen no se generó. Por favor, crea una nueva consulta o contacta al administrador para regenerar el resumen." 
                        });
                    }
                }

                var contentToReturn = System.IO.File.ReadAllText(targetFilePath);
                var filename = Path.GetFileName(targetFilePath);

                return File(Encoding.UTF8.GetBytes(contentToReturn), "text/plain", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen HL7 para appointmentId: {AppointmentId}", appointmentId);
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Descarga el resumen HL7 por patientId y fecha
        /// </summary>
        [HttpGet("by-patient/{patientId}")]
        public IActionResult GetSummaryByPatientId(long patientId, [FromQuery] DateTime? date = null)
        {
            try
            {
                var logDirectory = _configuration.GetValue<string>("Logging:LogDirectory", "logs");
                
                if (!Directory.Exists(logDirectory))
                {
                    return NotFound(new { message = "No se encontraron archivos de resumen" });
                }

                var summaryFiles = Directory.GetFiles(logDirectory, "RESUMEN_*.txt")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToList();

                if (!summaryFiles.Any())
                {
                    return NotFound(new { message = "No se encontraron resúmenes HL7" });
                }

                // Filtrar por fecha si se proporciona
                if (date.HasValue)
                {
                    summaryFiles = summaryFiles
                        .Where(f => new FileInfo(f).CreationTime.Date == date.Value.Date)
                        .ToList();
                }

                if (!summaryFiles.Any())
                {
                    return NotFound(new { message = "No se encontró resumen HL7 para la fecha especificada" });
                }

                // Buscar el archivo que contenga información del patientId
                // Por ahora, devolvemos el más reciente que coincida
                var latestFile = summaryFiles.First();
                var content = System.IO.File.ReadAllText(latestFile);
                
                // Verificar que el contenido menciona el patientId
                if (!content.Contains($"PatientId: {patientId}"))
                {
                    // Si no coincide, buscar en otros archivos
                    var matchingFile = summaryFiles.FirstOrDefault(f => 
                        System.IO.File.ReadAllText(f).Contains($"PatientId: {patientId}"));
                    
                    if (matchingFile != null)
                    {
                        latestFile = matchingFile;
                        content = System.IO.File.ReadAllText(latestFile);
                    }
                }

                var filename = Path.GetFileName(latestFile);
                return File(Encoding.UTF8.GetBytes(content), "text/plain", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen HL7 para patientId: {PatientId}", patientId);
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lista todos los resúmenes disponibles
        /// </summary>
        [HttpGet("list")]
        public IActionResult ListSummaries([FromQuery] int limit = 10)
        {
            try
            {
                var logDirectory = _configuration.GetValue<string>("Logging:LogDirectory", "logs");
                
                if (!Directory.Exists(logDirectory))
                {
                    return Ok(new List<object>());
                }

                var summaryFiles = Directory.GetFiles(logDirectory, "RESUMEN_*.txt")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Take(limit)
                    .Select(f =>
                    {
                        var fileInfo = new FileInfo(f);
                        var content = System.IO.File.ReadAllText(f);
                        
                        // Extraer información básica del archivo
                        var lines = content.Split('\n');
                        var messageControlId = lines.FirstOrDefault(l => l.Contains("Message Control ID:"))?.Split(':').LastOrDefault()?.Trim() ?? "N/A";
                        var date = lines.FirstOrDefault(l => l.Contains("Fecha/Hora:"))?.Split(':').Skip(1).FirstOrDefault()?.Trim() ?? fileInfo.CreationTime.ToString();
                        var patientId = lines.FirstOrDefault(l => l.Contains("PatientId:"))?.Split(':').LastOrDefault()?.Trim() ?? "N/A";
                        
                        return new
                        {
                            filename = fileInfo.Name,
                            messageControlId,
                            date,
                            patientId,
                            size = fileInfo.Length,
                            created = fileInfo.CreationTime
                        };
                    })
                    .ToList();

                return Ok(summaryFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando resúmenes HL7");
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Genera un resumen HL7 a partir de los datos de un encounter
        /// </summary>
        [HttpPost("generate")]
        public IActionResult GenerateSummary([FromBody] GenerateSummaryRequest request)
        {
            try
            {
                // Validar ModelState (validación automática de ASP.NET Core)
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new
                        {
                            field = x.Key,
                            errors = x.Value?.Errors.Select(e => e.ErrorMessage)
                        })
                        .ToList();
                    
                    _logger.LogWarning("Errores de validación en GenerateSummary: {Errors}", 
                        string.Join(", ", errors.Select(e => $"{e.field}: {string.Join(", ", e.errors ?? new List<string>())}")));
                    
                    return BadRequest(new 
                    { 
                        message = "Errores de validación en el request",
                        errors = errors
                    });
                }
                
                // Validar campos requeridos manualmente
                if (request == null)
                {
                    _logger.LogWarning("Request nulo recibido en GenerateSummary");
                    return BadRequest(new { message = "El request no puede ser nulo" });
                }
                
                if (request.EncounterId <= 0)
                {
                    _logger.LogWarning("EncounterId inválido: {EncounterId}", request.EncounterId);
                    return BadRequest(new { message = "EncounterId es requerido y debe ser mayor a 0" });
                }
                
                if (request.PatientId <= 0)
                {
                    _logger.LogWarning("PatientId inválido: {PatientId}", request.PatientId);
                    return BadRequest(new { message = "PatientId es requerido y debe ser mayor a 0" });
                }
                
                if (request.DoctorId <= 0)
                {
                    _logger.LogWarning("DoctorId inválido: {DoctorId}", request.DoctorId);
                    return BadRequest(new { message = "DoctorId es requerido y debe ser mayor a 0" });
                }
                
                if (request.AppointmentId <= 0)
                {
                    _logger.LogWarning("AppointmentId inválido: {AppointmentId}", request.AppointmentId);
                    return BadRequest(new { message = "AppointmentId es requerido y debe ser mayor a 0" });
                }
                
                _logger.LogInformation("Generando resumen HL7 para EncounterId: {EncounterId}, AppointmentId: {AppointmentId}", 
                    request.EncounterId, request.AppointmentId);

                // Construir resumen del paciente
                var patientInfo = new StringBuilder();
                if (!string.IsNullOrEmpty(request.PatientDni))
                    patientInfo.AppendLine($"DNI: {request.PatientDni}");
                if (!string.IsNullOrEmpty(request.PatientFirstName) || !string.IsNullOrEmpty(request.PatientLastName))
                    patientInfo.AppendLine($"Nombre: {request.PatientFirstName} {request.PatientLastName}".Trim());
                if (request.PatientDateOfBirth.HasValue)
                    patientInfo.AppendLine($"Fecha Nacimiento: {request.PatientDateOfBirth.Value:yyyy-MM-dd}");
                if (!string.IsNullOrEmpty(request.PatientPhone))
                    patientInfo.AppendLine($"Teléfono: {request.PatientPhone}");
                if (!string.IsNullOrEmpty(request.PatientAddress))
                    patientInfo.AppendLine($"Dirección: {request.PatientAddress}");
                patientInfo.AppendLine($"PatientId: {request.PatientId}");

                // Construir resumen de la cita
                var appointmentInfo = new StringBuilder();
                appointmentInfo.AppendLine($"PatientId: {request.PatientId}");
                appointmentInfo.AppendLine($"DoctorId: {request.DoctorId}");
                if (!string.IsNullOrEmpty(request.DoctorFirstName) || !string.IsNullOrEmpty(request.DoctorLastName))
                    appointmentInfo.AppendLine($"Doctor: {request.DoctorFirstName} {request.DoctorLastName}".Trim());
                if (!string.IsNullOrEmpty(request.DoctorSpecialty))
                    appointmentInfo.AppendLine($"Especialidad: {request.DoctorSpecialty}");
                if (request.AppointmentStartTime.HasValue)
                    appointmentInfo.AppendLine($"Fecha/Hora Inicio: {request.AppointmentStartTime.Value:yyyy-MM-dd HH:mm:ss}");
                if (request.AppointmentEndTime.HasValue)
                    appointmentInfo.AppendLine($"Fecha/Hora Fin: {request.AppointmentEndTime.Value:yyyy-MM-dd HH:mm:ss}");
                if (!string.IsNullOrEmpty(request.AppointmentReason))
                    appointmentInfo.AppendLine($"Motivo: {request.AppointmentReason}");
                if (!string.IsNullOrEmpty(request.EncounterReasons))
                    appointmentInfo.AppendLine($"Motivo de Consulta: {request.EncounterReasons}");
                if (!string.IsNullOrEmpty(request.EncounterAssessment))
                    appointmentInfo.AppendLine($"Diagnóstico: {request.EncounterAssessment}");
                appointmentInfo.AppendLine($"AppointmentId: {request.AppointmentId}");
                appointmentInfo.AppendLine($"EncounterId: {request.EncounterId}");

                // Generar Message Control ID único
                var messageControlId = $"ENC{request.EncounterId}_{DateTime.Now:yyyyMMddHHmmss}";
                
                // Generar mensaje HL7 v2 ORM^O01
                var hl7Message = GenerateHl7OrmMessage(request, messageControlId);

                // Guardar resumen (incluyendo el mensaje HL7)
                _messageLogger.LogSummary(
                    messageControlId,
                    patientInfo.ToString(),
                    appointmentInfo.ToString(),
                    "AA",
                    null,
                    hl7Message
                );

                _logger.LogInformation("Resumen HL7 generado exitosamente para EncounterId: {EncounterId}", request.EncounterId);

                return Ok(new 
                { 
                    message = "Resumen HL7 generado exitosamente",
                    messageControlId = messageControlId,
                    encounterId = request.EncounterId,
                    appointmentId = request.AppointmentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando resumen HL7 para EncounterId: {EncounterId}", request?.EncounterId ?? 0);
                return StatusCode(500, new { message = $"Error generando resumen: {ex.Message}", details = ex.ToString() });
            }
        }

        /// <summary>
        /// Genera un mensaje HL7 v2 ORM^O01 a partir de los datos del encounter
        /// </summary>
        private string GenerateHl7OrmMessage(GenerateSummaryRequest request, string messageControlId)
        {
            var sb = new StringBuilder();
            var now = DateTime.Now;
            var timestamp = now.ToString("yyyyMMddHHmmss");
            var dateOnly = now.ToString("yyyyMMdd");
            
            // MSH - Message Header
            sb.AppendLine($"MSH|^~\\&|CUIDARMED|CUIDARMED_HOSPITAL|Hl7Gateway|ABC_RADIOLOGY|{timestamp}||ORM^O01|{messageControlId}|P|2.3|||||||");
            
            // PID - Patient Identification
            var patientId = request.PatientDni ?? request.PatientId.ToString();
            var lastName = request.PatientLastName ?? "";
            var firstName = request.PatientFirstName ?? "";
            var birthDate = request.PatientDateOfBirth?.ToString("yyyyMMdd") ?? "";
            var phone = request.PatientPhone ?? "";
            var address = request.PatientAddress ?? "";
            
            sb.AppendLine($"PID|1||{patientId}^^^^EPI||{lastName}^{firstName}^^^MR.^||{birthDate}|M|||{address}|||{phone}|||||||");
            
            // PD1 - Patient Additional Demographic (opcional)
            sb.AppendLine($"PD1|||FACILITY^^{request.DoctorId}|{request.DoctorId}^{request.DoctorLastName}^{request.DoctorFirstName}^^^");
            
            // PV1 - Patient Visit
            var visitNumber = request.AppointmentId.ToString();
            var doctorId = request.DoctorId.ToString();
            var doctorLastName = request.DoctorLastName ?? "";
            var doctorFirstName = request.DoctorFirstName ?? "";
            var encounterDate = request.EncounterDate.ToString("yyyyMMdd");
            
            sb.AppendLine($"PV1|||^^^CUIDARMED HEALTH SYSTEMS^^^^^||| |{doctorId}^{doctorLastName}^{doctorFirstName}^^^||||||||||{encounterDate}||||||||||||||||||||||||||||||||V");
            
            // ORC - Common Order
            var orderNumber = request.AppointmentId.ToString();
            var orderDate = request.AppointmentStartTime?.ToString("yyyyMMddHHmmss") ?? timestamp;
            var orderEndDate = request.AppointmentEndTime?.ToString("yyyyMMddHHmmss") ?? timestamp;
            
            sb.AppendLine($"ORC|NW|{orderNumber}^EPIC|{orderNumber}^EPC||Final||^^^{orderDate}^^^^||{orderEndDate}|{doctorId}^{doctorLastName}^{doctorFirstName}^^^^||{doctorId}^{doctorLastName}^{doctorFirstName}^^^|{doctorId}^^^222^^^^^|{phone}||");
            
            // OBR - Observation Request
            var serviceCode = request.EncounterAssessment ?? "CONSULTA";
            var reason = request.EncounterReasons ?? request.AppointmentReason ?? "Consulta médica";
            
            sb.AppendLine($"OBR|1|{orderNumber}^EPC|{orderNumber}^EPC|{serviceCode}^CONSULTA MEDICA^^^CONSULTA ||||||||||||{doctorId}^{doctorLastName}^{doctorFirstName}^^^|{phone}||||||||Final||^^^{orderDate}^^^^|||||{doctorId}^{doctorLastName}^{doctorFirstName}^^^^||{visitNumber}^1A^CUIDARMED^CONSULTA^^^|^|");
            
            // DG1 - Diagnosis (si hay diagnóstico)
            if (!string.IsNullOrEmpty(request.EncounterAssessment))
            {
                sb.AppendLine($"DG1||I10|{request.EncounterAssessment}^DIAGNOSTICO^I10|{request.EncounterAssessment}|");
            }
            
            return sb.ToString();
        }
    }
}

