using PinguApps.ObjectStorage;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddObjectStorage(builder.Configuration);

WebApplication app = builder.Build();

app.MapGet("/", (IObjectStorage storage) =>
{
    string sampleKey = "uploads/latest";
    string publicUrl = storage.GetPublicUrl(sampleKey);
    return Results.Content($"""
        <!doctype html>
        <html>
        <body>
            <form method="post" enctype="multipart/form-data" action="/upload">
                <input type="file" name="file" accept="image/*" />
                <button type="submit">Upload</button>
            </form>
            <img src="{publicUrl}" alt="Latest upload" style="max-width: 480px;" />
            <p><a href="/read">Read latest upload</a></p>
        </body>
        </html>
        """, "text/html");
});

app.MapPost("/upload", async (IFormFile file, IObjectStorage storage, CancellationToken cancellationToken) =>
{
    await using Stream stream = file.OpenReadStream();
    await storage.PutAsync("uploads/latest", stream, file.ContentType, cancellationToken);
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapGet("/read", async (IObjectStorage storage, CancellationToken cancellationToken) =>
{
    if (!await storage.ExistsAsync("uploads/latest", cancellationToken))
    {
        return Results.NotFound("No upload exists yet.");
    }

    Stream stream = await storage.OpenReadAsync("uploads/latest", cancellationToken);
    return Results.File(stream, "application/octet-stream", "latest");
});

app.Run();
