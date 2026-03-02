namespace Json_Demo.Models
{
    // DTO ÚNICO para responder en todos los endpoints de usuarios
    public class UsuarioResponse
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Ciudad { get; set; } = string.Empty;

        public string Empresa { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Coordenadas { get; set; } = string.Empty;

        public string DistanciaAproximada { get; set; } = string.Empty;

        public bool TieneExtension { get; set; }

        public string Ubicacion { get; set; } = string.Empty;

        public string TelefonoOriginal { get; set; } = string.Empty;
    }
}