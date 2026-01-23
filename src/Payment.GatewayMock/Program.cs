var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();

app.MapPost("/process", async (HttpRequest req) =>
{
    // simple echo and success
    var payload = await req.ReadFromJsonAsync<object>();
    return Results.Ok(new { success = true, payload });
});

app.Run();
