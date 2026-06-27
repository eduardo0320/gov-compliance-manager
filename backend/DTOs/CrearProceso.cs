public class CrearProcesoRequest
{
    public int DominioId { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string MarcoNormativo { get; set; } = null!;
    public string EstadoImplementacion { get; set; } = "Sí"; // ENUM Sí/No en BD
    // Prioridad de implementación: 1..3 o null
    public int? PrioridadImplementacion { get; set; } = null;

    public List<CrearSubdominioDto> Subdominios { get; set; } = new();
}

public class CrearSubdominioDto
{
    public string PracticasGobierno { get; set; } = null!;
    public string IndicadoresAsociados { get; set; } = null!;
}

public class ProcesoResponse
{
    public int IdProceso { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string MarcoNormativo { get; set; } = null!;
    public string EstadoImplementacion { get; set; } = null!;
    public decimal PorcentajeAvance { get; set; }
    public int DominioId { get; set; }
    public List<SubdominioResponse> Subdominios { get; set; } = new();
}

public class SubdominioResponse
{
    public int IdSubdominio { get; set; }
    public string PracticasGobierno { get; set; } = null!;
    public string IndicadoresAsociados { get; set; } = null!;
}

public class EditarProcesoRequest
{
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? MarcoNormativo { get; set; }
    public string? EstadoImplementacion { get; set; }
    public int DominioId { get; set; }
    // Permitir editar la prioridad
    public int? PrioridadImplementacion { get; set; }
}

public class ActualizarAvanceActividadRequest
{
    public decimal Porcentaje { get; set; } // 0..100
}

