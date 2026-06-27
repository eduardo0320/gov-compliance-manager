# 📋 ARQUITECTURA DEL PROYECTO - Sistema de Normas MICITT

> **IMPORTANTE**: Este documento define la estructura y convenciones del proyecto. DEBE ser consultado antes de realizar cualquier modificación al código.

## 📁 Estructura General del Proyecto

```
sistema-normas-micitt/
├── backend/                      # API REST en ASP.NET Core (.NET 9.0)
│   ├── Controllers/             # Controladores de API
│   ├── Data/                    # Contexto de Entity Framework
│   ├── DTOs/                    # Data Transfer Objects
│   ├── Migrations/              # Migraciones de Entity Framework
│   ├── Models/                  # Entidades de dominio
│   ├── Repositories/            # Patrón Repository
│   │   ├── Interfaces/         # Contratos de repositorios
│   │   └── Implementations/    # Implementaciones de repositorios
│   ├── Services/               # Lógica de negocio
│   │   ├── Interfaces/        # Contratos de servicios
│   │   └── Implementations/   # Implementaciones de servicios
│   ├── Scripts/               # Scripts SQL
│   ├── Tests/                 # Pruebas unitarias
│   ├── Program.cs            # Punto de entrada
│   ├── Startup.cs            # Configuración de la aplicación
│   └── appsettings.json      # Configuración
│
└── frontend/                    # Aplicación React
    ├── src/
    │   ├── components/         # Componentes reutilizables
    │   ├── contexts/          # Context API de React
    │   ├── layouts/           # Layouts de página
    │   ├── pages/             # Páginas de la aplicación
    │   ├── App.jsx           # Componente raíz
    │   └── main.jsx          # Punto de entrada
    ├── public/
    └── package.json
```

---

## 🎯 BACKEND - ASP.NET Core

### Tecnologías y Versiones

- **.NET**: 9.0
- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 9.0.9
- **Base de Datos**: MySQL (Pomelo.EntityFrameworkCore.MySql 9.0.0)
- **Autenticación**: JWT (Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0)
- **Hashing**: BCrypt.Net-Next 4.0.3

### 🏗️ Arquitectura en Capas

El backend sigue una arquitectura en capas con el patrón Repository y Service:

```
┌─────────────────────────────────────────┐
│          CONTROLLERS                     │  ← Capa de Presentación (API)
├─────────────────────────────────────────┤
│           SERVICES                       │  ← Lógica de Negocio
├─────────────────────────────────────────┤
│         REPOSITORIES                     │  ← Acceso a Datos
├─────────────────────────────────────────┤
│      ENTITY FRAMEWORK (ORM)             │  ← Mapeo Objeto-Relacional
├─────────────────────────────────────────┤
│         BASE DE DATOS (MySQL)           │  ← Persistencia
└─────────────────────────────────────────┘
```

---

## 📂 ESTRUCTURA DETALLADA DEL BACKEND

### 1️⃣ **Controllers/** - Capa de Presentación

**Propósito**: Manejar las peticiones HTTP y devolver respuestas

**Namespace**: `backend.Controllers`

**Archivos**:

- `ActividadesController.cs` - CRUD de actividades
- `AutenticacionController.cs` - Login, registro, recuperación de contraseña
- `DominiosController.cs` - Gestión de dominios
- `EstadisticasController.cs` - Endpoints de estadísticas
- `LogsController.cs` - Auditoría y logs
- `ProcesosController.cs` - Gestión de procesos
- `RolesController.cs` - Gestión de roles
- `UsuarioController.cs` - Gestión de usuarios

**Convenciones**:

- Heredan de `ControllerBase`
- Usan atributo `[ApiController]`
- Ruta: `[Route("api/[controller]")]` o rutas personalizadas
- Inyectan SERVICIOS (no repositorios directamente)
- Usan `[Authorize]` para endpoints protegidos

**Ejemplo**:

```csharp
[ApiController]
[Route("api/dominios")]
public class DominiosController : ControllerBase
{
    private readonly IDominioService _dominioService;

    public DominiosController(IDominioService dominioService)
    {
        _dominioService = dominioService;
    }
}
```

---

### 2️⃣ **Services/** - Lógica de Negocio

**Estructura**:

