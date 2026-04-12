using Find_Me_A;
using System.Text.Json;
using System.Linq;
using Microsoft.Data.SqlClient;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
var app = builder.Build();

string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
using var conn = new SqlConnection(connectionString);

app.MapGet("/search", async (string query) =>
{
    var tmdb = new TMDB();
    var json = await tmdb.SearchMovies(query);

    var data = JsonDocument.Parse(json);

    var movies = data.RootElement
        .GetProperty("results")
        .EnumerateArray()
        .Select(movie => new
        {
            title = movie.GetProperty("title").GetString(),
            overview = movie.GetProperty("overview").GetString(),
            poster = movie.GetProperty("poster_path").GetString(),
            rating = movie.GetProperty("vote_average").GetDouble()
        });

    return movies;
});

app.MapGet("/db-test", () =>
{
    try
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();

        return Results.Ok(new
        {
            success = true,
            message = "Database connection successful."
        });
    }
    catch (Exception ex)
    {
        return Results.Problem("Database connection failed: " + ex.Message);
    }
});

app.MapPost("/watchlist/add", (string username, string movieTitle, int rating) => 
{
    try 
    {
        var watchList = new WatchList(connectionString);
        
        watchList.AddToWatchList(username, movieTitle, DateTime.Now, rating);
        
        return Results.Ok(new { message = $"Successfully added {movieTitle} for {username}" });
    }
    catch (Exception ex) 
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapDelete("/watchlist/remove", (string username, string movieTitle) => 
{
    string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });
    }

    try 
    {
        var watchList = new WatchList(connectionString);
        
        watchList.RemoveFromWatchList(username, movieTitle);
        
        return Results.Ok(new { message = $"Removed {movieTitle} from {username}'s list." });
    }
    catch (Exception ex) 
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/watchlist/update", (string username, string movieTitle, int newRating) => 
{
    string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

    try 
    {
        var watchList = new WatchList(connectionString ?? "");
        
        watchList.UpdateRating(username, movieTitle, newRating);
        
        return Results.Ok(new { message = $"Updated {movieTitle} rating to {newRating}." });
    }
    catch (Exception ex) 
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();