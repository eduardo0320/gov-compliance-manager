namespace Backend.Dtos
{
    public class DominioTreeDto
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string? description { get; set; }
        public string icon { get; set; } = "fas fa-folder";
        public List<ProcesoNodeDto> processes { get; set; } = new();
    }

    public class ProcesoNodeDto
    {
        public int id { get; set; }
        public string code { get; set; } = "";
        public string name { get; set; } = "";
        public List<ActivityNodeDto> activities { get; set; } = new();
    }

    public class ActivityNodeDto
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public int subdominioId { get; set; }
        public string subdominioName { get; set; } = "";
    }

    public class DominioTreeWithSubDto
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string icon { get; set; } = "fas fa-folder";
        public List<ProcesoWithSubDto> processes { get; set; } = new();
    }

    public class ProcesoWithSubDto
    {
        public int id { get; set; }
        public string code { get; set; } = "";
        public string name { get; set; } = "";
        public List<SubdominioNodeDto> subdominios { get; set; } = new();
    }

    public class SubdominioNodeDto
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public List<ActivityNodeDto> activities { get; set; } = new();
    }
}