```
Services/
├── Interfaces/              # Contratos de servicios
│   ├── IActividadService.cs
│   ├── IAuditoriaService.cs
│   ├── IAutenticacionService.cs
│   ├── IDominioService.cs
│   ├── IEmailService.cs
│   ├── IProcesoService.cs
│   ├── IRolService.cs
│   ├── ISubdominioService.cs
│   └── IUsuarioService.cs
│
└── Implementations/         # Implementaciones concretas
    ├── ActividadService.cs
    ├── AuditoriaService.cs
    ├── AutenticacionService.cs
    ├── DominioService.cs
    ├── EmailService.cs
    ├── ProcesoService.cs
    ├── RolService.cs
    ├── SubdominioService.cs
    └── UsuarioService.cs
```

**Namespaces**:

- Interfaces: `backend.Services.Interfaces`
- Implementations: `backend.Services.Implementations`

**Responsabilidades**:

- Validaciones de negocio
- Orquestación de repositorios
- Transformación de datos (DTOs ↔ Entidades)
- Lógica de negocio compleja
- Transacciones

**Convenciones**:

- Las implementaciones inyectan REPOSITORIOS
- Usan `using backend.Repositories.Interfaces;`
- NO acceden directamente a Entity Framework
- Manejan excepciones y retornan resultados apropiados

**Ejemplo**:

```csharp
using backend.Services.Interfaces;
using backend.Repositories.Interfaces;

namespace backend.Services.Implementations
{
    public class DominioService : IDominioService
    {
        private readonly IDominioRepository _dominioRepository;

        public DominioService(IDominioRepository dominioRepository)
        {
            _dominioRepository = dominioRepository;
        }
    }
}
```

---

### 3️⃣ **Repositories/** - Acceso a Datos

**Estructura**:

```
Repositories/
├── Interfaces/                      # Contratos de repositorios
│   ├── IRepository.cs              # ⭐ Repositorio base genérico
│   ├── IActividadRepository.cs
│   ├── IAuditoriaRepository.cs
│   ├── IDominioRepository.cs
│   ├── IProcesoRepository.cs
│   ├── IRolRepository.cs
│   ├── ISubdominioRepository.cs
│   └── IUsuarioRepository.cs
│
└── Implementations/                 # Implementaciones concretas
    ├── Repository.cs               # ⭐ Implementación base genérica
    ├── ActividadRepository.cs
    ├── AuditoriaRepository.cs
    ├── DominioRepository.cs
    ├── ProcesoRepository.cs
    ├── RolRepository.cs
    ├── SubdominioRepository.cs
    └── UsuarioRepository.cs
```

**Namespaces**:

- Interfaces: `backend.Repositories.Interfaces`
- Implementations: `backend.Repositories.Implementations`

**Patrón Repository Genérico**:

Todos los repositorios heredan de `IRepository<T, TKey>`:

```csharp
public interface IRepository<T, TKey> where T : class
{
    // CRUD Básico
    Task<T?> ObtenerPorId(TKey id);
    Task<IEnumerable<T>> ObtenerTodos();
    Task<T> Agregar(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(TKey id);
    Task<bool> ExistsAsync(TKey id);

    // Búsqueda avanzada
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    // Paginación
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null
    );

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<int> GuardarCambios();
}
```

**Convenciones**:

- Heredan de `Repository<T, TKey>`
- Implementan interfaces específicas
- Usan `using backend.Repositories.Interfaces;`
- Acceden a `DbSet<T>` a través de `_dbSet`
- Usan `_context` para operaciones complejas

**Ejemplo**:

```csharp
using backend.Data;
using Backend.Models;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories.Implementations
{
    public class DominioRepository : Repository<Dominio, int>, IDominioRepository
    {
        public DominioRepository(NormasDb context) : base(context)
        {
        }

        // Métodos específicos adicionales
        public async Task<Dominio?> FindByNombreAsync(string nombre)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.Nombre == nombre);
        }
    }
}
```

---

### 4️⃣ **Models/** - Entidades de Dominio

**Namespace**: `Backend.Models` o `backend.Models.Usuario`

**Archivos**:

- `Actividad.cs` - Actividades del sistema
- `Auditoria.cs` - Registro de auditoría
- `Dominio.cs` - Dominios de normas
- `Proceso.cs` - Procesos de normas
- `Rol.cs` - Roles de usuario
- `SubDominio.cs` - Subdominios
- `Usuario/Usuario.cs` - Entidad de usuario
- `Usuario/RecuperacionContrasena.cs` - Tokens de recuperación

