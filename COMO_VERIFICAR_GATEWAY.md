# Cómo Verificar si el Hl7Gateway está Corriendo

## Método 1: Usar el Script de Verificación (Recomendado)

Ejecuta en PowerShell desde la carpeta `Hl7Gateway`:

```powershell
.\verificar-gateway.ps1
```

Este script te dirá si el Gateway está corriendo y si responde correctamente.

## Método 2: Verificar el Puerto 5000

Ejecuta en PowerShell:

```powershell
netstat -ano | Select-String ":5000"
```

Si ves algo como:
```
TCP    127.0.0.1:5000         0.0.0.0:0              LISTENING       12345
```

Entonces el Gateway **ESTÁ corriendo** (el número 12345 es el ID del proceso).

Si no ves nada, el Gateway **NO está corriendo**.

## Método 3: Probar la API Directamente

Abre tu navegador y ve a:
```
http://localhost:5000/api/v1/Hl7Summary/list
```

O ejecuta en PowerShell:

```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/v1/Hl7Summary/list" -Method GET
```

Si obtienes una respuesta (aunque sea un array vacío `[]`), el Gateway **ESTÁ corriendo**.

Si obtienes un error de conexión, el Gateway **NO está corriendo**.

## Cómo Iniciar el Gateway

### Opción 1: Desde PowerShell (Recomendado)

1. Abre una nueva ventana de PowerShell
2. Navega a la carpeta del Gateway:
   ```powershell
   cd "C:\Users\herna\Desktop\CuidarMed+\Hl7Gateway"
   ```
3. Ejecuta:
   ```powershell
   dotnet run
   ```

Deberías ver mensajes como:
```
Iniciando Hl7Gateway...
API REST disponible en puerto 5000
```

**IMPORTANTE:** Deja esta ventana abierta. Si la cierras, el Gateway se detendrá.

### Opción 2: Usar el Script

Desde la carpeta `Hl7Gateway`, ejecuta:

```powershell
.\ver-hl7.ps1
```

### Opción 3: En una Ventana Separada (Background)

Para iniciarlo en una ventana separada que puedas cerrar sin detener el Gateway:

```powershell
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'C:\Users\herna\Desktop\CuidarMed+\Hl7Gateway'; dotnet run"
```

## Verificación Rápida

**El Gateway debe estar corriendo ANTES de:**
- Crear una nueva consulta (para que se genere el resumen HL7 automáticamente)
- Intentar descargar un resumen HL7 desde el panel del paciente

**Síntomas de que el Gateway NO está corriendo:**
- Error `ERR_CONNECTION_REFUSED` en la consola del navegador
- Mensaje "No se encontró resumen HL7 para esta consulta"
- Error 404 al intentar descargar el resumen

## Solución de Problemas

### El Gateway no inicia

1. Verifica que estés en la carpeta correcta:
   ```powershell
   cd "C:\Users\herna\Desktop\CuidarMed+\Hl7Gateway"
   ```

2. Verifica que el proyecto compile:
   ```powershell
   dotnet build
   ```

3. Si hay errores, corrígelos antes de ejecutar.

### El Gateway se detiene

- Asegúrate de no cerrar la ventana de PowerShell donde está corriendo
- Si se detiene, simplemente vuelve a ejecutar `dotnet run`

### El puerto 5000 está ocupado

Si otro programa está usando el puerto 5000:

1. Encuentra el proceso:
   ```powershell
   netstat -ano | Select-String ":5000"
   ```

2. Detén el proceso (reemplaza 12345 con el ID real):
   ```powershell
   Stop-Process -Id 12345 -Force
   ```

3. Vuelve a iniciar el Gateway.

