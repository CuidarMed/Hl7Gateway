# Hl7Gateway

Gateway HL7 v2 para CuidarMed+ que recibe mensajes ORM^O01 por MLLP y los procesa llamando a las APIs REST internas.

## Características

- **Listener MLLP** en puerto 2575 usando TcpListener
- **Parser HL7 v2** con NHapi (soporta ORM^O01)
- **Generación de ACK** (AA/AE/AR) con eco del MSH-10
- **Mapping HL7 a DTOs**:
  - PID → PatientUpsert (DirectoryMS)
  - ORC/OBR → AppointmentCreate (SchedulingMS)
  - PV1 (opcional)
- **Integraciones HTTP** con DirectoryMS y SchedulingMS
- **Manejo de errores** con ACK AE y logging detallado

## Configuración

Editar `appsettings.json`:

```json
{
  "Mllp": {
    "Port": 2575
  },
  "Microservices": {
    "DirectoryMS": {
      "BaseUrl": "http://localhost:5001",
      "AuthToken": ""
    },
    "SchedulingMS": {
      "BaseUrl": "http://localhost:5002",
      "AuthToken": ""
    }
  }
}
```

## Docker

```bash
docker-compose up -d
```

Variables de entorno:
- `DIRECTORYMS_URL`: URL base de DirectoryMS
- `SCHEDULINGMS_URL`: URL base de SchedulingMS
- `AUTH_TOKEN`: Token Bearer opcional

## Uso

1. Iniciar el Gateway:
```bash
dotnet run
```

2. Enviar mensaje HL7 usando el Sender:
```bash
cd Sender
dotnet run
```

## Notas sobre NHapi

La API de NHapi puede variar según la versión. Si hay errores de compilación relacionados con el acceso a campos de PID, ORC, OBR, etc., puede ser necesario ajustar el código según la versión exacta de NHapi instalada.

Los métodos de acceso pueden ser:
- Propiedades directas: `pid.PatientIdentifierList[0]`
- Métodos Get: `pid.GetPatientIdentifierList(0)`
- Índices: `pid.GetField(3)`

Consulte la documentación de NHapi para su versión específica.

