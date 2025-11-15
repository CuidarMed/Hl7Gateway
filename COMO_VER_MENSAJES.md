# Cómo Ver los Mensajes HL7 Generados

## Método 1: Ver en Consola (Más Fácil) ⭐

### Paso 1: Ejecutar el Gateway

Abre una terminal en la carpeta `Hl7Gateway` y ejecuta:

```powershell
# Opción A: Usar el script
.\ver-hl7.ps1

# Opción B: Ejecutar directamente
dotnet run
```

Verás algo como esto:

```
[Information] ========================================
[Information] MENSAJE HL7 RECIBIDO:
[Information] ========================================
[Information] MSH|^~\&|SENDING_APP|SENDING_FACILITY|...
PID|||12345678^^^DNI||GARCIA^JUAN^MARIA||...
[Information] ========================================
[Information] Procesando ORM^O01 con MessageControlID: MSG001
[Information] PID parseado - Identificador: 12345678, Nombre: JUAN GARCIA
[Information] DTO PatientUpsert creado: DNI=12345678, Nombre=JUAN GARCIA
[Information] ACK generado (AA):
MSH|^~\&|HL7GATEWAY|HL7GATEWAY|...
MSA|AA|MSG001|Mensaje procesado exitosamente
```

### Paso 2: Enviar un Mensaje de Prueba

Abre **otra terminal** y ejecuta:

```powershell
cd Sender
.\enviar-mensaje.ps1

# O directamente:
dotnet run
```

El Sender mostrará:
- El mensaje que envía
- El ACK que recibe del Gateway

## Método 2: Ver Mensaje Completo en Archivo

Puedes redirigir la salida a un archivo:

```powershell
dotnet run > hl7-logs.txt 2>&1
```

Luego abre `hl7-logs.txt` para ver todos los mensajes.

## Método 3: Usar Herramientas Externas

### HL7 Inspector (Windows)
- Descarga: https://www.hl7inspector.com/
- Conecta a `localhost:2575`
- Envía mensajes y ve las respuestas

### MLLP Client (Línea de comandos)
```bash
# Instalar herramienta MLLP
# Luego enviar mensaje:
echo "MSH|^~\&|..." | mllp-send localhost 2575
```

## Estructura del Mensaje que Verás

### Mensaje Original (ORM^O01):
```
MSH|^~\&|...          ← Cabecera
PID|||12345678...    ← Datos del paciente
PV1||I|...           ← Información de visita (opcional)
ORC|NW|...           ← Orden
OBR|1|...            ← Estudio solicitado
```

### ACK Generado:
```
MSH|^~\&|...          ← Cabecera del ACK
MSA|AA|MSG001|...     ← Confirmación (AA=Aceptado, AE=Error)
```

## Logs Detallados

El Gateway muestra:

1. **Mensaje completo recibido** (formato HL7)
2. **Segmentos parseados**:
   - MSH: Información del mensaje
   - PID: Datos del paciente (DNI, nombre, fecha nacimiento, teléfono)
   - ORC/OBR: Orden y estudio
   - PV1: Visita (si está presente)
3. **DTOs construidos**:
   - PatientUpsert: Datos mapeados para DirectoryMS
   - AppointmentCreate: Datos mapeados para SchedulingMS
4. **ACK generado**: Respuesta completa en formato HL7

## Nivel de Logging

Para ver más/menos detalles, edita `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",  // Cambiar a "Debug" para más detalles
      "Microsoft": "Warning"
    }
  }
}
```

## Ejemplo Completo de Salida

```
[Information] Iniciando Hl7Gateway en puerto 2575...
[Information] MLLP Listener iniciado en puerto 2575
[Information] Cliente conectado: 127.0.0.1:54321
[Information] ========================================
[Information] MENSAJE HL7 RECIBIDO:
[Information] ========================================
[Information] MSH|^~\&|SENDING_APP|SENDING_FACILITY|RECEIVING_APP|RECEIVING_FACILITY|20250114120000||ORM^O01|MSG001|P|2.3
PID|||12345678^^^DNI||GARCIA^JUAN^MARIA||19850115|M|||123 CALLE PRINCIPAL^^CIUDAD^PROVINCIA^12345||5551234567|||||||||||||||||
PV1||I|EMERGENCY^A1^01||||12345^DOCTOR^JOHN^MD|||||||||||V123456|||||||||||||||||||||||20250114120000
ORC|NW|ORD001|||CM|N||||20250114120000|^DOCTOR^JOHN^MD|12345^DOCTOR^JOHN^MD||||||
OBR|1|ORD001||LAB001^LABORATORIO COMPLETO^L|||20250114120000|||||||||^DOCTOR^JOHN^MD||||||20250114120000|||F
[Information] ========================================
[Information] Procesando ORM^O01 con MessageControlID: MSG001
[Information] PID parseado - Identificador: 12345678, Nombre: JUAN GARCIA
[Information] DTO PatientUpsert creado: DNI=12345678, Nombre=JUAN GARCIA
[Information] Paciente upsert exitoso, PatientId: 123
[Information] ORC/OBR parseado - PlacerOrderNumber: ORD001, UniversalServiceID: LAB001
[Information] DTO AppointmentCreate creado: PatientId=123, StartTime=2025-01-15 12:00:00, Reason=Estudio: LAB001
[Information] Appointment creado exitosamente, AppointmentId: 456
[Information] PV1 parseado - Location: EMERGENCY, VisitNumber: V123456
[Information] ACK generado (AA):
MSH|^~\&|HL7GATEWAY|HL7GATEWAY|EXTERNAL|EXTERNAL|20250114120001||ACK^A08|ACK001|P|2.3
MSA|AA|MSG001|Mensaje procesado exitosamente
[Information] ACK enviado a 127.0.0.1:54321
```

## Troubleshooting

**No veo los mensajes:**
- Verifica que el Gateway esté ejecutándose
- Verifica que el puerto 2575 esté disponible
- Revisa los logs de error

**El mensaje no se parsea:**
- Verifica el formato HL7 (debe terminar con `\r`)
- Revisa que sea un mensaje ORM^O01 válido
- Mira los logs de error para detalles

