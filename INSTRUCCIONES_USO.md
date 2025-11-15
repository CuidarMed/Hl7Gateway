# Instrucciones para Ver Mensajes HL7

## Opción 1: Ver Logs del Gateway (Recomendado)

El Gateway muestra todos los mensajes HL7 procesados en la consola con logging detallado.

### Pasos:

1. **Compilar el proyecto** (primero hay que corregir los errores de NHapi):
```bash
cd Hl7Gateway
dotnet build
```

2. **Ejecutar el Gateway**:
```bash
dotnet run
```

Verás logs como:
```
[Information] Iniciando Hl7Gateway en puerto 2575...
[Information] MLLP Listener iniciado en puerto 2575
[Information] Cliente conectado: 127.0.0.1:xxxxx
[Information] Procesando mensaje HL7:
MSH|^~\&|SENDING_APP|...
PID|||12345678...
[Information] PID parseado - Identificador: 12345678, Nombre: JUAN GARCIA
[Information] DTO PatientUpsert creado: DNI=12345678, Nombre=JUAN GARCIA
[Information] ACK generado (AA): MSH|^~\&|...
```

3. **En otra terminal, ejecutar el Sender**:
```bash
cd Sender
dotnet run
```

El Sender enviará un mensaje HL7 de ejemplo y mostrará el ACK recibido.

## Opción 2: Ver Mensajes en Archivo de Log

Puedes configurar logging a archivo modificando `Program.cs` para agregar un FileLogger.

## Opción 3: Usar Herramienta Externa MLLP

Puedes usar herramientas como:
- **HL7 Inspector** (Windows)
- **MLLP Client** (herramientas de línea de comandos)
- **Postman** (con plugin MLLP)

Para conectarte a `localhost:2575` y enviar mensajes HL7.

## Mensaje HL7 de Ejemplo

El Sender envía este mensaje de ejemplo:

```
MSH|^~\&|SENDING_APP|SENDING_FACILITY|RECEIVING_APP|RECEIVING_FACILITY|20250114120000||ORM^O01|MSG001|P|2.3
PID|||12345678^^^DNI||GARCIA^JUAN^MARIA||19850115|M|||123 CALLE PRINCIPAL^^CIUDAD^PROVINCIA^12345||5551234567|||||||||||||||||
PV1||I|EMERGENCY^A1^01||||12345^DOCTOR^JOHN^MD|||||||||||V123456|||||||||||||||||||||||20250114120000
ORC|NW|ORD001|||CM|N||||20250114120000|^DOCTOR^JOHN^MD|12345^DOCTOR^JOHN^MD||||||
OBR|1|ORD001||LAB001^LABORATORIO COMPLETO^L|||20250114120000|||||||||^DOCTOR^JOHN^MD||||||20250114120000|||F
```

## Ver Mensajes Parseados

El Gateway muestra en los logs:
- **MSH**: Cabecera del mensaje
- **PID**: Datos del paciente (identificador, nombre, fecha nacimiento, teléfono)
- **ORC/OBR**: Orden y estudio solicitado
- **PV1**: Información de visita (si está presente)
- **DTOs construidos**: PatientUpsert y AppointmentCreate
- **ACK generado**: Respuesta al mensaje

## Debugging

Para ver más detalles, ajusta el nivel de logging en `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

