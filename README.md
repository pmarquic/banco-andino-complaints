# Banco Andino - Sistema de Gestión de Reclamos Bancarios

Sistema completo de gestión de reclamos bancarios construido con Azure Functions .NET 10 (Isolated Worker Model) usando arquitectura de microservicios.

## 🏗️ Arquitectura del Sistema

El sistema está compuesto por 4 proyectos principales:

### 1. **BancoAndino.Complaints.Shared** (Class Library)
Biblioteca compartida con modelos de dominio, DTOs, DbContext e interfaces.

**Componentes:**
- **Models**: Entidades del dominio (Complaint, ComplaintStatus, ComplaintCategory, Evidence, ComplaintHistory)
- **DTOs**: Objetos de transferencia de datos usando records
- **Data**: ComplaintsDbContext con Entity Framework Core 9.0
- **Interfaces**: Contratos para repositorios y servicios

### 2. **BancoAndino.Complaints.API** (Azure Functions)
API REST para operaciones CRUD de reclamos.

**Endpoints HTTP:**
- `GET /api/complaints` - Listar reclamos con filtros
- `GET /api/complaints/{id}` - Obtener reclamo por ID
- `POST /api/complaints` - Crear nuevo reclamo
- `PUT /api/complaints/{id}` - Actualizar reclamo
- `PATCH /api/complaints/{id}/status` - Cambiar estado de reclamo
- `DELETE /api/complaints/{id}` - Eliminar reclamo
- `GET /api/complaints/{id}/evidences` - Obtener evidencias
- `POST /api/complaints/{id}/evidences` - Subir evidencia

**Servicios:**
- ComplaintRepository (Dapper + EF Core)
- StorageService (Azure Blob Storage)
- CacheService (Redis)

### 3. **BancoAndino.Complaints.EvidenceProcessor** (Azure Functions)
Procesador de evidencias con Event Grid trigger.

**Funcionalidad:**
- Trigger: Event Grid cuando se sube archivo a Blob Storage
- Valida tipo de archivo (PDF, JPG, PNG)
- Valida tamaño máximo (10MB)
- Extrae metadata y actualiza base de datos
- Elimina archivos inválidos

### 4. **BancoAndino.Complaints.Processor** (Azure Functions)
Procesador asíncrono de reclamos con Service Bus trigger.

**Funcionalidad:**
- Trigger: Service Bus Queue (`complaint-processing`)
- Asignación automática basada en categoría
- Envío de notificaciones
- Actualización de estados
- Registro de historial

## 🚀 Tecnologías Utilizadas

- **.NET 10.0** - Framework principal
- **Azure Functions v4** - Isolated Worker Model
- **Entity Framework Core 9.0** - ORM
- **Dapper 2.1.35** - Micro-ORM para consultas optimizadas
- **SQL Server** - Base de datos
- **Azure Blob Storage 12.22.2** - Almacenamiento de evidencias
- **Azure Service Bus 7.18.2** - Mensajería asíncrona
- **Redis (StackExchange.Redis 2.8.16)** - Cache distribuido
- **Serilog 4.1.0** - Logging estructurado
- **FluentValidation 11.11.0** - Validación de modelos

## 📋 Requisitos Previos

- .NET 10.0 SDK
- SQL Server (LocalDB o instancia completa)
- Azure Storage Emulator o cuenta de Azure Storage
- Redis Server (local o Azure Redis Cache)
- Azure Service Bus (o emulador)
- Visual Studio 2022 o VS Code con extensión de Azure Functions

## ⚙️ Configuración

### 1. Base de Datos

Las cadenas de conexión se configuran en `local.settings.json` de cada proyecto:

```json
{
  "SqlConnectionString": "Server=(localdb)\\mssqllocaldb;Database=BancoAndinoComplaints;Trusted_Connection=True;"
}
```

**Crear la base de datos:**
```bash
# Navegar al proyecto Shared
cd BancoAndino.Complaints.Shared

# Agregar migración inicial
dotnet ef migrations add InitialCreate --startup-project ../BancoAndino.Complaints.API

# Aplicar migración
dotnet ef database update --startup-project ../BancoAndino.Complaints.API
```

### 2. Azure Storage

Para desarrollo local, usar Azure Storage Emulator:
```json
{
  "BlobStorageConnectionString": "UseDevelopmentStorage=true"
}
```

Para producción, usar cadena de conexión real:
```json
{
  "BlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
}
```

### 3. Redis Cache

