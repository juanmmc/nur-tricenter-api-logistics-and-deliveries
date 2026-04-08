# Guía: Docker Compose con PostgreSQL, RabbitMQ y Migraciones

## Arquitectura de los servicios

El stack Docker expone los siguientes componentes:

| Servicio | Imagen | Puerto | Descripción |
|---|---|---|---|
| `api` | `juanmurielc/logisticsanddeliveries_webapi` | `9007:80` | WebApi ASP.NET 8 |
| `db` | `postgres:13.22` | interno | Base de datos PostgreSQL |

La API se conecta a un **RabbitMQ externo** (configurable vía `appsettings.json` o variables de entorno) que no forma parte del `docker-compose.yml`.

---

## Configuración actual — `docker-compose.yml`

```yaml
services:
  api:
    image: juanmurielc/logisticsanddeliveries_webapi:${IMAGE_TAG:-4.0.0}
    container_name: api_container
    ports:
      - 9007:80
    environment:
      - ConnectionStrings__LogisticsAndDeliveriesDatabase=Server=db;Port=5432;Database=api_db;Username=api_user;Password=ApiPass123;Include Error Detail=true
    volumes:
      - api_data:/app/data
    depends_on:
      db:
        condition: service_healthy
  db:
    image: postgres:13.22
    container_name: db_container
    environment:
      - POSTGRES_USER=api_user
      - POSTGRES_PASSWORD=ApiPass123
      - POSTGRES_DB=api_db
    volumes:
      - db_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U api_user -d api_db"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  api_data:
  db_data:
```

### Variable de entorno para ConnectionString

ASP.NET Core sobrescribe cualquier configuración del `appsettings.json` usando variables de entorno. Los dos guiones bajos (`__`) reemplazan el `:` en la jerarquía JSON:

```
ConnectionStrings__LogisticsAndDeliveriesDatabase=Server=db;...
```

### Tag de imagen dinámica

El campo `image` usa `${IMAGE_TAG:-4.0.0}`, lo que permite sobrescribir la versión sin editar el archivo:

```bash
IMAGE_TAG=5.0.0 docker compose up -d
```

---

## Migraciones automáticas al iniciar

Al arrancar el contenedor, `Program.cs` llama a `ApplyMigrationsAsync()` antes de procesar peticiones:

```csharp
var app = builder.Build();

await app.Services.ApplyMigrationsAsync();
```

`ApplyMigrationsAsync` (en `LogisticsAndDeliveries.Infrastructure/DependencyInjection.cs`) aplica las migraciones de EF Core **y** crea la tabla `outbox_message` con su índice si aún no existen:

```csharp
public static async Task ApplyMigrationsAsync(this IServiceProvider services)
{
    using var scope = services.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migrations");

    try
    {
        var context = serviceProvider.GetRequiredService<PersistenceDbContext>();
        await context.Database.MigrateAsync();

        await context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS outbox_message (
                id uuid NOT NULL,
                eventname text NOT NULL,
                type text NOT NULL,
                content text NOT NULL,
                occurredonutc timestamp with time zone NOT NULL,
                processedonutc timestamp with time zone NULL,
                error text NULL,
                CONSTRAINT PK_outbox_message PRIMARY KEY (id)
            );");

        await context.Database.ExecuteSqlRawAsync(@"
            CREATE INDEX IF NOT EXISTS IX_outbox_message_processedOnUtc_occurredOnUtc
            ON outbox_message (processedonutc, occurredonutc);");

        logger.LogInformation("Migraciones aplicadas exitosamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al aplicar migraciones: {Message}", ex.Message);
        throw;
    }
}
```

El `depends_on` con `condition: service_healthy` garantiza que PostgreSQL esté listo antes de que la API intente conectarse.

---

## Mensajería: RabbitMQ y patrón Outbox

La infraestructura registra dos `BackgroundService` en `DependencyInjection.cs`:

```csharp
services.AddHostedService<PackageDispatchCreatedConsumer>();
services.AddHostedService<OutboxPublisherService>();
```

