using System.Globalization;
using System.Text.Json;
using Json_Demo.Models;
using Microsoft.AspNetCore.Mvc;

namespace Json_Demo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public UsuariosController()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }


        // GET /api/usuarios
   
        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            try
            {
                var users = await GetUsersFromApi();

                if (users.Count == 0)
                {
                    return NotFound(new { mensaje = "No se encontraron usuarios" });
                }

                var usuarios = users
                    .Select(u => new
                    {
                        id = u.Id,
                        nombreCompleto = u.Name,
                        username = u.Username,
                        email = u.Email,
                        ciudad = u.Address.City,
                        empresa = u.Company.Name,
                        telefono = FormatearTelefono(u.Phone)
                    })
                    .OrderBy(u => u.nombreCompleto, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                object? distribucionPorCiudad = null;

                if (usuarios.Count > 5)
                {
                    distribucionPorCiudad = usuarios
                        .GroupBy(u => u.ciudad ?? "")
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.Count());
                }

                return Ok(new
                {
                    totalUsuarios = usuarios.Count,
                    distribucionPorCiudad = distribucionPorCiudad,
                    usuarios = usuarios
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new
                {
                    error = "No se pudo conectar con el servicio externo",
                    detalle = ex.Message
                });
            }
            catch (JsonException)
            {
                return StatusCode(500, new { error = "Error al procesar los datos recibidos" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Ocurrió un error interno" });
            }
        }


        // GET /api/usuarios/ciudad?nombre={texto}

        [HttpGet("ciudad")]
        public async Task<IActionResult> BuscarPorCiudad([FromQuery] string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    return BadRequest(new
                    {
                        error = "Debes enviar el parámetro 'nombre'",
                        ejemplo = "/api/usuarios/ciudad?nombre=Gwen"
                    });
                }

                var users = await GetUsersFromApi();

                var encontrados = users
                    .Where(u => (u.Address.City ?? "")
                        .Contains(nombre, StringComparison.OrdinalIgnoreCase))
                    .Select(u => new
                    {
                        id = u.Id,
                        nombre = u.Name,
                        ciudad = u.Address.City,
                        coordenadas = $"{u.Address.Geo.Lat}, {u.Address.Geo.Lng}"
                    })
                    .OrderBy(u => u.ciudad, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(u => u.nombre, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Ok(new
                {
                    textoBuscado = nombre,
                    totalEncontrados = encontrados.Count,
                    usuarios = encontrados
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error al procesar la búsqueda por ciudad" });
            }
        }


        // GET /api/usuarios/cercanos?lat={lat}&lng={lng}&radio={km}

        [HttpGet("cercanos")]
        public async Task<IActionResult> BuscarCercanos([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radio)
        {
            try
            {
                if (radio <= 0)
                {
                    return BadRequest(new
                    {
                        error = "El radio debe ser mayor que 0",
                        ejemplo = "/api/usuarios/cercanos?lat=40.7128&lng=-74.0060&radio=500"
                    });
                }

                var users = await GetUsersFromApi();

                var encontrados = new List<(User u, double distanciaKm)>();

                foreach (var u in users)
                {
                    if (!TryParseDouble(u.Address.Geo.Lat, out var latU)) continue;
                    if (!TryParseDouble(u.Address.Geo.Lng, out var lngU)) continue;

                    var distanciaKm = Math.Sqrt(Math.Pow(latU - lat, 2) + Math.Pow(lngU - lng, 2)) * 100;

                    if (distanciaKm <= radio)
                        encontrados.Add((u, distanciaKm));
                }

                var lista = encontrados
                    .OrderBy(x => x.distanciaKm)
                    .Select(x => new
                    {
                        id = x.u.Id,
                        nombre = x.u.Name,
                        ciudad = x.u.Address.City,
                        coordenadas = $"{x.u.Address.Geo.Lat}, {x.u.Address.Geo.Lng}",
                        distanciaAproximada = $"{Math.Round(x.distanciaKm)} km",
                        ubicacion = $"https://maps.google.com/?q={x.u.Address.Geo.Lat},{x.u.Address.Geo.Lng}"
                    })
                    .ToList();

                return Ok(new
                {
                    centroBusqueda = new { lat, lng },
                    radioBuscado = $"{radio} km",
                    totalEncontrados = lista.Count,
                    usuarios = lista
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error al calcular usuarios cercanos" });
            }
        }

        
        // GET /api/usuarios/{id}/tarjeta
      
        [HttpGet("{id:int}/tarjeta")]
        public async Task<IActionResult> Tarjeta(int id)
        {
            try
            {
                if (id <= 0 || id > 10)
                {
                    return BadRequest(new
                    {
                        error = "El id de usuario debe estar entre 1 y 10",
                        ejemplo = "/api/usuarios/1/tarjeta"
                    });
                }

                var response = await _httpClient.GetAsync($"/users/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(503, new
                    {
                        error = "El servicio externo no está disponible",
                        detalle = $"Código: {response.StatusCode}"
                    });
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(jsonString, _jsonOptions);

                if (user == null)
                {
                    return NotFound(new { mensaje = $"No existe el usuario con ID {id}" });
                }

                return Ok(new
                {
                    tarjeta = new
                    {
                        encabezado = new
                        {
                            nombre = (user.Name ?? "").ToUpperInvariant(),
                            usuario = "@" + (user.Username ?? "")
                        },
                        contacto = new
                        {
                            email = user.Email,
                            telefono = FormatearTelefono(user.Phone),
                            sitioWeb = AsegurarHttps(user.Website)
                        },
                        direccion = new
                        {
                            completa = $"{user.Address.Street}, {user.Address.Suite} - {user.Address.City}, {user.Address.Zipcode}",
                            geo = $"{user.Address.Geo.Lat}, {user.Address.Geo.Lng}"
                        },
                        empresa = new
                        {
                            nombre = user.Company.Name,
                            lema = $"\"{user.Company.CatchPhrase}\"",
                            giro = user.Company.Bs
                        },
                        mapa = $"https://maps.google.com/?q={user.Address.Geo.Lat},{user.Address.Geo.Lng}"
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error al generar la tarjeta" });
            }
        }

  
        // GET /api/usuarios/telefonos
      
        [HttpGet("telefonos")]
        public async Task<IActionResult> Telefonos()
        {
            try
            {
                var users = await GetUsersFromApi();

                var directorio = users
                    .Select(u => new
                    {
                        id = u.Id,
                        nombre = u.Name,
                        telefonoOriginal = u.Phone,
                        telefonoFormateado = FormatearTelefono(u.Phone),
                        tieneExtension = TieneExtension(u.Phone),
                        email = u.Email
                    })
                    .OrderBy(x => x.nombre, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return Ok(new
                {
                    totalContactos = directorio.Count,
                    contactosConExtension = directorio.Count(x => x.tieneExtension),
                    contactosSinExtension = directorio.Count(x => !x.tieneExtension),
                    directorio = directorio
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Error al generar el directorio telefónico" });
            }
        }


        private async Task<List<User>> GetUsersFromApi()
        {
            var response = await _httpClient.GetAsync("/users");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error externo: {response.StatusCode}");

            var jsonString = await response.Content.ReadAsStringAsync();

            var users = JsonSerializer.Deserialize<List<User>>(jsonString, _jsonOptions);

            return users ?? new List<User>();
        }

    
        private static bool TieneExtension(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            return phone.Contains('x') || phone.Contains('X');
        }

        private static string FormatearTelefono(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";

            var p = phone.Trim();

            // requisito: reemplazar "x" por "ext."
            p = p.Replace(" x", " ext. ", StringComparison.OrdinalIgnoreCase)
                 .Replace("X", " ext. ", StringComparison.OrdinalIgnoreCase)
                 .Replace("x", " ext. ", StringComparison.OrdinalIgnoreCase);

            while (p.Contains("  "))
                p = p.Replace("  ", " ");

            return p.Trim();
        }

        private static string AsegurarHttps(string? website)
        {
            if (string.IsNullOrWhiteSpace(website)) return "";
            var w = website.Trim();

            if (w.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                w.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return w;

            return "https://" + w;
        }

        private static bool TryParseDouble(string? s, out double value)
        {
            return double.TryParse(
                s,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value
            );
        }
    }
}
