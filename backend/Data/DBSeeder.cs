using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class DbSeeder
    {
        private readonly NormasDb _db;

        public DbSeeder(NormasDb db)
        {
            _db = db;
        }

        public async Task SeedDominiosCobitAsync()
        {
            if (await _db.Dominios.AnyAsync())
            {
                Console.WriteLine("✅ Dominios COBIT ya existen");
                return;
            }

            Console.WriteLine("📝 Creando dominios COBIT...");

            _db.Dominios.AddRange(
                new Dominio { Nombre = "EDM - Evaluar, Dirigir y Monitorear" },
                new Dominio { Nombre = "APO - Alinear, Planificar y Organizar" },
                new Dominio { Nombre = "BAI - Construir, Adquirir e Implementar" },
                new Dominio { Nombre = "DSS - Entregar, Dar Servicio y Soporte" },
                new Dominio { Nombre = "MEA - Monitorear, Evaluar y Valorar" }
            );

            await _db.SaveChangesAsync();
            Console.WriteLine("✅ Dominios COBIT creados correctamente");
        }

        public async Task SeedProcesosCobitAsync()
        {
            if (await _db.Procesos.AnyAsync())
            {
                Console.WriteLine("Procesos COBIT ya existen");
                return;
            }

            Console.WriteLine("Creando procesos COBIT...");

            var procesos = new List<Proceso>
            {
                // === EDM ===
                new Proceso {  Codigo = "EDM01",Nombre = "Asegurar el establecimiento y mantenimiento del marco de gobierno",MarcoNormativo = "COBIT 2019",EstadoImplementacion = "Sí",PorcentajeAvance = 75.00m,PrioridadImplementacion = 3,FechaConclusionImplementacion = null,FechaCreacion = DateTime.UtcNow,FechaModificacion = DateTime.UtcNow,CreadoPorId = 1,ModificadoPorId = 1,DominioId = 1},
                new Proceso {  Codigo = "EDM02", Nombre = "Asegurar la entrega de beneficios", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 60.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 1 },
                new Proceso {  Codigo = "EDM03", Nombre = "Asegurar la optimización del riesgo", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 55.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 1 },
                new Proceso {  Codigo = "EDM04", Nombre = "Asegurar la optimización de recursos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 70.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 1 },
                new Proceso {  Codigo = "EDM05", Nombre = "Asegurar la transparencia hacia las partes interesadas", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "No", PorcentajeAvance = 30.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 1 },

                // === APO ===
                new Proceso {  Codigo = "APO01", Nombre = "Gestionar el marco de gestión de TI", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 80.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO02", Nombre = "Gestionar la estrategia", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 65.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO03", Nombre = "Gestionar la arquitectura empresarial", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 70.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO04", Nombre = "Gestionar la innovación", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "No", PorcentajeAvance = 40.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO05", Nombre = "Gestionar el portafolio", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 75.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO06", Nombre = "Gestionar el presupuesto y los costos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 85.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO07", Nombre = "Gestionar los recursos humanos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 60.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO08", Nombre = "Gestionar las relaciones", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 50.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO09", Nombre = "Gestionar los acuerdos de servicio", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 70.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO10", Nombre = "Gestionar los proveedores", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí in", PorcentajeAvance = 65.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO11", Nombre = "Gestionar la calidad", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "No", PorcentajeAvance = 45.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO12", Nombre = "Gestionar el riesgo", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 75.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO13", Nombre = "Gestionar la seguridad", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 80.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },
                new Proceso {  Codigo = "APO14", Nombre = "Gestionar los datos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 55.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 2 },

                // === BAI ===
                new Proceso { Codigo = "BAI01", Nombre = "Gestionar los programas y proyectos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 70.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI02", Nombre = "Gestionar la definición de requerimientos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 75.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI03", Nombre = "Gestionar la identificación y construcción de soluciones", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 65.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI04", Nombre = "Gestionar la disponibilidad y capacidad", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 60.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI05", Nombre = "Gestionar la habilitación del cambio organizacional", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "No", PorcentajeAvance = 35.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI06", Nombre = "Gestionar los cambios", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 80.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI07", Nombre = "Gestionar la aceptación del cambio y la transición", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 70.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI08", Nombre = "Gestionar el conocimiento", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "No", PorcentajeAvance = 40.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI09", Nombre = "Gestionar los activos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 75.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI10", Nombre = "Gestionar la configuración", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 85.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },
                new Proceso {  Codigo = "BAI11", Nombre = "Gestionar los proyectos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 80.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 3 },

                // === DSS ===
                new Proceso {  Codigo = "DSS01", Nombre = "Gestionar las operaciones", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 85.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 4 },
                new Proceso {  Codigo = "DSS02", Nombre = "Gestionar las peticiones e incidentes del servicio", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 90.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 4 },
                new Proceso {  Codigo = "DSS03", Nombre = "Gestionar los problemas", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 75.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 4 },
                new Proceso {  Codigo = "DSS04", Nombre = "Gestionar la continuidad", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 70.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 4 },
                new Proceso {  Codigo = "DSS05", Nombre = "Gestionar los servicios de seguridad", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 80.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 4 },
                new Proceso {  Codigo = "DSS06", Nombre = "Gestionar los controles de procesos de negocio", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 65.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 4 },

                // === MEA ===
                new Proceso {  Codigo = "MEA01", Nombre = "Monitorear, evaluar y valorar el rendimiento y la conformidad", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 70.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 5 },
                new Proceso {  Codigo = "MEA02", Nombre = "Monitorear, evaluar y valorar el sistema de control interno", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 65.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 5 },
                new Proceso {  Codigo = "MEA03", Nombre = "Monitorear, evaluar y valorar la conformidad con requerimientos externos", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "Sí", PorcentajeAvance = 75.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 5 },
                new Proceso {  Codigo = "MEA04", Nombre = "Proporcionar gobierno, riesgo y cumplimiento", MarcoNormativo = "COBIT 2019", EstadoImplementacion = "No", PorcentajeAvance = 50.00m, PrioridadImplementacion = 3, FechaConclusionImplementacion = null, FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow, CreadoPorId = 1, ModificadoPorId = 1, DominioId = 5 },
            };

            _db.Procesos.AddRange(procesos);
            await _db.SaveChangesAsync();

            Console.WriteLine("Procesos COBIT (EDM + APO) creados correctamente");
        }

        public async Task SeedSubdominiosCobitAsync()
        {
            if (await _db.Subdominios.AnyAsync())
            {
                Console.WriteLine("Subdominios COBIT ya existen");
                return;
            }

            Console.WriteLine("Creando subdominios COBIT...");

            var subdominios = new List<Subdominio>
            {
                // === EDM ===
                new Subdominio { PracticasGobierno = "Establecimiento del Marco de Gobierno", IndicadoresAsociados = "Procesos para establecer y mantener el marco de gobierno corporativo", ProcesoId = 1 },
                new Subdominio { PracticasGobierno = "Entrega de Beneficios", IndicadoresAsociados = "Procesos para asegurar la realización de beneficios esperados", ProcesoId = 2 },
                new Subdominio { PracticasGobierno = "Optimización del Riesgo", IndicadoresAsociados = "Procesos para gestionar y optimizar los riesgos organizacionales", ProcesoId = 3 },
                new Subdominio { PracticasGobierno = "Optimización de Recursos", IndicadoresAsociados = "Procesos para optimizar el uso de recursos organizacionales", ProcesoId = 4 },
                new Subdominio { PracticasGobierno = "Transparencia hacia Partes Interesadas", IndicadoresAsociados = "Procesos para asegurar comunicación transparente con stakeholders", ProcesoId = 5 },

                // === APO ===
                new Subdominio { PracticasGobierno = "Marco de Gestión", IndicadoresAsociados = "Establecimiento del marco general de gestión de TI", ProcesoId = 6 },
                new Subdominio { PracticasGobierno = "Estrategia Empresarial", IndicadoresAsociados = "Alineación de la estrategia de TI con los objetivos de negocio", ProcesoId = 7 },
                new Subdominio { PracticasGobierno = "Arquitectura Empresarial", IndicadoresAsociados = "Gestión de la arquitectura tecnológica y de negocio", ProcesoId = 8 },
                new Subdominio { PracticasGobierno = "Innovación Tecnológica", IndicadoresAsociados = "Promoción y gestión de la innovación en TI", ProcesoId = 9 },
                new Subdominio { PracticasGobierno = "Gestión de Portafolio", IndicadoresAsociados = "Administración del portafolio de proyectos y servicios", ProcesoId = 10 },
                new Subdominio { PracticasGobierno = "Gestión Financiera", IndicadoresAsociados = "Control presupuestario y gestión de costos de TI", ProcesoId = 11 },
                new Subdominio { PracticasGobierno = "Gestión del Talento", IndicadoresAsociados = "Administración del capital humano en TI", ProcesoId = 12 },
                new Subdominio { PracticasGobierno = "Gestión de Relaciones", IndicadoresAsociados = "Mantenimiento de relaciones con partes interesadas", ProcesoId = 13 },
                new Subdominio { PracticasGobierno = "Acuerdos de Servicio", IndicadoresAsociados = "Definición y gestión de niveles de servicio", ProcesoId = 14 },
                new Subdominio { PracticasGobierno = "Gestión de Proveedores", IndicadoresAsociados = "Administración de relaciones con proveedores externos", ProcesoId = 15 },
                new Subdominio { PracticasGobierno = "Gestión de Calidad", IndicadoresAsociados = "Aseguramiento de la calidad en procesos y servicios", ProcesoId = 16 },
                new Subdominio { PracticasGobierno = "Gestión de Riesgos", IndicadoresAsociados = "Identificación, análisis y tratamiento de riesgos", ProcesoId = 17 },
                new Subdominio { PracticasGobierno = "Gestión de Seguridad", IndicadoresAsociados = "Protección de activos de información y sistemas", ProcesoId = 18 },
                new Subdominio { PracticasGobierno = "Gestión de Datos", IndicadoresAsociados = "Administración del ciclo de vida de los datos", ProcesoId = 19 },

                // === BAI ===
                new Subdominio { PracticasGobierno = "Gestión de Programas", IndicadoresAsociados = "Coordinación y gestión de programas y proyectos", ProcesoId = 20 },
                new Subdominio { PracticasGobierno = "Definición de Requerimientos", IndicadoresAsociados = "Análisis y documentación de requerimientos", ProcesoId = 21 },
                new Subdominio { PracticasGobierno = "Construcción de Soluciones", IndicadoresAsociados = "Desarrollo e implementación de soluciones tecnológicas", ProcesoId = 22 },
                new Subdominio { PracticasGobierno = "Gestión de Capacidad", IndicadoresAsociados = "Planificación y gestión de la capacidad tecnológica", ProcesoId = 23 },
                new Subdominio { PracticasGobierno = "Cambio Organizacional", IndicadoresAsociados = "Facilitación del cambio en la organización", ProcesoId = 24 },
                new Subdominio { PracticasGobierno = "Control de Cambios", IndicadoresAsociados = "Gestión formal de cambios en el ambiente tecnológico", ProcesoId = 25 },
                new Subdominio { PracticasGobierno = "Transición de Servicios", IndicadoresAsociados = "Implementación y puesta en producción de cambios", ProcesoId = 26 },
                new Subdominio { PracticasGobierno = "Gestión del Conocimiento", IndicadoresAsociados = "Captura, almacenamiento y transferencia de conocimiento", ProcesoId = 27 },
                new Subdominio { PracticasGobierno = "Gestión de Activos", IndicadoresAsociados = "Administración del inventario de activos tecnológicos", ProcesoId = 28 },
                new Subdominio { PracticasGobierno = "Gestión de Configuración", IndicadoresAsociados = "Control de la configuración de componentes de TI", ProcesoId = 29 },
                new Subdominio { PracticasGobierno = "Ejecución de Proyectos", IndicadoresAsociados = "Implementación y control de proyectos específicos", ProcesoId = 30 },

                // === DSS ===
                new Subdominio { PracticasGobierno = "Operaciones de TI", IndicadoresAsociados = "Gestión diaria de las operaciones tecnológicas", ProcesoId = 31 },
                new Subdominio { PracticasGobierno = "Mesa de Servicios", IndicadoresAsociados = "Atención de peticiones e incidentes de usuarios", ProcesoId = 32 },
                new Subdominio { PracticasGobierno = "Gestión de Problemas", IndicadoresAsociados = "Análisis y resolución de problemas recurrentes", ProcesoId = 33 },
                new Subdominio { PracticasGobierno = "Continuidad del Negocio", IndicadoresAsociados = "Planificación y gestión de la continuidad operacional", ProcesoId = 34 },
                new Subdominio { PracticasGobierno = "Seguridad Operacional", IndicadoresAsociados = "Implementación de controles de seguridad operacional", ProcesoId = 35 },
                new Subdominio { PracticasGobierno = "Controles de Proceso", IndicadoresAsociados = "Supervisión de controles en procesos de negocio", ProcesoId = 36 },

                // === MEA ===
                new Subdominio { PracticasGobierno = "Monitoreo del Rendimiento", IndicadoresAsociados = "Supervisión del desempeño y conformidad organizacional", ProcesoId = 37 },
                new Subdominio { PracticasGobierno = "Control Interno", IndicadoresAsociados = "Evaluación del sistema de control interno", ProcesoId = 38 },
                new Subdominio { PracticasGobierno = "Cumplimiento Regulatorio", IndicadoresAsociados = "Verificación del cumplimiento de requerimientos externos", ProcesoId = 39 },
                new Subdominio { PracticasGobierno = "Gobierno y Cumplimiento", IndicadoresAsociados = "Supervisión integral de gobierno, riesgo y cumplimiento", ProcesoId = 40 }
            };

            _db.Subdominios.AddRange(subdominios);
            await _db.SaveChangesAsync();

            Console.WriteLine("40 subdominios COBIT creados correctamente");
        }

        public async Task SeedActividadesEjemploAsync()
        {
            if (await _db.Actividades.AnyAsync())
            {
                Console.WriteLine("Actividades de ejemplo ya existen");
                return;
            }

            Console.WriteLine("Creando actividades de ejemplo...");

            _db.Actividades.AddRange(
                new Actividad { Nombre = "Definir estructura de gobierno de TI", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 12, 31), EstadoImplementacion = "En Progreso", PorcentajeAvance = 60.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Definición de roles y responsabilidades en proceso", SubdominioId = 1 },
                new Actividad { Nombre = "Establecer comités de gobierno", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 11, 30), EstadoImplementacion = "Pendiente", PorcentajeAvance = 25.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Pendiente aprobación de estructura organizacional", SubdominioId = 1 },
                new Actividad { Nombre = "Crear políticas de gobierno de TI", Implementable = "Sí", FechaCompromiso = new DateTime(2026, 1, 31), EstadoImplementacion = "En Progreso", PorcentajeAvance = 40.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Borrador de políticas en revisión", SubdominioId = 1 },
                new Actividad { Nombre = "Implementar framework de gestión COBIT", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 10, 30), EstadoImplementacion = "En Progreso", PorcentajeAvance = 75.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Framework en fase de implementación", SubdominioId = 6 },
                new Actividad { Nombre = "Definir procesos de gestión de TI", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 11, 15), EstadoImplementacion = "En Progreso", PorcentajeAvance = 50.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Documentación de procesos iniciada", SubdominioId = 6 },
                new Actividad { Nombre = "Establecer métricas de gestión", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 12, 15), EstadoImplementacion = "Pendiente", PorcentajeAvance = 30.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Definición de KPIs pendiente", SubdominioId = 6 },
                new Actividad { Nombre = "Establecer oficina de gestión de proyectos (PMO)", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 12, 1), EstadoImplementacion = "En Progreso", PorcentajeAvance = 65.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "PMO en proceso de estructuración", SubdominioId = 20 },
                new Actividad { Nombre = "Implementar metodología de gestión de proyectos", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 11, 1), EstadoImplementacion = "En Progreso", PorcentajeAvance = 80.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Metodología definida, en proceso de capacitación", SubdominioId = 20 },
                new Actividad { Nombre = "Crear portafolio de proyectos institucionales", Implementable = "Sí", FechaCompromiso = new DateTime(2026, 1, 15), EstadoImplementacion = "Pendiente", PorcentajeAvance = 20.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Inventario inicial completado", SubdominioId = 20 },
                new Actividad { Nombre = "Implementar centro de operaciones de red (NOC)", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 11, 30), EstadoImplementacion = "En Progreso", PorcentajeAvance = 70.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Infraestructura del NOC en instalación", SubdominioId = 31 },
                new Actividad { Nombre = "Establecer procedimientos operacionales", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 10, 31), EstadoImplementacion = "En Progreso", PorcentajeAvance = 85.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Procedimientos documentados, en proceso de validación", SubdominioId = 31 },
                new Actividad { Nombre = "Implementar monitoreo proactivo de sistemas", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 12, 31), EstadoImplementacion = "En Progreso", PorcentajeAvance = 55.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Herramientas de monitoreo en configuración", SubdominioId = 31 },
                new Actividad { Nombre = "Implementar dashboard ejecutivo de TI", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 11, 15), EstadoImplementacion = "En Progreso", PorcentajeAvance = 60.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Dashboard en desarrollo, faltan métricas financieras", SubdominioId = 37 },
                new Actividad { Nombre = "Establecer proceso de auditoría interna de TI", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 12, 31), EstadoImplementacion = "Pendiente", PorcentajeAvance = 35.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Plan de auditoría en elaboración", SubdominioId = 37 },
                new Actividad { Nombre = "Crear reportes de cumplimiento regulatorio", Implementable = "Sí", FechaCompromiso = new DateTime(2026, 2, 28), EstadoImplementacion = "Pendiente", PorcentajeAvance = 15.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Análisis de requerimientos regulatorios iniciado", SubdominioId = 37 },
                new Actividad { Nombre = "Capacitación en seguridad informática", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 10, 15), EstadoImplementacion = "Implementado", PorcentajeAvance = 100.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Programa de capacitación completado exitosamente", SubdominioId = 18 },
                new Actividad { Nombre = "Actualización de infraestructura de red", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 12, 1), EstadoImplementacion = "En Progreso", PorcentajeAvance = 45.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Fase 1 completada, iniciando Fase 2", SubdominioId = 31 },
                new Actividad { Nombre = "Implementación de respaldo automático", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 9, 30), EstadoImplementacion = "Implementado", PorcentajeAvance = 100.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Sistema de respaldo operacional", SubdominioId = 34 },
                new Actividad { Nombre = "Análisis de riesgos de ciberseguridad", Implementable = "Sí", FechaCompromiso = new DateTime(2025, 11, 30), EstadoImplementacion = "En Progreso", PorcentajeAvance = 70.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Análisis técnico completado, falta documentación", SubdominioId = 17 },
                new Actividad { Nombre = "Migración a servicios en la nube", Implementable = "No", FechaCompromiso = new DateTime(2026, 6, 30), EstadoImplementacion = "Pendiente", PorcentajeAvance = 10.00m, FuncionariosResponsablesId = 1, FechaControl = null, Documentos = null, Observaciones = "Proyecto en fase de planeación inicial", SubdominioId = 22 }
            );

            await _db.SaveChangesAsync();
            Console.WriteLine("Actividades de ejemplo creadas correctamente");
        }





        public async Task SeedAllAsync()
        {
            Console.WriteLine("🌱 Iniciando seed de datos...");

            await SeedDominiosCobitAsync();
            await SeedProcesosCobitAsync();
            await SeedSubdominiosCobitAsync();
            await SeedActividadesEjemploAsync();

            // Agregar más métodos según necesites

            Console.WriteLine("✅ Seed completado");
        }
    }
}