| Servicio | Rol |
|---|---|
| `PackageDispatchCreatedConsumer` | Consume eventos de la cola `produccion.paquete-despacho-creado` y crea el paquete correspondiente |
| `OutboxPublisherService` | Lee filas de `outbox_message` y las publica al exchange `outbox.events` con el routing key `logistica.paquete.estado-actualizado` |

### Configuración de RabbitMQ (`appsettings.json`)

```json
"RabbitMq": {
  "HostName": "154.38.180.80",
  "Port": 5672,
  "UserName": "admin",
  "Password": "rabbit_mq",
  "VirtualHost": "/",
  "ExchangeName": "outbox.events",
  "InputQueueName": "produccion.paquete-despacho-creado",
  "InputRoutingKey": "produccion.paquete-despacho-creado",
  "OutputRoutingKey": "logistica.paquete.estado-actualizado",
  "DeclareTopology": false,
  "ReconnectDelaySeconds": 10,
  "PrefetchCount": 10,
  "OutboxBatchSize": 50,
  "OutboxPublishIntervalSeconds": 5
}
```

Para sobrescribir en Docker usa variables de entorno con doble guión bajo:

```
RabbitMq__HostName=mi-servidor
RabbitMq__Password=mi-password
```

---

## Cómo usar

### Paso 1: Construir la imagen Docker

Desde la carpeta `src/`:

```bash
docker build -t juanmurielc/logisticsanddeliveries_webapi:4.0.0 -f Dockerfile .
```

Subir imagen a Docker Hub:

```bash
docker logout
docker login -u juanmurielc
docker image push -a juanmurielc/logisticsanddeliveries_webapi
```

### Paso 2: Iniciar los servicios

```bash
docker compose up -d
```

Con tag personalizado:

```bash
IMAGE_TAG=4.0.0 docker compose up -d
```

Con nombre de proyecto explícito:

```bash
docker compose up -d -p logisticsanddeliveries_webapi
```

### Paso 3: Ver los logs

Para verificar que las migraciones se aplicaron correctamente:

```bash
docker compose logs api
```

Deberías ver: `Migraciones aplicadas exitosamente.`

### Paso 4: Verificar la base de datos

```bash
docker exec -it db_container psql -U api_user -d api_db
```

Luego:
```sql
\dt
```

Mostrará todas las tablas, incluida `outbox_message`.

---

## Comandos útiles

| Acción | Comando |
|---|---|
| Ver contenedores activos | `docker compose ps` |
| Ver logs en tiempo real | `docker compose logs -f` |
| Detener servicios | `docker compose down` |
| Detener y eliminar volúmenes | `docker compose down -v` |
| Reiniciar solo la API | `docker compose restart api` |
| Ver logs solo de la API | `docker compose logs -f api` |

---

## Notas Importantes

- El `appsettings.json` mantiene la configuración de desarrollo local
- Las variables de entorno en Docker **sobrescriben** las del `appsettings.json`
- La API acepta JSON en **camelCase** (ej: `patientName`, no `PatientName`)
- Si se cambia el esquema de la base de datos, se necesita:
  1. Crear una nueva migración en la máquina local
  2. Reconstruir la imagen Docker
  3. Reiniciar los contenedores

---

## Troubleshooting

### Si la API no puede conectarse a la DB:
```bash
# Verificar que PostgreSQL esté corriendo
docker-compose logs db

# Reiniciar los servicios
docker-compose restart
```

### Si las migraciones fallan:
```bash
# Ver los logs detallados
docker-compose logs api

# Eliminar todo y empezar de nuevo
docker-compose down -v
docker-compose up -d
```

### Ejemplo de prueba exitosa:

```bash
curl -X 'POST' \
  'http://localhost/api/Package/createPackage' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afe1",
  "number": "PKG-001",
  "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afc1",
  "patientName": "Dana Muriel",
  "patientPhone": "12312323",
  "deliveryAddress": "Urb. Palmas del Norte, calle Cedro",
  "deliveryLatitude": 2222,
  "deliveryLongitude": 3333,
  "scheduledDate": "2025-11-08",
  "driverId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}'
```

**Nota:** Asegúrate de usar GUIDs válidos (solo caracteres hexadecimales: 0-9, a-f)

### Para probar la API:
```bash
curl http://localhost/swagger
```
