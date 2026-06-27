-- Esquema de Base de Datos - Sistema de Normas MICITT
-- Creación de todas las tablas necesarias

-- Tabla de roles
CREATE TABLE IF NOT EXISTS `rol` (
    `idRol` int NOT NULL AUTO_INCREMENT,
    `nombre` varchar(100) COLLATE utf8mb4_general_ci NOT NULL,
    PRIMARY KEY (`idRol`),
    UNIQUE KEY `nombre` (`nombre`)
) ENGINE = InnoDB AUTO_INCREMENT = 13 DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_general_ci;

-- Tabla de usuarios
CREATE TABLE IF NOT EXISTS `usuarios` (
    `Id_Usuario` int NOT NULL AUTO_INCREMENT,
    `cedula` varchar(20) NOT NULL,
    `nombre` varchar(40) NOT NULL,
    `correo_electronico` varchar(50) NOT NULL,
    `departamento` varchar(50) DEFAULT NULL,
    `idRol` int NOT NULL,
    `contrasena` varchar(255) NOT NULL,
    `estado` tinyint(1) NOT NULL DEFAULT '1',
    `fechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `fechaUltimaModificacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `ultimoAcceso` datetime DEFAULT NULL,
    `intentosLoginFallidos` int DEFAULT '0',
    `fechaBloqueado` datetime DEFAULT NULL,
    PRIMARY KEY (`Id_Usuario`),
    UNIQUE KEY `correo_electronico` (`correo_electronico`),
    KEY `FK_Usuario_Rol` (`idRol`),
    CONSTRAINT `FK_Usuario_Rol` FOREIGN KEY (`idRol`) REFERENCES `rol` (`idRol`)
) ENGINE = InnoDB AUTO_INCREMENT = 29 DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

-- Tabla de dominios
CREATE TABLE IF NOT EXISTS `dominio` (
    `id_Dominio` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(255) NOT NULL,
    PRIMARY KEY (`id_Dominio`)
) ENGINE = InnoDB AUTO_INCREMENT = 6 DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

-- Tabla de procesos
CREATE TABLE IF NOT EXISTS `proceso` (
  id_Proceso INT NOT NULL AUTO_INCREMENT,
  Codigo VARCHAR(20) NOT NULL,
  Nombre VARCHAR(500) NOT NULL,
  MarcoNormativo VARCHAR(500) NOT NULL,
  
  EstadoImplementacion VARCHAR(12) NOT NULL DEFAULT 'Sí',
  
  PorcentajeAvance DECIMAL(5,2) NOT NULL DEFAULT 0.00
    CHECK (PorcentajeAvance >= 0 AND PorcentajeAvance <= 100),
  
  PrioridadImplementacion INT NOT NULL DEFAULT 0,
  
  FechaConclusionImplementacion DATETIME NULL,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaModificacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  
  CreadoPorId INT NOT NULL,
  ModificadoPorId INT NOT NULL,
  DominioId INT NOT NULL,
  
  PRIMARY KEY (id_Proceso),
  UNIQUE KEY UQ_Proceso_Codigo (Codigo),
  
  KEY FK_Proceso_CreadoPor (CreadoPorId),
  KEY FK_Proceso_ModificadoPor (ModificadoPorId),
  KEY FK_Proceso_Dominio (DominioId),
  
  CONSTRAINT FK_Proceso_CreadoPor FOREIGN KEY (CreadoPorId)
    REFERENCES usuarios (Id_Usuario)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  
  CONSTRAINT FK_Proceso_ModificadoPor FOREIGN KEY (ModificadoPorId)
    REFERENCES usuarios (Id_Usuario)
    ON DELETE RESTRICT ON UPDATE CASCADE,
  
  CONSTRAINT FK_Proceso_Dominio FOREIGN KEY (DominioId)
    REFERENCES dominio (id_Dominio)
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB 
  DEFAULT CHARSET=utf8mb4 
  COLLATE=utf8mb4_0900_ai_ci;


-- Tabla de subdominios
CREATE TABLE IF NOT EXISTS `subdominio` (
  `id_Subdominio` int NOT NULL AUTO_INCREMENT,
  `PracticasGobierno` varchar(255) NOT NULL,
  `indicadoresAsociados` varchar(500) NOT NULL,
  `ProcesoId` int NOT NULL,
  PRIMARY KEY (`id_Subdominio`),
  KEY `FK_Subdominio_Proceso` (`ProcesoId`),
  CONSTRAINT `FK_Subdominio_Proceso` FOREIGN KEY (`ProcesoId`) REFERENCES `proceso` (`id_Proceso`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Tabla de actividades
CREATE TABLE IF NOT EXISTS `actividad` (
  `id_Actividad` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(500) NOT NULL,
  `Implementable` enum('Sí','No') NOT NULL DEFAULT 'Sí',
  `FechaCompromiso` datetime DEFAULT NULL,
  `EstadoImplementacion` enum('Pendiente','En Progreso','En Revisión','Implementado') NOT NULL DEFAULT 'Pendiente',
  `PorcentajeAvance` decimal(5,2) NOT NULL DEFAULT '0.00',
  `FuncionariosResponsablesId` int NOT NULL,
  `FechaControl` datetime DEFAULT NULL,
  `Documentos` text,
  `Observaciones` text,
  `SubdominioId` int NOT NULL,
  PRIMARY KEY (`id_Actividad`),
  KEY `FK_Actividad_Responsable` (`FuncionariosResponsablesId`),
  KEY `FK_Actividad_Subdominio` (`SubdominioId`),
  CONSTRAINT `FK_Actividad_Responsable` FOREIGN KEY (`FuncionariosResponsablesId`) REFERENCES `usuarios` (`Id_Usuario`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Actividad_Subdominio` FOREIGN KEY (`SubdominioId`) REFERENCES `subdominio` (`id_Subdominio`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Tabla de auditoría
CREATE TABLE IF NOT EXISTS `auditoria` (
    `id_Auditoria` int NOT NULL AUTO_INCREMENT,
    `descripcion` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `fecha_evento` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `id_usuario` int NULL,
    `tipo_evento` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `modulo` varchar(100) CHARACTER SET utf8mb4 NULL,
    `direccion_ip` varchar(50) CHARACTER SET utf8mb4 NULL,
    `navegador` varchar(500) CHARACTER SET utf8mb4 NULL,
    `datos_anteriores` longtext CHARACTER SET utf8mb4 NULL,
    `datos_nuevos` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_auditoria` PRIMARY KEY (`id_Auditoria`),
    CONSTRAINT `FK_auditoria_usuarios` FOREIGN KEY (`id_usuario`) REFERENCES `usuarios` (`Id_Usuario`)
) CHARACTER SET=utf8mb4;

-- Reiniciar contadores de AUTO_INCREMENT para tablas existentes
ALTER TABLE `rol` AUTO_INCREMENT = 1;
ALTER TABLE `usuarios` AUTO_INCREMENT = 1;
ALTER TABLE `dominio` AUTO_INCREMENT = 1;
ALTER TABLE `proceso` AUTO_INCREMENT = 1;
ALTER TABLE `subdominio` AUTO_INCREMENT = 1;
ALTER TABLE `actividad` AUTO_INCREMENT = 1;
ALTER TABLE `auditoria` AUTO_INCREMENT = 1;