Instalar Redis localmente o usar cadena de conexión a Azure Redis:
```json
{
  "RedisConnectionString": "localhost:6379"
}
```

### 4. Azure Service Bus

Configurar la cadena de conexión en cada proyecto que la necesite:
```json
{
  "ServiceBusConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=..."
}
```

**Crear la cola:**
```bash
# Usando Azure CLI
az servicebus queue create --resource-group <resource-group> --namespace-name <namespace> --name complaint-processing
```

## 🔧 Compilación y Ejecución

### Compilar toda la solución
```bash
dotnet build BancoAndino.Complaints.sln
```

### Ejecutar la API
```bash
cd BancoAndino.Complaints.API
func start
```

### Ejecutar el procesador de evidencias
```bash
cd BancoAndino.Complaints.EvidenceProcessor
func start
```

### Ejecutar el procesador de reclamos
```bash
cd BancoAndino.Complaints.Processor
func start
```

## 📊 Modelo de Datos

### Complaint (Reclamo)
- ComplaintId (PK)
- CustomerId
- Title
- Description
- StatusId (FK)
- CategoryId (FK)
- Priority
- AssignedTo
- CreatedAt, UpdatedAt
- SlaDeadline, ResolvedAt
- ResolutionNotes

### ComplaintStatus (Estados)
1. New - Nuevo
2. In Progress - En progreso
3. Pending Customer - Esperando cliente
4. Resolved - Resuelto
5. Closed - Cerrado

### ComplaintCategory (Categorías)
1. Account Issues - Problemas de cuenta (24h SLA)
2. Card Problems - Problemas de tarjeta (24h SLA)
3. Transaction Dispute - Disputa de transacción (48h SLA)
4. Service Quality - Calidad de servicio (72h SLA)
5. Other - Otros (96h SLA)

### Evidence (Evidencia)
- EvidenceId (PK)
- ComplaintId (FK)
- BlobUrl
- FileName, FileSize, ContentType
- UploadedAt, UploadedBy

### ComplaintHistory (Historial)
- HistoryId (PK)
- ComplaintId (FK)
- OldStatusId, NewStatusId
- ChangedBy, Notes
- ChangedAt

## 🔐 Seguridad

- **Authentication**: Azure Functions usa AuthorizationLevel.Function
- **Data Protection**: Nullable reference types habilitado
- **Validation**: FluentValidation para validación de entrada
- **SQL Injection**: Uso de Dapper con parámetros
- **File Upload**: Validación de tipos y tamaños de archivo

## 📈 Logging y Monitoreo

- **Serilog** para logging estructurado en consola
- **Application Insights** para telemetría y monitoreo
- Logs de todas las operaciones críticas
- Manejo de excepciones con registro detallado

## 🧪 Testing

```bash
# Ejecutar tests (si existen)
dotnet test
```

## 🚀 Deployment

### Azure Portal
1. Crear Function Apps para cada proyecto
2. Configurar Connection Strings y App Settings
3. Publicar usando Visual Studio o Azure CLI

### Azure CLI
```bash
# Crear Resource Group
az group create --name rg-complaints --location eastus

# Crear Storage Account
az storage account create --name stcomplaints --resource-group rg-complaints

# Crear Function Apps
az functionapp create --name func-complaints-api --resource-group rg-complaints --consumption-plan-location eastus --runtime dotnet-isolated --runtime-version 10 --functions-version 4 --storage-account stcomplaints

# Deploy
func azure functionapp publish func-complaints-api
```

## 📝 Ejemplos de Uso

### Crear un reclamo
```bash
curl -X POST http://localhost:7071/api/complaints \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST001",
    "title": "Problema con tarjeta",
    "description": "No puedo usar mi tarjeta de débito",
    "categoryId": 2,
    "priority": "High"
  }'
```

### Obtener reclamos
```bash
curl http://localhost:7071/api/complaints?statusId=1
```

### Actualizar estado
```bash
curl -X PATCH http://localhost:7071/api/complaints/1/status \
  -H "Content-Type: application/json" \
  -d '{
    "statusId": 2,
    "changedBy": "admin@bancoandino.com",
    "notes": "Asignado a especialista"
  }'
```

## 🤝 Contribución

1. Fork el proyecto
2. Crear una rama de feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Crear Pull Request

## 📄 Licencia

Este proyecto es privado y propiedad de Banco Andino.

## 📞 Soporte

Para soporte técnico, contactar a: soporte@bancoandino.com
