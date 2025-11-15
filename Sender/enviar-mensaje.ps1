# Script para enviar un mensaje HL7 de prueba al Gateway

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Hl7Sender - Enviar Mensaje HL7" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$hostname = if ($args.Length -gt 0) { $args[0] } else { "localhost" }
$port = if ($args.Length -gt 1) { [int]$args[1] } else { 2575 }

Write-Host "Conectando a $hostname`:$port..." -ForegroundColor Yellow
Write-Host ""

# Ejecutar el Sender
dotnet run -- $hostname $port

