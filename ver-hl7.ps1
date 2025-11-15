# Script para ver mensajes HL7 en el Gateway
# Ejecuta el Gateway y muestra los mensajes procesados

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Hl7Gateway - Visualizador de Mensajes" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Este script ejecutará el Gateway y mostrará:" -ForegroundColor Yellow
Write-Host "  - Mensajes HL7 recibidos (completos)" -ForegroundColor White
Write-Host "  - Segmentos parseados (MSH, PID, ORC, OBR, PV1)" -ForegroundColor White
Write-Host "  - DTOs construidos (PatientUpsert, AppointmentCreate)" -ForegroundColor White
Write-Host "  - ACKs generados (AA/AE/AR)" -ForegroundColor White
Write-Host ""

Write-Host "Para enviar un mensaje de prueba, abre otra terminal y ejecuta:" -ForegroundColor Green
Write-Host "  cd Sender" -ForegroundColor White
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""

Write-Host "Presiona Ctrl+C para detener el Gateway" -ForegroundColor Yellow
Write-Host ""
Write-Host "Iniciando Gateway..." -ForegroundColor Green
Write-Host ""

# Ejecutar el Gateway
dotnet run

