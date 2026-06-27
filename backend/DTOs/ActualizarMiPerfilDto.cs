using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    /// <summary>
    /// DTO para que un usuario actualice su propia información personal
    /// Solo incluye campos editables por el propio usuario
    /// </summary>
    public class ActualizarMiPerfilDto : IValidatableObject
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [MaxLength(40, ErrorMessage = "El nombre no puede exceder 40 caracteres")]
        [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres")]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        [MaxLength(50, ErrorMessage = "El correo electrónico no puede exceder 50 caracteres")]
        public string correo_electronico { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "El departamento no puede exceder 50 caracteres")]
        public string? departamento { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validación adicional de formato de email
            if (!string.IsNullOrEmpty(correo_electronico))
            {
                var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(correo_electronico, emailPattern))
                {
                    yield return new ValidationResult(
                        "El formato del correo electrónico no es válido",
                        new[] { nameof(correo_electronico) });
                }
            }

            // Validación de que el nombre no contenga números ni caracteres especiales
            if (!string.IsNullOrEmpty(nombre))
            {
                var nombrePattern = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(nombre, nombrePattern))
                {
                    yield return new ValidationResult(
                        "El nombre solo puede contener letras y espacios",
                        new[] { nameof(nombre) });
                }
            }
        }
    }
}
