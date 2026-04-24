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
            json = await tmdb.GetPopularMovies();
        }
        else
        {
            string genreName = "";


            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();


                using var cmd = new SqlCommand(
                    "SELECT GenreName FROM Genres WHERE GenreID = @GenreID", conn);


                cmd.Parameters.AddWithValue("@GenreID", topGenre.Item1);


                var result = cmd.ExecuteScalar();
                genreName = result?.ToString() ?? "";
            }


            if (string.IsNullOrWhiteSpace(genreName))
            {
                json = await tmdb.GetPopularMovies();
            }
            else
            {
                json = await tmdb.DiscoverByGenre(new[] { genreName });
            }
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
        Console.WriteLine(ex.Message);
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
            string actorName = "";


            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();


                using var cmd = new SqlCommand(
                    "SELECT ActorName FROM Actors WHERE ActorID = @ActorID", conn);


                cmd.Parameters.AddWithValue("@ActorID", topActor.Item1);


                var result = cmd.ExecuteScalar();
                actorName = result?.ToString() ?? "";
            }


            if (string.IsNullOrWhiteSpace(actorName))
            {
                json = await tmdb.GetTopRatedMovies();
            }
            else
            {
                json = await tmdb.DiscoverByActor(new[] { actorName });
            }
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
        Console.WriteLine(ex.Message);
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

app.MapGet("/users", () =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });
    }

    try
    {
        var users = new List<string>();

        using var conn = new SqlConnection(connectionString);
        conn.Open();

        string sql = "SELECT Username FROM Users ORDER BY Username";

        using var cmd = new SqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            users.Add(reader["Username"].ToString() ?? "");
        }

        return Results.Ok(users);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/watchlist", async (string username) =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });
    }

    try
    {
        var watchList = new WatchList(connectionString);
        var items = watchList.GetWatchedList(username);
        var tmdb = new TMDB();

        var result = new List<object>();

        foreach (var item in items)
        {
            string? poster = null;

            try
            {
                poster = await tmdb.GetPosterPathByTitleName(item.Title.TitleName);
            }
            catch
            {
                poster = null;
            }

            result.Add(new
            {
                titleName = item.Title.TitleName,
                titleType = item.Title.TitleType,
                averageRating = item.Title.AverageRating,
                userRating = item.UserRating,
                watchedDate = item.WatchedDate.ToString("yyyy-MM-dd"),
                genres = item.Title.Genres,
                actors = item.Title.Actors,
                poster = poster
            });
        }

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});


app.Run();