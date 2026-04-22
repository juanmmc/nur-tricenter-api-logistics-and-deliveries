[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/rVhxZd2x)

# Microservicio "Logística y Entregas"

Microservicio para gestionar operaciones de reparto (entregas) de paquetes alimenticios. Modela el ciclo de vida de un paquete, la actividad de repartidores y su planificación de rutas.

## Funcionalidades principales:

### Gestión de repartidores (Drivers)
- Crear un repartidor.
- Registrar ubicación del repartidor.
- Listar repartidores.
- Obtener repartidor.

### Gestión de paquetes (Package)
- Crear paquete con información del paciente, dirección de entrega y asignación de driver.
- Cambiar estado: marcar en tránsito (si tiene orden asignado), entregado (con evidencia), fallido, cancelar.
- Registrar incidentes de entrega (solo si el paquete está en estado Failed).
- Registrar orden para planificación de entrega.
- Listar entregas asignadas a un repartidor por fecha y ordenados según el orden definido.
 
## Ejecutar la API desde consola

dotnet run --project src/LogisticsAndDeliveries.WebApi/LogisticsAndDeliveries.WebApi.csproj

## Ejecución del entorno con Docker Compose

Ubicarse en la carpeta src Para levantar el entorno de trabajo completo:

```bash
docker compose --project-name logisticsanddeliveries_webapi up -d
```

Para detener el entorno:

```bash
docker compose --project-name logisticsanddeliveries_webapi down
```

# Diagrama de Clases

![Class diagram](./img/class_diagram.png)

## Documentacion API para Frontend

Para el equipo frontend, la documentacion detallada de endpoints de Drivers y Packages (metodos GET/POST, parametros requeridos, respuestas y reglas de negocio) esta en:

- [Guia API Frontend - Driver y Package](./src/LogisticsAndDeliveries/LogisticsAndDeliveries.WebApi/FRONTEND_API_DRIVER_PACKAGE.md)