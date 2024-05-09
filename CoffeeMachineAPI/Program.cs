var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

int requestCount = 0;

app.MapGet("/brew-coffee", () =>
{
    var now = DateTime.UtcNow;
    // on April 1st
    if (now.Month == 4 && now.Day == 1)
    {
        return Results.StatusCode(418); 
    }
    // secure the thread in case of race condition
    var count = Interlocked.Increment(ref requestCount);
    
    // every 5th request
    if (count % 5 == 0)
    {
        return Results.StatusCode(503); 
    }

    var response = new
    {
        Message = "Your piping hot coffee is ready",
        Prepared = now.ToString("yyyy-MM-ddTHH:mm:sszzz")
    };
    return Results.Json(response);
});

app.Run();
