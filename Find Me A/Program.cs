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

app.Run();