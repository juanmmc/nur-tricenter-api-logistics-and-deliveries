# Guia de API para Frontend

Esta guia documenta los endpoints expuestos por:
- `DriverController`
- `PackageController`

Base path comun:
- `/api/Driver`
- `/api/Package`

## Convenciones generales

- Todos los endpoints actuales usan `GET` o `POST`.
- Cuando una operacion es exitosa, la API responde con `200 OK`.
- Respuestas exitosas pueden devolver:
  - Un objeto (por ejemplo, `DriverDto`, `PackageDto`).
  - Un arreglo de objetos.
  - Un `Guid` (creaciones).
  - `true` (operaciones de cambio de estado/actualizacion).

### Formato de error

En caso de error, la API devuelve un objeto con este formato:

```json
{
  "code": "Delivery.InvalidStatusTransition",
  "description": "La transicion de estado no es valida.",
  "type": 0,
  "structuredMessage": "La transicion de estado no es valida."
}
```

Mapeo de tipo de error a HTTP:
- `NotFound` (`type = 3`) -> `404 Not Found`
- `Conflict` (`type = 4`) -> `409 Conflict`
- `Failure` o `Validation` (`type = 0/1`) -> `400 Bad Request`
- `Problem` (`type = 2`) -> `500 Internal Server Error`

## DriverController

Ruta base: `/api/Driver`

### 1) Crear Driver
- Metodo: `POST`
- URL: `/api/Driver/createDriver`
- Body requerido:

```json
{
  "name": "Juan Perez"
}
```

Campos:
- `name` (string, requerido, no vacio)

Respuesta exitosa:
- `200 OK` con body `Guid` del driver creado.

Ejemplo:
```json
"f9f4f2d6-c6ba-4c22-a3db-303d9f8b8d20"
```

Errores comunes:
- `400 Bad Request` si `name` esta vacio.

### 2) Obtener Driver por Id
- Metodo: `GET`
- URL: `/api/Driver/getDriver?driverId={GUID}`
- Query params requeridos:
- `driverId` (Guid)

Respuesta exitosa:
- `200 OK` con `DriverDto`.

```json
{
  "id": "f9f4f2d6-c6ba-4c22-a3db-303d9f8b8d20",
  "name": "Juan Perez",
  "latitude": -16.5,
  "longitude": -68.15,
  "lastLocationUpdate": "2026-04-20T14:03:57.315Z"
}
```

Errores comunes:
- `404 Not Found` si no existe el driver.

### 3) Actualizar ubicacion del Driver
- Metodo: `POST`
- URL: `/api/Driver/updateDriverLocation`
- Body requerido:

```json
{
  "driverId": "f9f4f2d6-c6ba-4c22-a3db-303d9f8b8d20",
  "latitude": -16.5,
  "longitude": -68.15
}
```

Campos:
- `driverId` (Guid, requerido)
- `latitude` (double, rango valido: `-90` a `90`)
- `longitude` (double, rango valido: `-180` a `180`)

Respuesta exitosa:
- `200 OK` con body `true`.

Errores comunes:
- `404 Not Found` si no existe el driver.
- `400 Bad Request` si latitud/longitud estan fuera de rango.

### 4) Listar Drivers
- Metodo: `GET`
- URL: `/api/Driver/getDrivers`
- Parametros: ninguno

Respuesta exitosa:
- `200 OK` con arreglo de `DriverDto`.

```json
[
  {
    "id": "f9f4f2d6-c6ba-4c22-a3db-303d9f8b8d20",
    "name": "Juan Perez",
    "latitude": -16.5,
    "longitude": -68.15,
    "lastLocationUpdate": "2026-04-20T14:03:57.315Z"
  }
]
```

## PackageController

Ruta base: `/api/Package`

### Modelo principal de respuesta (`PackageDto`)

