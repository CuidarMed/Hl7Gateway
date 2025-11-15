# Archivos Generados por el Gateway

El Gateway genera automáticamente archivos de texto con los mensajes HL7 procesados.

## Ubicación

Por defecto, los archivos se guardan en la carpeta `logs/` dentro del proyecto.

Puedes cambiar la ubicación editando `appsettings.json`:

```json
{
  "Logging": {
    "LogDirectory": "ruta/personalizada"
  }
}
```

## Tipos de Archivos Generados

### 1. Mensajes Recibidos
**Formato:** `RECIBIDO_YYYYMMDD_HHMMSS.txt`

Contiene el mensaje HL7 completo recibido.

**Ejemplo:**
```
========================================
MENSAJE HL7 - RECIBIDO
Fecha/Hora: 2025-01-14 12:00:00
========================================

MENSAJE HL7 COMPLETO:
----------------------------------------
MSH|^~\&|SENDING_APP|SENDING_FACILITY|...
PID|||12345678^^^DNI||GARCIA^JUAN^MARIA||...
----------------------------------------
```

### 2. Resúmenes de Consulta
**Formato:** `RESUMEN_MSG001_YYYYMMDD_HHMMSS.txt`

Contiene un resumen legible de la consulta procesada.

**Ejemplo:**
```
========================================
RESUMEN DE CONSULTA HL7
========================================
Fecha/Hora: 2025-01-14 12:00:00
Message Control ID: MSG001
Estado: PROCESADO EXITOSAMENTE

INFORMACIÓN DEL PACIENTE:
----------------------------------------
DNI: 12345678
Nombre: JUAN GARCIA
Fecha Nacimiento: 1985-01-15
Teléfono: 5551234567
Dirección: 123 CALLE PRINCIPAL, CIUDAD, PROVINCIA
PatientId: 123

INFORMACIÓN DE LA CITA/ORDEN:
----------------------------------------
PatientId: 123
DoctorId: 1
Fecha/Hora Inicio: 2025-01-15 12:00:00
Fecha/Hora Fin: 2025-01-15 13:00:00
Motivo: Estudio: LAB001
AppointmentId: 456

========================================
ACK Code: AA
========================================
```

### 3. ACKs Generados
**Formato:** `ACK_AA_YYYYMMDD_HHMMSS.txt` o `ACK_AE_YYYYMMDD_HHMMSS.txt`

Contiene el ACK (confirmación) enviado al sistema externo.

**Ejemplo:**
```
========================================
MENSAJE HL7 - ACK_AA
Fecha/Hora: 2025-01-14 12:00:01
========================================

MENSAJE HL7 COMPLETO:
----------------------------------------
MSH|^~\&|HL7GATEWAY|HL7GATEWAY|...
MSA|AA|MSG001|Mensaje procesado exitosamente
----------------------------------------

RESUMEN DE LA CONSULTA:
----------------------------------------
Mensaje procesado exitosamente
----------------------------------------
```

## Uso del Resumen en Consultas

Los archivos `RESUMEN_*.txt` están diseñados para ser incluidos en el resumen de la consulta médica.

### Características:
- ✅ Formato legible y estructurado
- ✅ Incluye toda la información relevante del paciente
- ✅ Incluye información de la cita/orden creada
- ✅ Muestra el estado del procesamiento
- ✅ Fácil de imprimir o adjuntar a historiales

### Ejemplo de Uso:

1. **Guardar en historial del paciente:**
   - Copiar el archivo `RESUMEN_*.txt` a la carpeta del paciente
   - Adjuntar al sistema de historiales clínicos

2. **Imprimir para consulta:**
   - Abrir el archivo `RESUMEN_*.txt`
   - Imprimir directamente

3. **Integrar con sistema:**
   - Leer el archivo `RESUMEN_*.txt`
   - Parsear la información
   - Importar al sistema de gestión

## Desactivar Generación de Archivos

Si no quieres que se generen archivos, edita `appsettings.json`:

```json
{
  "Logging": {
    "EnableFileLogging": false
  }
}
```

## Limpieza de Archivos Antiguos

Los archivos se acumulan con el tiempo. Puedes:

1. **Eliminar manualmente:**
   ```powershell
   Remove-Item logs\*.txt
   ```

2. **Configurar rotación automática** (futuro):
   - Implementar limpieza de archivos mayores a X días
   - Comprimir archivos antiguos

## Estructura de Carpetas

```
Hl7Gateway/
├── logs/
│   ├── RECIBIDO_20250114_120000.txt
│   ├── RESUMEN_MSG001_20250114_120001.txt
│   ├── ACK_AA_20250114_120001.txt
│   └── ...
```