**Convenciones**:

- Propiedades con notación snake_case para coincidir con BD
- Navegación entre entidades configurada
- Data annotations para validación

**Ejemplo**:

```csharp
namespace Backend.Models
{
    public class Dominio
    {
        public int id_Dominio { get; set; }
        public string Nombre { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Navegación
        public ICollection<Proceso> Procesos { get; set; }
    }
}
```

---

### 5️⃣ **DTOs/** - Data Transfer Objects

**Namespace**: `Backend.Dtos` o `backend.DTOs`

**Propósito**: Transferir datos entre capas sin exponer entidades de dominio

**Archivos**:

- `ActividadesDtos.cs`
- `ActualizarMiPerfilDto.cs`
- `CambiarContrasenaDto.cs`
- `CrearProceso.cs`
- `DominioDtos.cs`
- `FiltroUsuariosDto.cs`
- `MiPerfilDto.cs`
- `SearchDtos.cs`
- `UsuarioEdicionDto.cs`
- `UsuarioRegistroDto.cs`

**Convenciones**:

- Usar para requests/responses de API
- Contienen solo propiedades necesarias
- No incluyen lógica de negocio

---

### 6️⃣ **Data/** - Contexto de Entity Framework

**Namespace**: `backend.Data`

**Archivo**: `NormasDb.cs`

**Propósito**:

- Configurar conexión a BD
- Mapear entidades a tablas
- Configurar relaciones

**Ejemplo**:

```csharp
namespace backend.Data
{
    public class NormasDb : DbContext
    {
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Dominio> Dominios { get; set; }
        // ...

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuraciones de entidades
        }
    }
}
```

---

### 7️⃣ **Migrations/** - Migraciones de Base de Datos

**Namespace**: `backend.Migrations`

**Archivos**:

- `20251004215232_CreacionBaseDeDatos.cs`
- `20251015050956_AddRecuperacionesContrasena.cs`
- `20251017062324_AgregarDebeRestablecerContrasena.cs`
- `NormasDbModelSnapshot.cs`

**Comandos**:

```bash
# Crear migración
dotnet ef migrations add NombreMigracion

# Aplicar migraciones
dotnet ef database update

# Revertir última migración
dotnet ef database update PreviousMigrationName
```

---

### 8️⃣ **Startup.cs** - Configuración de la Aplicación

**Responsabilidades**:

- Configurar servicios (DI)
- Configurar middleware
- Configurar CORS
- Configurar autenticación JWT
- Registrar repositorios y servicios

**IMPORTANTE - Inyección de Dependencias**:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ⚠️ USAR SIEMPRE ESTOS NAMESPACES COMPLETOS:

    // Repositories
    services.AddScoped<backend.Repositories.Interfaces.IUsuarioRepository,
                      backend.Repositories.Implementations.UsuarioRepository>();
    services.AddScoped<backend.Repositories.Interfaces.IRolRepository,
                      backend.Repositories.Implementations.RolRepository>();
    services.AddScoped<backend.Repositories.Interfaces.IDominioRepository,
                      backend.Repositories.Implementations.DominioRepository>();
    // ... etc

    // Services
    services.AddScoped<backend.Services.Interfaces.IUsuarioService,
                      backend.Services.Implementations.UsuarioService>();
    services.AddScoped<backend.Services.Interfaces.IRolService,
                      backend.Services.Implementations.RolService>();
    // ... etc
}
```

---

## 🎨 FRONTEND - React

### Tecnologías

- **React**: 18.2.0
- **Router**: react-router-dom 6.15.0
- **State Management**: @tanstack/react-query 5.90.2
- **Build Tool**: Vite 4.4.5
- **PDF Generation**: jspdf 3.0.3, jspdf-autotable 5.0.2

### Estructura

```
src/
├── components/         # Componentes reutilizables
├── contexts/          # Context API (AuthContext, etc.)
├── layouts/           # Layouts de página
├── pages/            # Páginas de la aplicación
├── App.jsx           # Componente raíz con rutas
└── main.jsx          # Punto de entrada
```

---

## 🔐 Autenticación y Autorización

### JWT (JSON Web Tokens)

**Backend**:

- Configurado en `Startup.cs`
- Token generado en `AutenticacionService`
- Validado en middleware

**Frontend**:

- Token almacenado en `localStorage`
- Enviado en header `Authorization: Bearer {token}`
- Manejado por `AuthContext`

### Roles

1. **SUPERADMIN** (id: 1) - Acceso total
2. **ADMIN** (id: 2) - Gestión administrativa
3. **USER** (id: 3) - Usuario estándar

---

## 📋 CONVENCIONES Y MEJORES PRÁCTICAS

### ✅ DO - Hacer

1. **Separación de Responsabilidades**:
   - Controllers → llaman a Services
   - Services → llaman a Repositories
   - Repositories → acceden a Entity Framework

2. **Inyección de Dependencias**:
   - Siempre usar interfaces
   - Registrar en `Startup.cs`
   - Usar namespaces completos en DI

3. **Namespaces Correctos**:

   ```csharp
   // Controllers
   using backend.Services.Interfaces;

   // Services
   using backend.Repositories.Interfaces;
   using backend.Services.Interfaces;

   // Repositories
   using backend.Repositories.Interfaces;
   using backend.Data;
   ```

4. **Async/Await**:
   - Todos los métodos de BD deben ser async
   - Usar sufijo `Async` en nombres de métodos

5. **Manejo de Errores**:
   - Try-catch en Services
   - Retornar respuestas HTTP apropiadas

### ❌ DON'T - No Hacer

1. **NO** acceder directamente a Entity Framework desde Controllers
2. **NO** usar `namespace backend.Repositories` (obsoleto)
3. **NO** mezclar lógica de negocio en Controllers
4. **NO** exponer entidades directamente, usar DTOs
5. **NO** hardcodear strings de conexión

---

## 🚀 Comandos Útiles

### Backend

```bash
# Compilar
dotnet build