```json
{
  "id": "d22f6e67-8ce8-4e57-a4a1-23df75c9c176",
  "driverId": "f9f4f2d6-c6ba-4c22-a3db-303d9f8b8d20",
  "number": "PKG-01",
  "patientId": "70a29f3a-660f-4416-8ed8-5de74fd3f3b4",
  "patientName": "Ana Perez",
  "patientPhone": "77912345",
  "deliveryAddress": "Calle Falsa 123",
  "deliveryLatitude": -16.5,
  "deliveryLongitude": -68.15,
  "deliveryDate": "2026-04-20",
  "deliveryEvidence": "base64-o-url",
  "deliveryOrder": 1,
  "deliveryStatus": "InTransit",
  "incidentType": "PatientAbsent",
  "incidentDescription": "No se encontro al paciente",
  "updatedAt": "2026-04-20T14:03:57.315Z"
}
```

Valores de `deliveryStatus`:
- `Pending`
- `InTransit`
- `Completed`
- `Failed`
- `Cancelled`

Valores de `incidentType`:
- `PatientAbsent`
- `IncorrectAddress`
- `RefusedByReceiver`
- `WeatherIssue`
- `Other`

### 1) Crear Package
- Metodo: `POST`
- URL: `/api/Package/createPackage`
- Body requerido:

```json
{
  "id": "d22f6e67-8ce8-4e57-a4a1-23df75c9c176",
  "number": "PKG-01",
  "patientId": "70a29f3a-660f-4416-8ed8-5de74fd3f3b4",
  "patientName": "Ana Perez",
  "patientPhone": "77912345",
  "deliveryAddress": "Calle Falsa 123",
  "deliveryLatitude": -16.5,
  "deliveryLongitude": -68.15,
  "deliveryDate": "2026-04-20",
  "driverId": "f9f4f2d6-c6ba-4c22-a3db-303d9f8b8d20"
}
```

Campos requeridos:
- `id` (Guid)
- `number` (string no vacio)
- `patientId` (Guid no vacio)
- `patientName` (string no vacio)
- `patientPhone` (string no vacio)
- `deliveryAddress` (string no vacio)
- `deliveryLatitude` (double entre `-90` y `90`)
- `deliveryLongitude` (double entre `-180` y `180`)
- `deliveryDate` (DateOnly, formato `yyyy-MM-dd`, no puede ser fecha pasada)
- `driverId` (Guid no vacio)

Respuesta exitosa:
- `200 OK` con body `Guid` del package.

Errores comunes:
- `400 Bad Request` por campos invalidos.

Nota:
- Si ya existe un package con el mismo `id`, la API responde `200 OK` con ese mismo `id`.

### 2) Obtener Package por Id
- Metodo: `GET`
- URL: `/api/Package/getPackage?packageId={GUID}`
- Query params requeridos:
- `packageId` (Guid)

Respuesta exitosa:
- `200 OK` con `PackageDto`.

Errores comunes:
- `404 Not Found` si no existe el package.

### 3) Listar Packages por Driver y Fecha
- Metodo: `GET`
- URL: `/api/Package/getPackagesByDriverAndDeliveryDate?driverId={GUID}&deliveryDate={yyyy-MM-dd}`
- Query params requeridos:
- `driverId` (Guid)
- `deliveryDate` (DateOnly en formato `yyyy-MM-dd`)

Respuesta exitosa:
- `200 OK` con arreglo de `PackageDto`, ordenado por `deliveryOrder` ascendente.

### 4) Definir orden de entrega
- Metodo: `POST`
- URL: `/api/Package/setDeliveryOrder`
- Body requerido:

```json
{
  "packageId": "d22f6e67-8ce8-4e57-a4a1-23df75c9c176",
  "deliveryOrder": 1
}
```

Reglas:
- `deliveryOrder` debe ser mayor a 0.
- Solo se puede definir orden cuando el paquete esta en estado `Pending`.

Respuesta exitosa:
- `200 OK` con body `true`.

