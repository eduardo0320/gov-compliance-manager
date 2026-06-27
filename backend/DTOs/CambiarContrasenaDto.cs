using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace backend.DTOs
{
    public class CambiarContrasenaDto : IValidatableObject
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        public string ContrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]).{8,}$", 
            ErrorMessage = "La nueva contraseña debe contener al menos: 1 mayúscula, 1 minúscula, 1 número y 1 símbolo")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(NuevaContrasena) && !string.IsNullOrEmpty(ConfirmarContrasena))
            {
                if (NuevaContrasena != ConfirmarContrasena)
                {
                    yield return new ValidationResult(
                        "Las contraseñas no coinciden",
                        new[] { nameof(ConfirmarContrasena) });
                }
            }
        }
    }
}
