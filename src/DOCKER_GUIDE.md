# Guía: Docker Compose con PostgreSQL y Migraciones

## Resumen de los Cambios Realizados

### ✅ Variable de Entorno para ConnectionString

ASP.NET Core, sobrescribe cualquier configuración del `appsettings.json` usando variables de entorno. La sintaxis es:
```
ConnectionStrings__NombreDeConexion
```

Los dos guiones bajos (`__`) reemplazan el `:` en la jerarquía JSON.

**Cambios en `docker-compose.yml`:**
```yaml
environment:
  - ConnectionStrings__LogisticsAndDeliveriesDatabase=Server=db;Port=5432;Database=api_db;Username=api_user;Password=ApiPass123;Include Error Detail=true
```

### ✅ Ejecutar Migraciones Automáticamente

Modificamos el código para que las migraciones se apliquen automáticamente cuando inicia el contenedor.

**Cambios realizados:**

1. **DependencyInjection.cs** - Agregamos método de extensión:
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
        logger.LogInformation("Migraciones aplicadas exitosamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al aplicar migraciones: {Message}", ex.Message);
        throw;
    }
}
```

2. **Program.cs** - Aplicamos las migraciones al iniciar y configuramos JSON:
```csharp
// Configurar para aceptar camelCase en JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

var app = builder.Build();

// Aplicar migraciones automáticamente al iniciar
await app.Services.ApplyMigrationsAsync();
```

3. **docker-compose.yml** - Agregamos healthcheck y depends_on:
```yaml
depends_on:
  db:
    condition: service_healthy
```

Esto asegura que el contenedor de la API espere a que PostgreSQL esté listo antes de intentar conectarse.

---

## Cómo Usar

### Paso 1: Reconstruir la imagen Docker

Después de los cambios en el código, necesitas reconstruir la imagen:

```bash
cd c:/Users/PC/Documents/Git/microservicios/ms2024-m2-tf-juanmmc/src
docker build -t juanmurielc/logisticsanddeliveries_webapi:1.0.0 -f Dockerfile .
```

O también:

```bash
docker image build -t juanmurielc/logisticsanddeliveries_webapi:4.0.0 .
```

Subir imagen a Docker Hub:

```bash
docker logout
docker login -u juanmurielc
docker image push -a juanmurielc/logisticsanddeliveries_webapi
```

### Paso 2: Iniciar los servicios

```bash
docker-compose up -d
```

O también:

```bash
docker compose up -d -p logisticsanddeliveries_webapi
```

### Paso 3: Ver los logs

Para verificar que las migraciones se aplicaron correctamente:

```bash
docker-compose logs api
```

Deberías ver un mensaje: `Migraciones aplicadas exitosamente.`

### Paso 4: Verificar la base de datos

Puedes conectarte a PostgreSQL para verificar las tablas:

```bash
docker exec -it db_container psql -U api_user -d api_db
```

Luego ejecuta:
```sql
\dt
```

Esto mostrará todas las tablas creadas por las migraciones.

---

## Comandos Útiles

### Ver todos los contenedores
```bash
docker-compose ps
```

### Detener los servicios
```bash
docker-compose down
```

### Detener y eliminar volúmenes (base de datos)
```bash
docker-compose down -v
```

### Ver logs en tiempo real
```bash
docker-compose logs -f
```

### Reiniciar solo un servicio
```bash
docker-compose restart api
```

## Cambios Adicionales Aplicados

Durante la configuración, se realizaron los siguientes ajustes:

### 1. Configuración JSON en Program.cs
Se agregó soporte para deserialización case-insensitive y camelCase:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
```

### 2. Modificación de CreatePackageCommand
Se cambió de constructor posicional a propiedades `init` para compatibilidad con JSON:
```csharp
// ANTES (no funcionaba con JSON deserialización)
public record CreatePackageCommand(Guid Id, string Number, ...) : IRequest<Result<Guid>>;

// DESPUÉS (funciona correctamente)
public record CreatePackageCommand : IRequest<Result<Guid>>
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    // ...
}
```

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
