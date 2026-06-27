namespace Backend.Dtos
{
    public class SearchResultResponse
    {
        public string Tipo { get; set; } = null!; // "Proceso" o "Actividad"
        public int Id { get; set; }
        public string Titulo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string? DominioNombre { get; set; }
        public string? Ruta { get; set; }
    }
}
