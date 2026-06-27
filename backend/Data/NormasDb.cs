using System;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using backend.Models.UsuarioContrasena;

namespace backend.Data
{
      public class NormasDb : DbContext
      {
            public NormasDb(DbContextOptions<NormasDb> options) : base(options) { }

            // Setea las tablas para consultas LINQ
            public virtual DbSet<Usuario> Usuarios { get; set; }
            public virtual DbSet<Rol> Roles { get; set; }
            public virtual DbSet<Proceso> Procesos { get; set; }
            public virtual DbSet<Dominio> Dominios { get; set; }
            public virtual DbSet<Actividad> Actividades { get; set; }
            public virtual DbSet<Subdominio> Subdominios { get; set; }
            public virtual DbSet<Auditoria> Auditorias { get; set; }

            public virtual DbSet<backend.Models.UsuarioContrasena.RecuperacionContrasena> RecuperacionesContrasena { get; set; }
            public virtual DbSet<backend.Models.UsuarioContrasena.TwoFactorCode> TwoFactorCodes { get; set; }

            // Gestión Documental
            public virtual DbSet<Documento> Documentos { get; set; }
            public virtual DbSet<VersionDocumento> VersionesDocumento { get; set; }
            public virtual DbSet<RelacionDocumento> RelacionesDocumento { get; set; }
            public virtual DbSet<MetadatoDocumento> MetadatosDocumento { get; set; }

            // Notificaciones (HU-016, HU-027)
            public virtual DbSet<Notificacion> Notificaciones { get; set; }

            // Historial de versiones de actividades
            public virtual DbSet<HistorialVersionActividad> HistorialVersionesActividades { get; set; }

            // DEFINE COMO SE CONTRUYEN LAS TABLAS Y RELACIONES
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                  // --- Rol ---
                  modelBuilder.Entity<Rol>().ToTable("rol");

                  // --- Usuario ---
                  modelBuilder.Entity<Usuario>(entity =>
                  {
                        entity.HasKey(e => e.Id_Usuario);

                        entity.Property(e => e.cedula)
                        .IsRequired()
                        .HasMaxLength(20);

                        entity.Property(e => e.nombre)
                        .IsRequired()
                        .HasMaxLength(40);

                        entity.Property(e => e.correo_electronico)
                        .IsRequired()
                        .HasMaxLength(50);

                        entity.Property(e => e.departamento)
                        .HasMaxLength(50);

                        entity.Property(e => e.idRol)
                        .IsRequired();

                        entity.Property(e => e.contrasena)
                        .IsRequired()
                        .HasMaxLength(255);

                        entity.Property(e => e.estado)
                        .HasDefaultValue(true);

                        entity.Property(e => e.fechaCreacion)
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                        entity.Property(e => e.fechaUltimaModificacion)
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                        entity.Property(e => e.ultimoAcceso);

                        entity.Property(e => e.TwoFactorEnabled)
                              .HasDefaultValue(false);

                        entity.HasIndex(e => e.correo_electronico)
                        .IsUnique();

                        entity.HasOne(u => u.Rol)
                        .WithMany(r => r.Usuarios)
                        .HasForeignKey(u => u.idRol)
                        .OnDelete(DeleteBehavior.Restrict);
                  });

                  modelBuilder.Entity<backend.Models.UsuarioContrasena.TwoFactorCode>(entity =>
                  {
                        entity.HasKey(e => e.Id);
                        entity.Property(e => e.CodigoHash).IsRequired().HasMaxLength(255);
                        entity.Property(e => e.ExpiraEn).IsRequired();
                        entity.Property(e => e.Usado).HasDefaultValue(false);
                        entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
                        entity.HasOne(e => e.Usuario)
                              .WithMany()
                              .HasForeignKey(e => e.UsuarioId)
                              .OnDelete(DeleteBehavior.Cascade);
                  });

                  // --- Proceso ---
                  modelBuilder.Entity<Proceso>(e =>
                  {
                        e.HasIndex(p => p.Codigo).IsUnique();
                        e.Property(p => p.PorcentajeAvance)
                   .HasPrecision(5, 2)
                   .HasDefaultValue(0.00m);

                        // Se respeta el valor por defecto existente
                        e.Property(p => p.EstadoImplementacion)
                         .HasDefaultValue("Sí");

                        e.Property(p => p.PrioridadImplementacion)
                         .HasDefaultValue(3);

                        e.HasOne(p => p.Dominio)
                   .WithMany(d => d.Procesos)
                   .HasForeignKey(p => p.DominioId)
                   .OnDelete(DeleteBehavior.Restrict);

                        e.HasMany(p => p.Subdominios)
                   .WithOne(s => s.Proceso)
                   .HasForeignKey(s => s.ProcesoId)
                   .OnDelete(DeleteBehavior.Restrict);
                  });