Errores comunes:
- `404 Not Found` si no existe el package.
- `400 Bad Request` por orden invalido o transicion de estado no permitida.

### 5) Cancelar entrega
- Metodo: `POST`
- URL: `/api/Package/cancelDelivery`
- Body requerido:

```json
{
  "packageId": "d22f6e67-8ce8-4e57-a4a1-23df75c9c176"
}
```

Reglas:
- No se puede cancelar si ya esta `Completed`.

Respuesta exitosa:
- `200 OK` con body `true`.

Errores comunes:
- `404 Not Found` si no existe el package.
- `400 Bad Request` si intenta cancelarse uno ya completado.

### 6) Marcar entrega fallida
- Metodo: `POST`
- URL: `/api/Package/markDeliveryFailed`
- Body requerido:

```json
{
  "packageId": "d22f6e67-8ce8-4e57-a4a1-23df75c9c176"
}
```

Reglas:
- Solo se puede pasar a `Failed` desde `InTransit`.

Respuesta exitosa:
- `200 OK` con body `true`.

Errores comunes:
- `404 Not Found` si no existe el package.
- `400 Bad Request` por transicion de estado invalida.

### 7) Marcar entrega en transito
- Metodo: `POST`
- URL: `/api/Package/markDeliveryInTransit`
- Body requerido:

```json
{
  "packageId": "d22f6e67-8ce8-4e57-a4a1-23df75c9c176"
}
```

Reglas:
- `deliveryOrder` debe ser mayor a 0.
- Solo se puede pasar a `InTransit` desde `Pending`.

Respuesta exitosa:
- `200 OK` con body `true`.

Errores comunes:
- `404 Not Found` si no existe el package.
- `400 Bad Request` por orden invalido o transicion no permitida.

### 8) Marcar entrega completada
- Metodo: `POST`
- URL: `/api/Package/markDeliveryCompleted`
- Body requerido:

```json
{
  "packageId": "d22f6e67-8ce8-4e57-a4a1-23df75c9c176",
  "deliveryEvidence": "base64-o-url"
}
```

Reglas:
- `deliveryEvidence` es obligatorio y no vacio.
- Solo se puede pasar a `Completed` desde `InTransit`.

Respuesta exitosa:
- `200 OK` con body `true`.

Errores comunes:
- `404 Not Found` si no existe el package.
- `400 Bad Request` por evidencia vacia o transicion invalida.

### 9) Registrar incidente de entrega
- Metodo: `POST`
- URL: `/api/Package/registerDeliveryIncident`
- Body requerido:

```json
{
  "packageId": "d22f6e67-8ce8-4e57-a4a1-23df75c9c176",
  "incidentType": "PatientAbsent",
  "incidentDescription": "No se encontro al paciente"
}
```

Importante para serializacion de enum:
- `incidentType` es enum. Dependiendo de la configuracion de serializacion del API, puede aceptarse como string (`"PatientAbsent"`) o entero (`0`).
- Recomendacion frontend: enviar string semantico si el backend lo acepta.

Reglas:
- `incidentDescription` obligatorio.
- Solo se puede registrar incidente cuando `deliveryStatus` es `Failed`.

Respuesta exitosa:
- `200 OK` con body `true`.

Errores comunes:
- `404 Not Found` si no existe el package.
- `400 Bad Request` por descripcion vacia o estado invalido.

## Flujo recomendado para frontend (estado de entrega)

Secuencia valida tipica:
1. Crear package (`Pending`).
2. Definir `deliveryOrder` (> 0).
3. Marcar `InTransit`.
4. Cerrar en `Completed` con evidencia, o en `Failed`.
5. Si esta `Failed`, registrar incidente.

Transiciones clave a bloquear en UI:
- No permitir `InTransit` sin `deliveryOrder`.
- No permitir `Completed` sin evidencia.
- No permitir incidente si no esta en `Failed`.
- No permitir cancelar cuando ya esta `Completed`.
