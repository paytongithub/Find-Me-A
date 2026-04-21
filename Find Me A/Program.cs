/*
using Find_Me_A;
using System.Text.Json;
using System.Linq;
using Microsoft.Data.SqlClient;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
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
*/
using Find_Me_A;
using System.Text.Json;
using System.Linq;
using Microsoft.Data.SqlClient;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();


string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

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
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });
    }

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
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });
    }

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
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });
    }

    try
    {
        var watchList = new WatchList(connectionString);
        watchList.UpdateRating(username, movieTitle, newRating);

        return Results.Ok(new { message = $"Updated {movieTitle} rating to {newRating}." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

/*app.MapGet("/featured", async () =>
{
    var tmdb = new TMDB();
    var json = await tmdb.GetPopularMovies();

    var data = JsonDocument.Parse(json);

    var movies = data.RootElement
        .GetProperty("results")
        .EnumerateArray()
        .Take(10)
        .Select(movie => new
        {
            title = movie.GetProperty("title").GetString(),
            poster = movie.GetProperty("poster_path").GetString(),
            rating = movie.GetProperty("vote_average").GetDouble()
        });

    return movies;
});

app.MapGet("/top-picks", async () =>
{
    var tmdb = new TMDB();
    var json = await tmdb.GetTopRatedMovies();

    var data = JsonDocument.Parse(json);

    var movies = data.RootElement
        .GetProperty("results")
        .EnumerateArray()
        .Take(10)
        .Select(movie => new
        {
            title = movie.GetProperty("title").GetString(),
            poster = movie.GetProperty("poster_path").GetString(),
            rating = movie.GetProperty("vote_average").GetDouble()
        });

    return movies;
});
*/

app.MapGet("/featured", async (string username) =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });
    }

    try
    {
        var tmdb = new TMDB();

        var watched = Recommendation.GetUserWatchedDetails(connectionString, username);
        var genreProfile = Recommendation.GetGenreRecommendationProfileFromWatched(watched);

        var topGenre = genreProfile.GenreData
            .OrderByDescending(g => g.Item3)
            .ThenByDescending(g => g.Item2)
            .FirstOrDefault();

        string json;

        if (topGenre == null)
        {
            // fallback
            json = await tmdb.GetPopularMovies();
        }
        else
        {
            json = await tmdb.DiscoverMoviesByGenre(topGenre.Item1);
        }

        var data = JsonDocument.Parse(json);

        var movies = data.RootElement
            .GetProperty("results")
            .EnumerateArray()
            .Take(10)
            .Select(movie => new
            {
                title = movie.GetProperty("title").GetString(),
                overview = movie.GetProperty("overview").GetString(),
                poster = movie.GetProperty("poster_path").GetString(),
                rating = movie.GetProperty("vote_average").GetDouble()
            });

        return Results.Ok(movies);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message); // IMPORTANT
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/top-picks", async (string username) =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });
    }

    try
    {
        var watched = Recommendation.GetUserWatchedDetails(connectionString, username);
        var actorProfile = Recommendation.GetActorRecommendationProfileFromWatched(watched);

        var topActor = actorProfile.ActorData
            .OrderByDescending(a => a.Item3)
            .ThenByDescending(a => a.Item2)
            .FirstOrDefault();

        var tmdb = new TMDB();
        string json;

        if (topActor == null)
        {
            json = await tmdb.GetTopRatedMovies();
        }
        else
        {
            json = await tmdb.DiscoverMoviesByActor(topActor.Item1);
        }

        var data = JsonDocument.Parse(json);

        var movies = data.RootElement
            .GetProperty("results")
            .EnumerateArray()
            .Take(10)
            .Select(movie => new
            {
                title = movie.GetProperty("title").GetString(),
                overview = movie.GetProperty("overview").GetString(),
                poster = movie.GetProperty("poster_path").GetString(),
                rating = movie.GetProperty("vote_average").GetDouble()
            });

        return Results.Ok(movies);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/trending", async () =>
{
    var tmdb = new TMDB();
    var json = await tmdb.GetTrendingMovies();

    var data = JsonDocument.Parse(json);

    var movies = data.RootElement
        .GetProperty("results")
        .EnumerateArray()
        .Take(10)
        .Select(movie => new
        {
            title = movie.GetProperty("title").GetString(),
            poster = movie.GetProperty("poster_path").GetString(),
            rating = movie.GetProperty("vote_average").GetDouble()
        });

    return Results.Ok(movies);
});
app.MapGet("/random", async (string? genre) =>
{
    var tmdb = new TMDB();
    var json = await tmdb.GetPopularMovies();

    var data = JsonDocument.Parse(json);

    var movies = data.RootElement
        .GetProperty("results")
        .EnumerateArray()
        .Select(movie => new
        {
            title = movie.GetProperty("title").GetString(),
            overview = movie.GetProperty("overview").GetString(),
            poster = movie.GetProperty("poster_path").GetString(),
            rating = movie.GetProperty("vote_average").GetDouble(),
            genreIds = movie.GetProperty("genre_ids").EnumerateArray().Select(g => g.GetInt32()).ToList()
        })
        .ToList();

    if (!string.IsNullOrWhiteSpace(genre) && genre != "All")
    {
        var genreMap = new Dictionary<string, int>
        {
            { "Action", 28 },
            { "Comedy", 35 },
            { "Drama", 18 },
            { "Horror", 27 }
        };

        if (genreMap.TryGetValue(genre, out int genreId))
        {
            movies = movies.Where(m => m.genreIds.Contains(genreId)).ToList();
        }
    }

    if (movies.Count == 0)
    {
        return Results.NotFound(new { message = "No movies found." });
    }

    var rng = new Random();
    var pick = movies[rng.Next(movies.Count)];

    return Results.Ok(new
    {
        title = pick.title,
        overview = pick.overview,
        poster = pick.poster,
        rating = pick.rating
    });
});
app.Run();