                  // --- Subdominio ---
                  modelBuilder.Entity<Subdominio>(e =>
                  {
                        e.HasIndex(s => s.ProcesoId);
                  });

                  // --- Actividad ---
                  modelBuilder.Entity<Actividad>(e =>
                  {
                        e.Property(a => a.PorcentajeAvance)
                   .HasPrecision(5, 2)
                   .HasDefaultValue(0.00m);

                        e.Property(a => a.Implementable)
                   .HasDefaultValue("Sí");

                        e.Property(a => a.EstadoImplementacion)
                   .HasDefaultValue("Pendiente");

                        e.HasOne(a => a.FuncionariosResponsables)
                   .WithMany()
                   .HasForeignKey(a => a.FuncionariosResponsablesId)
                   .OnDelete(DeleteBehavior.Restrict);

                        e.HasOne(a => a.Subdominio)
                   .WithMany(s => s.Actividades)
                   .HasForeignKey(a => a.SubdominioId)
                   .OnDelete(DeleteBehavior.Restrict);
                  });

                  // --- Auditoria ---
                  modelBuilder.Entity<Auditoria>(entity =>
                  {
                        entity.ToTable("auditoria");
                        entity.HasKey(e => e.IdAuditoria);

                        entity.Property(e => e.Descripcion)
                        .IsRequired()
                        .HasMaxLength(500);

                        entity.Property(e => e.TipoEvento)
                        .IsRequired()
                        .HasMaxLength(100);

                        entity.Property(e => e.Modulo)
                        .HasMaxLength(100);

                        entity.Property(e => e.DireccionIp)
                        .HasMaxLength(50);

                        entity.Property(e => e.Navegador)
                        .HasMaxLength(500);

                        entity.HasOne(e => e.Usuario)
                        .WithMany()
                        .HasForeignKey(e => e.IdUsuario)
                        .OnDelete(DeleteBehavior.SetNull);

                        entity.HasIndex(e => e.FechaEvento);
                        entity.HasIndex(e => e.TipoEvento);
                        entity.HasIndex(e => e.Modulo);
                  });

                  // --- Historial de versiones de actividades ---
                  modelBuilder.Entity<HistorialVersionActividad>(entity =>
                  {
                        entity.HasOne(h => h.Actividad)
                        .WithMany()
                        .HasForeignKey(h => h.ActividadId)
                        .OnDelete(DeleteBehavior.Cascade);

                        entity.HasIndex(h => new { h.ActividadId, h.Version })
                        .IsUnique();

                        entity.HasIndex(h => h.FechaRegistro);
                  });

                  // =========================================================
                  // GESTIÓN DOCUMENTAL
                  // =========================================================

                  // --- Documento ---
                  modelBuilder.Entity<Documento>(e =>
                  {
                        // FK: Documento → Actividad
                        e.HasOne(d => d.Actividad)
                         .WithMany(a => a.DocumentosVinculados)
                         .HasForeignKey(d => d.ActividadId)
                         .OnDelete(DeleteBehavior.Restrict);

                        // FK circular: Documento.VersionActualId → VersionDocumento
                        // IsRequired(false) es NECESARIO para que EF Core cree la tabla sin error circular
                        e.HasOne(d => d.VersionActual)
                         .WithMany()
                         .HasForeignKey(d => d.VersionActualId)
                         .IsRequired(false)
                         .OnDelete(DeleteBehavior.Restrict);

                        // Tres FK a Usuario — configuradas explícitamente para evitar ambigüedad
                        e.HasOne(d => d.CreadoPor)
                         .WithMany()
                         .HasForeignKey(d => d.CreadoPorId)
                         .OnDelete(DeleteBehavior.Restrict);

                        e.HasOne(d => d.ModificadoPor)
                         .WithMany()
                         .HasForeignKey(d => d.ModificadoPorId)
                         .IsRequired(false)
                         .OnDelete(DeleteBehavior.Restrict);

                        e.HasOne(d => d.EliminadoPor)
                         .WithMany()
                         .HasForeignKey(d => d.EliminadoPorId)
                         .IsRequired(false)
                         .OnDelete(DeleteBehavior.Restrict);

                        // RowVersion: TIMESTAMP(6) DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)
                        e.Property(d => d.RowVersion)
                         .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                         .ValueGeneratedOnAddOrUpdate();

                        e.Property(d => d.Estado).HasDefaultValue("Borrador");
                        e.Property(d => d.Confidencialidad).HasDefaultValue("Interna");
                        e.Property(d => d.RolEnActividad).HasDefaultValue("Anexo");
                        e.Property(d => d.Eliminado).HasDefaultValue(false);
                        // e.Property(d => d.FechaCreacion) // No default SQL, se asigna en C#

                        e.HasIndex(d => new { d.ActividadId, d.Estado, d.Eliminado })
                         .HasDatabaseName("IDX_Documentos_Actividad_Estado");
                        e.HasIndex(d => d.FechaVencimiento)
                         .HasDatabaseName("IDX_FechaVencimiento");
                        e.HasIndex(d => d.Estado)
                         .HasDatabaseName("IDX_Estado");
                  });