# Ejecutar
dotnet run

# Crear migración
dotnet ef migrations add NombreMigracion

# Aplicar migraciones
dotnet ef database update

# Limpiar y reconstruir
dotnet clean
dotnet build
```

### Frontend

```bash
# Instalar dependencias
npm install

# Modo desarrollo
npm run dev

# Build producción
npm run build

# Preview build
npm run preview
```

---

## 📊 Base de Datos

### Estructura Principal

- **Usuarios** - Gestión de usuarios
- **Roles** - Roles del sistema
- **Dominios** - Dominios de normas
- **Procesos** - Procesos asociados a dominios
- **Subdominios** - Subdivisiones de procesos
- **Actividades** - Actividades de subdominios
- **Auditoria** - Registro de eventos del sistema
- **RecuperacionesContrasena** - Tokens de recuperación

---

## 🔄 Flujo de una Request Típica

```
1. HTTP Request → Controller
                    ↓
2. Controller → Service (con DTO)
                    ↓
3. Service → Repository (con Entidad)
                    ↓
4. Repository → Entity Framework
                    ↓
5. Entity Framework → Base de Datos
                    ↓
6. Base de Datos → Entity Framework
                    ↓
7. Entity Framework → Repository
                    ↓
8. Repository → Service
                    ↓
9. Service → Controller (con DTO)
                    ↓
10. Controller → HTTP Response
```

---

## 📝 Notas Importantes

1. **Usuario Admin Inicial**:
   - Cédula: `000000000`
   - Email: `eherreram200@gmail.com`
   - Contraseña: `superadmin1234`
   - Rol: SUPERADMIN

2. **Migraciones Automáticas**:
   - Se aplican automáticamente al iniciar la aplicación
   - Configurado en `Program.cs`

3. **Semilla de Datos**:
   - Roles creados automáticamente si no existen
   - Usuario admin creado si no hay usuarios

4. **Scripts SQL**:
   - Ubicados en `backend/Scripts/`
   - Se copian al output al compilar

---

## 🆘 Solución de Problemas Comunes

### Error: "No se encuentra IRepositorio"

✅ Verificar: `using backend.Repositories.Interfaces;`

### Error: "Circular dependency"

✅ Verificar inyección de dependencias en `Startup.cs`

### Error de compilación después de mover archivos

✅ Verificar namespaces en archivos movidos
✅ Reconstruir: `dotnet clean && dotnet build`

### Error de migraciones

✅ Verificar string de conexión en `appsettings.json`
✅ Aplicar: `dotnet ef database update`

---

## 📞 Contacto y Recursos

- **Proyecto**: Sistema de Normas MICITT
- **Repository**: LuisMaMS/sistema-normas-micitt
- **Branch**: main

---

**Última actualización**: Octubre 19, 2025
**Versión del documento**: 1.0
