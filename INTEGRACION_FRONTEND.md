# Integración del Botón de Descarga HL7 en el Frontend

## ✅ Implementado

Se ha agregado un botón **"Descargar Resumen HL7"** en el modal de consulta del doctor.

## Ubicación del Botón

El botón aparece en el modal de consulta (`openEncounterModal` en `Frontend/js/doctor.js`), junto a los botones:
- Cancelar
- **Descargar Resumen HL7** ← NUEVO
- Recetar
- Guardar Consulta

## Funcionalidad

1. **Al hacer clic** en "Descargar Resumen HL7":
   - Intenta descargar el resumen por `appointmentId`
   - Si no encuentra, intenta por `patientId`
   - Descarga el archivo `.txt` con el resumen de la consulta HL7

2. **El archivo descargado** contiene:
   - Información del paciente (DNI, nombre, fecha nacimiento, teléfono, dirección)
   - Información de la cita/orden (fechas, motivo, IDs)
   - Estado del procesamiento (AA = exitoso, AE = error)

## API Endpoints Utilizados

- `GET /api/v1/Hl7Summary/by-appointment/{appointmentId}` - Descarga por ID de cita
- `GET /api/v1/Hl7Summary/by-patient/{patientId}` - Descarga por ID de paciente

## Configuración

El frontend busca el Hl7Gateway en:
- `http://localhost:5000/api`
- `http://127.0.0.1:5000/api`

Asegúrate de que el Gateway esté ejecutándose en el puerto 5000.

## Uso

1. El doctor abre una consulta
2. Hace clic en "Descargar Resumen HL7"
3. Se descarga automáticamente el archivo `.txt` con el resumen
4. El archivo se puede imprimir o adjuntar al historial del paciente

