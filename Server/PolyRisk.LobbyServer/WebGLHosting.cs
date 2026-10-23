using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

// Optional same-origin WebGL delivery. Lobby/game routes and authentication stay unchanged.
internal static class WebGLHosting
{
    public static int RequestBudget()
    {
        string? configured = Environment.GetEnvironmentVariable("POLYRISK_REQUESTS_PER_MINUTE");
        if (string.IsNullOrEmpty(configured)) return 900;
        if (!int.TryParse(configured, out int budget) || budget < 60 || budget > 3600)
            throw new InvalidOperationException("POLYRISK_REQUESTS_PER_MINUTE must be between 60 and 3600.");
        return budget;
    }

    public static string ListenUrls()
    {
        string? configured = Environment.GetEnvironmentVariable("POLYRISK_URLS");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        string? assignedPort = Environment.GetEnvironmentVariable("PORT");
        if (string.IsNullOrEmpty(assignedPort)) return "http://0.0.0.0:5080";
        if (!int.TryParse(assignedPort, out int port) || port < 1 || port > 65535)
            throw new InvalidOperationException("PORT must be an integer between 1 and 65535.");
        return $"http://0.0.0.0:{port}";
    }

    public static void UseWebGLHosting(this WebApplication app)
    {
        string? root = Environment.GetEnvironmentVariable("POLYRISK_WEBGL_ROOT");
        if (string.IsNullOrWhiteSpace(root)) return;
        string absoluteRoot = Path.GetFullPath(root);
        if (!File.Exists(Path.Combine(absoluteRoot, "index.html")))
            throw new InvalidOperationException("POLYRISK_WEBGL_ROOT must contain the exported WebGL index.html.");

        var files = new PhysicalFileProvider(absoluteRoot);
        var types = new WebGLContentTypes();
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            ContentTypeProvider = types,
            OnPrepareResponse = context =>
            {
                string path = context.Context.Request.Path.Value ?? string.Empty;
                if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    context.Context.Response.Headers.ContentEncoding = "gzip";
                else if (path.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
                    context.Context.Response.Headers.ContentEncoding = "br";
                // Unity's named build files are content-hashed. HTML and loader stay revalidated.
                context.Context.Response.Headers.CacheControl = IsHashedBuildAsset(path)
                    ? "public, max-age=31536000, immutable" : "no-cache";
            }
        });
    }

    private static bool IsHashedBuildAsset(string path)
    {
        if (!path.StartsWith("/Build/", StringComparison.Ordinal)) return false;
        string file = Path.GetFileName(path);
        int firstDot = file.IndexOf('.');
        return firstDot == 32 && file.AsSpan(0, firstDot).IndexOfAnyExcept("0123456789abcdefABCDEF") < 0;
    }

    private sealed class WebGLContentTypes : IContentTypeProvider
    {
        private readonly FileExtensionContentTypeProvider types = new();

        public WebGLContentTypes()
        {
            types.Mappings[".wasm"] = "application/wasm";
            types.Mappings[".data"] = "application/octet-stream";
            types.Mappings[".unityweb"] = "application/octet-stream";
        }

        public bool TryGetContentType(string subpath, out string contentType)
        {
            string plain = subpath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                || subpath.EndsWith(".br", StringComparison.OrdinalIgnoreCase) ? subpath[..^3] : subpath;
            return types.TryGetContentType(plain, out contentType!);
        }
    }
}
