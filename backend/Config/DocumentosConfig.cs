namespace backend.Config;

public class DocumentosConfig
{
    public string RutaRepositorio { get; set; } = string.Empty;
    public string RutaDominios { get; set; } = string.Empty;
    public string RutaTemporal { get; set; } = string.Empty;
    public string RutaBackups { get; set; } = string.Empty;
    public int TamanoMaximoMB { get; set; } = 50;
    public string[] ExtensionesPermitidas { get; set; } = [".pdf", ".docx", ".pptx"];
    public EstructuraJerarquicaConfig EstructuraJerarquica { get; set; } = new();
    public bool HabilitarBackupAutomatico { get; set; } = true;
    public int IntervaloBackupDias { get; set; } = 7;
    public MantenimientoConfig MantenimientoAutomatico { get; set; } = new();
    public SeguridadConfig Seguridad { get; set; } = new();
}

public class EstructuraJerarquicaConfig
{
    public bool UsarNombresEnRutas { get; set; } = true;
    public int MaxCaracteresNombre { get; set; } = 50;
    public bool SanitizarNombres { get; set; } = true;
}

public class MantenimientoConfig
{
    public int LimpiarTempCadaHoras { get; set; } = 24;
    public int EliminarBackupsAntiguosDias { get; set; } = 90;
}

public class SeguridadConfig
{
    public bool CalcularChecksums { get; set; } = true;
    public bool ValidarIntegridad { get; set; } = true;
    public bool RequireAntivirusScan { get; set; } = false;
}
