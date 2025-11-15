using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Hl7Gateway.Services
{
    public class Hl7MessageLogger
    {
        private readonly ILogger<Hl7MessageLogger> _logger;
        private readonly string _logDirectory;
        private readonly bool _enableFileLogging;

        public Hl7MessageLogger(ILogger<Hl7MessageLogger> logger, IConfiguration configuration)
        {
            _logger = logger;
            _enableFileLogging = configuration.GetValue<bool>("Logging:EnableFileLogging", true);
            _logDirectory = configuration.GetValue<string>("Logging:LogDirectory", "logs");
            
            if (_enableFileLogging && !Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void LogMessage(string messageType, string hl7Message, string? summary = null)
        {
            if (!_enableFileLogging) return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"{messageType}_{timestamp}.txt";
                var filepath = Path.Combine(_logDirectory, filename);

                var content = new StringBuilder();
                content.AppendLine("========================================");
                content.AppendLine($"MENSAJE HL7 - {messageType}");
                content.AppendLine($"Fecha/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine("========================================");
                content.AppendLine();
                content.AppendLine("MENSAJE HL7 COMPLETO:");
                content.AppendLine("----------------------------------------");
                content.AppendLine(hl7Message);
                content.AppendLine("----------------------------------------");
                
                if (!string.IsNullOrEmpty(summary))
                {
                    content.AppendLine();
                    content.AppendLine("RESUMEN DE LA CONSULTA:");
                    content.AppendLine("----------------------------------------");
                    content.AppendLine(summary);
                    content.AppendLine("----------------------------------------");
                }

                File.WriteAllText(filepath, content.ToString());
                _logger.LogInformation("Mensaje HL7 guardado en: {Filepath}", filepath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando mensaje HL7 en archivo");
            }
        }

        public void LogSummary(string messageControlId, string patientInfo, string? appointmentInfo, string ackCode, string? errorMessage = null, string? hl7Message = null)
        {
            if (!_enableFileLogging) return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"RESUMEN_{messageControlId}_{timestamp}.txt";
                var filepath = Path.Combine(_logDirectory, filename);

                var content = new StringBuilder();
                content.AppendLine("========================================");
                content.AppendLine("RESUMEN DE CONSULTA HL7");
                content.AppendLine("========================================");
                content.AppendLine($"Fecha/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                content.AppendLine($"Message Control ID: {messageControlId}");
                content.AppendLine($"Estado: {(ackCode == "AA" ? "PROCESADO EXITOSAMENTE" : "ERROR")}");
                content.AppendLine();
                
                content.AppendLine("INFORMACIÓN DEL PACIENTE:");
                content.AppendLine("----------------------------------------");
                content.AppendLine(patientInfo);
                content.AppendLine();
                
                if (!string.IsNullOrEmpty(appointmentInfo))
                {
                    content.AppendLine("INFORMACIÓN DE LA CITA/ORDEN:");
                    content.AppendLine("----------------------------------------");
                    content.AppendLine(appointmentInfo);
                    content.AppendLine();
                }
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    content.AppendLine("ERROR:");
                    content.AppendLine("----------------------------------------");
                    content.AppendLine(errorMessage);
                    content.AppendLine();
                }
                
                if (!string.IsNullOrEmpty(hl7Message))
                {
                    content.AppendLine("MENSAJE HL7 v2 (ORM^O01):");
                    content.AppendLine("----------------------------------------");
                    content.AppendLine(hl7Message);
                    content.AppendLine("----------------------------------------");
                    content.AppendLine();
                }
                
                content.AppendLine("========================================");
                content.AppendLine($"ACK Code: {ackCode}");
                content.AppendLine("========================================");

                File.WriteAllText(filepath, content.ToString());
                _logger.LogInformation("Resumen guardado en: {Filepath}", filepath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando resumen en archivo");
            }
        }
    }
}

