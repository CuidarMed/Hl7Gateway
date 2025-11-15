# Script para verificar si el Hl7Gateway está corriendo

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Verificación del Hl7Gateway" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar si el puerto 5000 está en uso
$port5000 = netstat -ano | Select-String ":5000" | Select-String "LISTENING"

if ($port5000) {
    Write-Host "✅ El Hl7Gateway ESTÁ CORRIENDO" -ForegroundColor Green
    Write-Host ""
    Write-Host "Puerto 5000 está escuchando:" -ForegroundColor Yellow
    $port5000 | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
    Write-Host ""
    
    # Intentar hacer una petición HTTP para verificar
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/v1/Hl7Summary/list" -Method GET -TimeoutSec 2 -ErrorAction Stop
        Write-Host "✅ El Gateway responde correctamente" -ForegroundColor Green
        Write-Host "   Status: $($response.StatusCode)" -ForegroundColor White
    } catch {
        Write-Host "⚠️  El puerto está abierto pero el Gateway no responde" -ForegroundColor Yellow
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "❌ El Hl7Gateway NO está corriendo" -ForegroundColor Red
    Write-Host ""
    Write-Host "Para iniciarlo, ejecuta:" -ForegroundColor Yellow
    Write-Host "  cd Hl7Gateway" -ForegroundColor White
    Write-Host "  dotnet run" -ForegroundColor White
    Write-Host ""
    Write-Host "O usa el script:" -ForegroundColor Yellow
    Write-Host "  .\ver-hl7.ps1" -ForegroundColor White
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

