using Hl7Gateway.DTOs;
using Hl7Gateway.Mappers;
using Microsoft.Extensions.Logging;
using NHapi.Base.Parser;
using NHapi.Model.V23.Message;
using System.Text;

namespace Hl7Gateway.Services
{
    public class Hl7MessageProcessor
    {
        private readonly ILogger<Hl7MessageProcessor> _logger;
        private readonly DirectoryService _directoryService;
        private readonly SchedulingService _schedulingService;
        private readonly Hl7MessageLogger _messageLogger;
        private readonly PipeParser _parser;

        public Hl7MessageProcessor(
            ILogger<Hl7MessageProcessor> logger,
            DirectoryService directoryService,
            SchedulingService schedulingService,
            Hl7MessageLogger messageLogger)
        {
            _logger = logger;
            _directoryService = directoryService;
            _schedulingService = schedulingService;
            _messageLogger = messageLogger;
            _parser = new PipeParser();
        }

        public async Task<byte[]> ProcessMessageAsync(byte[] messageBytes)
        {
            try
            {
                var messageText = Encoding.UTF8.GetString(messageBytes);
                _logger.LogInformation("========================================");
                _logger.LogInformation("MENSAJE HL7 RECIBIDO:");
                _logger.LogInformation("========================================");
                _logger.LogInformation("{Message}", messageText);
                _logger.LogInformation("========================================");
                
                // Guardar mensaje en archivo
                _messageLogger.LogMessage("RECIBIDO", messageText);

                // Parsear mensaje HL7
                var message = _parser.Parse(messageText);
                
                if (message is ORM_O01 ormMessage)
                {
                    return await ProcessOrmMessageAsync(ormMessage);
                }
                else
                {
                    _logger.LogWarning("Tipo de mensaje no soportado: {MessageType}", message.GetType().Name);
                    return GenerateAck(message, "AE", "Tipo de mensaje no soportado");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje HL7");
                // Intentar extraer MSH para generar ACK
                try
                {
                    var messageText = Encoding.UTF8.GetString(messageBytes);
                    var message = _parser.Parse(messageText);
                    return GenerateAck(message, "AE", $"Error procesando mensaje: {ex.Message}");
                }
                catch
                {
                    // Si no se puede parsear, generar ACK genérico
                    return Encoding.UTF8.GetBytes(GenerateGenericAck("AE", "Error procesando mensaje"));
                }
            }
        }

        private async Task<byte[]> ProcessOrmMessageAsync(ORM_O01 ormMessage)
        {
            try
            {
                var msh = ormMessage.MSH;
                var messageControlId = msh.MessageControlID?.Value ?? "UNKNOWN";
                _logger.LogInformation("Procesando ORM^O01 con MessageControlID: {MessageControlId}", messageControlId);

                // Extraer PID
                var patient = ormMessage.GetPATIENT();
                if (patient == null)
                {
                    _logger.LogError("Mensaje ORM^O01 sin grupo PATIENT");
                    return GenerateAck(ormMessage, "AE", "Falta grupo PATIENT");
                }

                var pid = patient.PID;
                if (pid == null)
                {
                    _logger.LogError("Mensaje ORM^O01 sin segmento PID");
                    return GenerateAck(ormMessage, "AE", "Falta segmento PID");
                }

                // Validar PID requeridos
                string? identifierValue = null;
                try
                {
                    var identifier = pid.GetPatientIdentifierList(0);
                    identifierValue = identifier?.IDNumber?.Value;
                }
                catch { }

                string? firstName = null;
                string? lastName = null;
                try
                {
                    var patientName = pid.GetPatientName(0);
                    firstName = patientName?.GivenName?.Value;
                    lastName = patientName?.FamilyName?.Value;
                }
                catch { }

                _logger.LogInformation("PID parseado - Identificador: {Id}, Nombre: {FirstName} {LastName}", 
                    identifierValue, firstName, lastName);

                if (string.IsNullOrEmpty(identifierValue))
                {
                    _logger.LogError("PID-3 (identificador) es requerido");
                    return GenerateAck(ormMessage, "AE", "PID-3 (identificador) es requerido");
                }

                if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
                {
                    _logger.LogError("PID-5 (nombre) es requerido");
                    return GenerateAck(ormMessage, "AE", "PID-5 (nombre) es requerido");
                }

                // Mapear PID a PatientUpsert
                var patientDto = Hl7ToDtoMapper.MapPidToPatientUpsert(pid);
                _logger.LogInformation("DTO PatientUpsert creado: DNI={Dni}, Nombre={FirstName} {LastName}", 
                    patientDto.Dni, patientDto.FirstName, patientDto.LastName);

                // Upsert paciente en DirectoryMS
                var patientId = await _directoryService.UpsertPatientAsync(patientDto);
                if (patientId == null)
                {
                    _logger.LogError("Error en upsert de paciente");
                    return GenerateAck(ormMessage, "AE", "Error en upsert de paciente");
                }

                _logger.LogInformation("Paciente upsert exitoso, PatientId: {PatientId}", patientId);

                // Construir resumen del paciente
                var patientSummary = $"DNI: {patientDto.Dni}\n" +
                                    $"Nombre: {patientDto.FirstName} {patientDto.LastName}\n" +
                                    $"Fecha Nacimiento: {patientDto.DateOfBirth}\n" +
                                    $"Teléfono: {patientDto.Phone}\n" +
                                    $"Dirección: {patientDto.Adress}\n" +
                                    $"PatientId: {patientId}";

                // Extraer ORC/OBR
                var order = ormMessage.GetORDER();
                if (order != null)
                {
                    var orc = order.ORC;
                    var orderDetail = order.GetORDER_DETAIL();
                    var obr = orderDetail?.OBR;

                    if (orc != null && obr != null)
                    {
                        string placerOrderNumber = "N/A";
                        try
                        {
                            placerOrderNumber = orc.GetPlacerOrderNumber()?.EntityIdentifier?.Value ?? "N/A";
                        }
                        catch { }

                        string universalServiceId = "N/A";
                        try
                        {
                            universalServiceId = obr.GetUniversalServiceID()?.Identifier?.Value ?? "N/A";
                        }
                        catch { }

                        _logger.LogInformation("ORC/OBR parseado - PlacerOrderNumber: {PlacerOrderNumber}, UniversalServiceID: {UniversalServiceID}",
                            placerOrderNumber, universalServiceId);

                        // Mapear ORC/OBR a AppointmentCreate
                        var appointmentDto = Hl7ToDtoMapper.MapOrcObrToAppointmentCreate(orc, obr, patientId.Value);
                        _logger.LogInformation("DTO AppointmentCreate creado: PatientId={PatientId}, StartTime={StartTime}, Reason={Reason}",
                            appointmentDto.PatientId, appointmentDto.StartTime, appointmentDto.Reason);

                        // Crear appointment en SchedulingMS
                        var appointmentId = await _schedulingService.CreateAppointmentFromOrderAsync(appointmentDto);
                        if (appointmentId == null)
                        {
                            _logger.LogError("Error creando appointment desde orden");
                            return GenerateAck(ormMessage, "AE", "Error creando appointment desde orden");
                        }

                        _logger.LogInformation("Appointment creado exitosamente, AppointmentId: {AppointmentId}", appointmentId);
                        
                        // Construir resumen de la cita
                        var appointmentSummary = $"PatientId: {appointmentDto.PatientId}\n" +
                                                $"DoctorId: {appointmentDto.DoctorId}\n" +
                                                $"Fecha/Hora Inicio: {appointmentDto.StartTime}\n" +
                                                $"Fecha/Hora Fin: {appointmentDto.EndTime}\n" +
                                                $"Motivo: {appointmentDto.Reason}\n" +
                                                $"AppointmentId: {appointmentId}";
                        
                        // Guardar resumen completo
                        _messageLogger.LogSummary(
                            messageControlId,
                            patientSummary,
                            appointmentSummary,
                            "AA"
                        );
                    }
                    else
                    {
                        // Guardar resumen solo con paciente (sin cita)
                        _messageLogger.LogSummary(
                            messageControlId,
                            patientSummary,
                            null,
                            "AA"
                        );
                    }
                }
                else
                {
                    // Guardar resumen solo con paciente (sin orden)
                    _messageLogger.LogSummary(
                        messageControlId,
                        patientSummary,
                        null,
                        "AA"
                    );
                }

                // Extraer PV1 si existe (opcional)
                var patientVisit = patient.PATIENT_VISIT;
                var pv1 = patientVisit?.PV1;
                if (pv1 != null)
                {
                    _logger.LogInformation("PV1 parseado - Location: {Location}, VisitNumber: {VisitNumber}",
                        pv1.AssignedPatientLocation?.PointOfCare?.Value,
                        pv1.VisitNumber?.ID?.Value);
                    // PV1 es opcional, solo logueamos
                }

                // Generar ACK AA (Application Accept)
                var ackBytes = GenerateAck(ormMessage, "AA", "Mensaje procesado exitosamente");
                
                // Guardar ACK en archivo
                var ackText = Encoding.UTF8.GetString(ackBytes);
                _messageLogger.LogMessage("ACK_AA", ackText, "Mensaje procesado exitosamente");
                
                return ackBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje ORM^O01");
                
                // Guardar error en resumen
                try
                {
                    var msh = ormMessage.MSH;
                    var messageControlId = msh.MessageControlID?.Value ?? "UNKNOWN";
                    _messageLogger.LogSummary(
                        messageControlId,
                        "Error al procesar mensaje",
                        null,
                        "AE",
                        ex.Message
                    );
                }
                catch { }
                
                var ackBytes = GenerateAck(ormMessage, "AE", $"Error: {ex.Message}");
                var ackText = Encoding.UTF8.GetString(ackBytes);
                _messageLogger.LogMessage("ACK_AE", ackText, $"Error: {ex.Message}");
                
                return ackBytes;
            }
        }

        private byte[] GenerateAck(NHapi.Base.Model.IMessage originalMessage, string ackCode, string textMessage)
        {
            try
            {
                var msh = originalMessage.GetStructure("MSH") as NHapi.Model.V23.Segment.MSH;
                if (msh == null)
                {
                    return Encoding.UTF8.GetBytes(GenerateGenericAck(ackCode, textMessage));
                }

                var ack = new ACK();
                var ackMsh = ack.MSH;

                // MSH-1: Field Separator
                ackMsh.FieldSeparator.Value = "|";
                
                // MSH-2: Encoding Characters
                ackMsh.EncodingCharacters.Value = "^~\\&";
                
                // MSH-3: Sending Application (intercambiar con Receiving)
                ackMsh.SendingApplication.NamespaceID.Value = msh.ReceivingApplication?.NamespaceID?.Value ?? "HL7GATEWAY";
                ackMsh.ReceivingApplication.NamespaceID.Value = msh.SendingApplication?.NamespaceID?.Value ?? "EXTERNAL";
                
                // MSH-4: Sending Facility
                ackMsh.SendingFacility.NamespaceID.Value = msh.ReceivingFacility?.NamespaceID?.Value ?? "HL7GATEWAY";
                ackMsh.ReceivingFacility.NamespaceID.Value = msh.SendingFacility?.NamespaceID?.Value ?? "EXTERNAL";
                
                // MSH-5: Receiving Application
                // MSH-6: Receiving Facility
                // (ya intercambiados arriba)
                
                // MSH-7: Date/Time of Message
                ackMsh.DateTimeOfMessage.TimeOfAnEvent.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                
                // MSH-9: Message Type
                ackMsh.MessageType.MessageType.Value = "ACK";
                ackMsh.MessageType.TriggerEvent.Value = "A08"; // ACK genérico
                
                // MSH-10: Message Control ID (eco del original)
                ackMsh.MessageControlID.Value = msh.MessageControlID?.Value ?? Guid.NewGuid().ToString();
                
                // MSH-11: Processing ID
                ackMsh.ProcessingID.ProcessingID.Value = msh.ProcessingID?.ProcessingID?.Value ?? "P";
                
                // MSH-12: Version ID
                try
                {
                    ackMsh.VersionID.VersionID.Value = msh.VersionID?.VersionID?.Value ?? "2.3";
                }
                catch
                {
                    ackMsh.VersionID.VersionID.Value = "2.3";
                }

                // MSA-1: Acknowledgment Code
                ack.MSA.AcknowledgmentCode.Value = ackCode;
                
                // MSA-2: Message Control ID (eco del MSH-10 original)
                ack.MSA.MessageControlID.Value = msh.MessageControlID?.Value ?? "UNKNOWN";
                
                // MSA-3: Text Message
                if (!string.IsNullOrEmpty(textMessage))
                {
                    ack.MSA.TextMessage.Value = textMessage;
                }

                var ackString = _parser.Encode(ack);
                _logger.LogInformation("ACK generado ({AckCode}):\n{Ack}", ackCode, ackString);
                
                return Encoding.UTF8.GetBytes(ackString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando ACK");
                return Encoding.UTF8.GetBytes(GenerateGenericAck(ackCode, textMessage));
            }
        }

        private string GenerateGenericAck(string ackCode, string textMessage)
        {
            var controlId = Guid.NewGuid().ToString();
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"MSH|^~\\&|HL7GATEWAY|HL7GATEWAY|EXTERNAL|EXTERNAL|{timestamp}||ACK^A08|{controlId}|P|2.3\r" +
                   $"MSA|{ackCode}|{controlId}|{textMessage}\r";
        }
    }
}

