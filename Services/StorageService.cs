using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace AlmaInter.Services
{
    public interface IStorageService
    {
        Task<string> SubirImagenAsync(IFormFile archivo, CancellationToken ct = default);
        Task EliminarAsync(string? urlPublica, CancellationToken ct = default);
    }
    public class StorageService : IStorageService
    {
        private readonly HttpClient _http;
        private readonly SupabaseOptions _opciones;
        private readonly ILogger<StorageService> _logger;

        public StorageService(HttpClient http, IOptions<SupabaseOptions> opciones, ILogger<StorageService> logger)
        {
            _opciones = opciones.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_opciones.Url) ||
                string.IsNullOrWhiteSpace(_opciones.SecretKey) ||
                string.IsNullOrWhiteSpace(_opciones.Bucket))
            {
                throw new InvalidOperationException(
                    "Falta configurar Supabase:Url, Supabase:SecretKey o Supabase:Bucket.");
            }

            _http = http;
            _http.BaseAddress = new Uri(_opciones.Url.TrimEnd('/') + "/");
            _http.DefaultRequestHeaders.Add("apikey", _opciones.SecretKey);

            // Las claves antiguas (JWT) también van como Bearer; las nuevas (sb_secret_...) solo en apikey
            if (!_opciones.SecretKey.StartsWith("sb_", StringComparison.Ordinal))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _opciones.SecretKey);
            }
        }

        public async Task<string> SubirImagenAsync(IFormFile archivo, CancellationToken ct = default)
        {
            await using var stream = archivo.OpenReadStream();

            var tipo = DetectarTipo(stream)
                ?? throw new InvalidOperationException("Formato de imagen no permitido. Usa JPG, PNG o WebP.");

            var ruta = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}.{tipo.Extension}";

            using var contenido = new StreamContent(stream);
            contenido.Headers.ContentType = new MediaTypeHeaderValue(tipo.ContentType);

            using var peticion = new HttpRequestMessage(HttpMethod.Post,
                $"storage/v1/object/{_opciones.Bucket}/{ruta}")
            {
                Content = contenido
            };

            using var respuesta = await _http.SendAsync(peticion, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                var detalle = await respuesta.Content.ReadAsStringAsync(ct);
                _logger.LogError("Error al subir imagen a Supabase: {Estado} {Detalle}",
                    (int)respuesta.StatusCode, detalle);
                throw new InvalidOperationException("No se pudo subir la imagen. Intenta de nuevo.");
            }

            return $"{_opciones.Url.TrimEnd('/')}/storage/v1/object/public/{_opciones.Bucket}/{ruta}";
        }

        public async Task EliminarAsync(string? urlPublica, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(urlPublica))
                return;

            var prefijo = $"{_opciones.Url.TrimEnd('/')}/storage/v1/object/public/{_opciones.Bucket}/";
            if (!urlPublica.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                return;

            var ruta = urlPublica[prefijo.Length..];

            try
            {
                using var respuesta = await _http.DeleteAsync($"storage/v1/object/{_opciones.Bucket}/{ruta}", ct);
                if (!respuesta.IsSuccessStatusCode)
                {
                    _logger.LogWarning("No se pudo borrar la imagen {Ruta}: {Estado}",
                        ruta, (int)respuesta.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Una imagen huérfana no debe romper la operación del usuario
                _logger.LogWarning(ex, "No se pudo borrar la imagen {Ruta}", ruta);
            }
        }

        private readonly record struct TipoImagen(string ContentType, string Extension);

        // Se identifica el formato por los primeros bytes del archivo, no por lo que declara el navegador
        private static TipoImagen? DetectarTipo(Stream stream)
        {
            Span<byte> h = stackalloc byte[12];
            var leidos = stream.ReadAtLeast(h, h.Length, throwOnEndOfStream: false);
            stream.Position = 0;

            if (leidos < 12)
                return null;

            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)
                return new TipoImagen("image/jpeg", "jpg");

            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47)
                return new TipoImagen("image/png", "png");

            if (h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
                h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P')
                return new TipoImagen("image/webp", "webp");

            return null;
        }
    }
}