                  // --- VersionDocumento ---
                  modelBuilder.Entity<VersionDocumento>(e =>
                  {
                        // FK circular: VersionDocumento.DocumentoId → Documento (ON DELETE CASCADE)
                        e.HasOne(v => v.Documento)
                         .WithMany(d => d.Versiones)
                         .HasForeignKey(v => v.DocumentoId)
                         .OnDelete(DeleteBehavior.Cascade);

                        e.HasOne(v => v.SubidoPor)
                         .WithMany()
                         .HasForeignKey(v => v.SubidoPorId)
                         .OnDelete(DeleteBehavior.Restrict);

                        // Constraint: un número de versión único por documento
                        e.HasIndex(v => new { v.DocumentoId, v.NumeroVersion })
                         .IsUnique()
                         .HasDatabaseName("UQ_Documento_NumeroVersion");

                        e.HasIndex(v => v.ChecksumSHA256)
                         .HasDatabaseName("IDX_ChecksumSHA256");

                        e.Property(v => v.FechaSubida).HasDefaultValueSql("CURRENT_TIMESTAMP");
                        e.Property(v => v.Activo).HasDefaultValue(true);
                  });

                  // --- RelacionDocumento ---
                  modelBuilder.Entity<RelacionDocumento>(e =>
                  {
                        // Dos FK al mismo tipo Documento — deben configurarse explícitamente
                        e.HasOne(r => r.DocumentoOrigen)
                         .WithMany(d => d.RelacionesComoOrigen)
                         .HasForeignKey(r => r.DocumentoOrigenId)
                         .OnDelete(DeleteBehavior.Cascade);

                        // RESTRICT en destino para evitar ciclos de CASCADE en MySQL
                        e.HasOne(r => r.DocumentoDestino)
                         .WithMany(d => d.RelacionesComoDestino)
                         .HasForeignKey(r => r.DocumentoDestinoId)
                         .OnDelete(DeleteBehavior.Restrict);

                        e.HasOne(r => r.CreadoPor)
                         .WithMany()
                         .HasForeignKey(r => r.CreadoPorId)
                         .OnDelete(DeleteBehavior.Restrict);

                        e.HasIndex(r => new { r.DocumentoOrigenId, r.DocumentoDestinoId, r.TipoRelacion })
                         .IsUnique()
                         .HasDatabaseName("UQ_Relacion");

                        e.Property(r => r.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
                        e.Property(r => r.Activo).HasDefaultValue(true);
                  });

                  // --- MetadatoDocumento ---
                  modelBuilder.Entity<MetadatoDocumento>(e =>
                  {
                        e.HasOne(m => m.Documento)
                         .WithMany(d => d.Metadatos)
                         .HasForeignKey(m => m.DocumentoId)
                         .OnDelete(DeleteBehavior.Cascade);

                        e.HasOne(m => m.CreadoPor)
                         .WithMany()
                         .HasForeignKey(m => m.CreadoPorId)
                         .OnDelete(DeleteBehavior.Restrict);

                        e.HasIndex(m => new { m.DocumentoId, m.Clave })
                         .IsUnique()
                         .HasDatabaseName("UQ_Documento_Clave");

                        e.Property(m => m.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
                  });

                  // --- Notificacion ---
                  modelBuilder.Entity<Notificacion>(e =>
                  {
                        e.HasOne(n => n.UsuarioDestino)
                         .WithMany()
                         .HasForeignKey(n => n.UsuarioDestinoId)
                         .OnDelete(DeleteBehavior.Cascade);

                        e.Property(n => n.Leida).HasDefaultValue(false);
                        e.Property(n => n.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
                        e.HasIndex(n => new { n.UsuarioDestinoId, n.Leida });
                  });

                  base.OnModelCreating(modelBuilder);
            }
      }
}
