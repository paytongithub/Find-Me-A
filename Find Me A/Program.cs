using Find_Me_A;
using System.Text.Json;
using System.Linq;
using Microsoft.Data.SqlClient;
using DotNetEnv;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

string? connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
app.MapGet("/login", async (HttpContext ctx) =>
{
    var html = await File.ReadAllTextAsync("wwwroot/LoginPage.html");
    return Results.Content(html, "text/html");
});

app.MapPost("/login", (HttpContext ctx) =>
{
    if (string.IsNullOrEmpty(connectionString))
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });

    // Read form fields
    string username = ctx.Request.Form["username"].ToString();
    string password = ctx.Request.Form["password"].ToString();

    var login = new Login(connectionString);
    var (success, message, userId) = login.AuthenticateUser(username, password);

    if (success)
        return Results.Ok(new { message, userId });
    else
        return Results.BadRequest(new { error = message });
});

app.MapPost("/register", (HttpContext ctx) =>
{
    if (string.IsNullOrEmpty(connectionString))
        return Results.BadRequest(new { error = "DB_CONNECTION is missing" });

    string username = ctx.Request.Form["username"].ToString();
    string password = ctx.Request.Form["password"].ToString();

    var login = new Login(connectionString);
    var (success, message) = login.RegisterUser(username, password);

    if (success)
        return Results.Ok(new { message });
    else
        return Results.BadRequest(new { error = message });
});

app.MapGet("/search", async (string query) =>
{
    var tmdb = new TMDB();

    //known genres you want to support
    string[] knownGenres =
    {
        "Action", "Comedy", "Horror", "Romance",
        "Animation", "Science Fiction", "Sci-Fi",
        "Anime", "Rom-Com"
    };

    //Genre search branch
    if (knownGenres.Contains(query, StringComparer.OrdinalIgnoreCase))
    {
        var genreJson = await tmdb.DiscoverByGenre(new[] { query });
        var genreResults = ParseDiscoverResults(genreJson);
        return Results.Ok(genreResults);
    }

    //Fallback: TMDB multi-search
    var Searchjson = await tmdb.SearchAll(query);
    var data = JsonDocument.Parse(Searchjson);

    var moviesResults = data.RootElement
        .GetProperty("results")
        .EnumerateArray()
        .Where(item =>
            item.GetProperty("media_type").GetString() == "movie" ||
            item.GetProperty("media_type").GetString() == "tv")
        .Select(item => new
        {
            id = item.GetProperty("id").GetInt32(),
            mediaType = item.GetProperty("media_type").GetString(),
            title = item.TryGetProperty("title", out var title)
                ? title.GetString()
                : item.TryGetProperty("name", out var name)
                    ? name.GetString()
                    : "Untitled",

            overview = item.TryGetProperty("overview", out var overview)
                ? overview.GetString()
                : "",

            poster = item.TryGetProperty("poster_path", out var poster) &&
                    poster.ValueKind != JsonValueKind.Null
                ? poster.GetString()
                : "",

            rating = item.TryGetProperty("vote_average", out var rating)
                ? rating.GetDouble()
                : 0,

        });

    return Results.Ok(moviesResults);
});

//helper method
static IEnumerable<object> ParseDiscoverResults(string json)
{
    var data = JsonDocument.Parse(json);

    return data.RootElement
        .GetProperty("results")
        .EnumerateArray()
        .Select(item => new
        {
            id = item.GetProperty("id").GetInt32(),
            mediaType = "movie",
            title = item.GetProperty("title").GetString(),
            overview = item.TryGetProperty("overview", out var overview)
            ? overview.GetString()
            : "",
            poster = item.TryGetProperty("poster_path", out var poster) 
            && poster.ValueKind != JsonValueKind.Null
            ? poster.GetString()
            : "",
            rating = item.TryGetProperty("vote_average", out var rating) 
            ? rating.GetDouble()
            : 0,
        });

};

// Run automated tests (returns plain text). Respects RUN_TESTS_ALLOW_DB_WRITE env var inside tests.
app.MapGet("/run-tests", async () =>
{
    try
    {
        var result = await TestRunner.RunAllTestsAsync(connectionString ?? string.Empty);
        return Results.Content(result, "text/plain");
    }
    catch (Exception ex)
    {
        return Results.Problem("Tests failed: " + ex.Message);
    }
});

// Rendered details page (server-side)
app.MapGet("/details", async (string title) =>
{
    var tmdb = new TMDB();
    Title? t = null;
    try { t = await tmdb.GetTitleByName(title); } catch { }

    var sb = new System.Text.StringBuilder();
    sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Details</title><link rel=\"stylesheet\" href=\"DetailsPage.css\"></head><body class=\"dt-body\">");
    sb.Append("<header class=\"dt-header\"><button class=\"dt-back-btn\" onclick=\"history.back()\">❮ Back</button></header><main class=\"dt-main\">");

    if (t != null)
    {
        var poster = t.PosterPath != null ? $"https://image.tmdb.org/t/p/w500{t.PosterPath}" : "https://via.placeholder.com/300x450?text=No+Image";
        sb.Append($"<img src=\"{System.Net.WebUtility.HtmlEncode(poster)}\" class=\"dt-poster\">\n");
        sb.Append($"<h1 class=\"dt-title\">{System.Net.WebUtility.HtmlEncode(t.TitleName)}</h1>\n");
        sb.Append($"<p class=\"dt-subinfo\">{(t.ReleaseDate == DateTime.MinValue ? string.Empty : t.ReleaseDate.Year.ToString())} • {(t.TitleType ?? "")}</p>\n");
        sb.Append($"<p class=\"dt-summary\">{System.Net.WebUtility.HtmlEncode(t.Overview ?? "No summary available.")}</p>\n");
        sb.Append($"<button class=\"dt-watchlist-btn\" onclick='addToWatchlist({System.Text.Json.JsonSerializer.Serialize(t.TitleName)})'>+ Add to Watchlist</button>\n");
        if (!string.IsNullOrWhiteSpace(t.ImdbId))
        {
            var imdbUrl = $"https://www.imdb.com/title/{t.ImdbId}";
            sb.Append($"<p>IMDb: <a href=\"{imdbUrl}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(t.ImdbId)}</a></p>");
        }
        if (t.Genres != null && t.Genres.Any()) sb.Append($"<p>Genres: {System.Net.WebUtility.HtmlEncode(string.Join(", ", t.Genres))}</p>");
        if (t.Actors != null && t.Actors.Any()) sb.Append($"<p>Actors: {System.Net.WebUtility.HtmlEncode(string.Join(", ", t.Actors.Take(10)))}...</p>");
    }
    else
    {
        sb.Append("<h1 class=\"dt-title\">Title not found</h1><p class=\"dt-summary\">No details available.</p>");
    }

    sb.Append("</main><nav class=\"dt-bottom-nav\"><a href=\"/\">Home</a><a href=\"/searchpage.html\">Search</a><a href=\"/watchlist\">Watchlist</a></nav>");

    // client-side script to add to watchlist with optional rating
    sb.Append(@"<script>
async function addToWatchlist(title) {
  const username = prompt('Enter your username to add to watchlist:');
  if (!username) { alert('Cancelled'); return; }
  let ratingStr = prompt('Enter a rating (1-10) or leave blank:');
  let rating = 0;
  if (ratingStr !== null && ratingStr !== '') {
    rating = parseInt(ratingStr);
    if (isNaN(rating) || rating < 0) { alert('Invalid rating'); return; }
  }
  const body = new URLSearchParams();
  body.append('username', username);
  body.append('movieTitle', title);
  body.append('rating', rating);
  try {
    const res = await fetch('/watchlist/add', {
      method: 'POST',
      headers: {'Content-Type':'application/x-www-form-urlencoded'},
      body: body.toString()
    });
    const data = await res.json();
    if (res.ok) alert(data.message || 'Added to watchlist');
    else alert(data.error || 'Failed to add');
  } catch (e) {
    alert('Request failed: ' + e.message);
  }
}
</script>");

    sb.Append("</body></html>");

    return Results.Content(sb.ToString(), "text/html");
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
                id = movie.GetProperty("id").GetInt32(),
                mediaType = "movie",
                title = movie.GetProperty("title").GetString(),
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
                id = movie.GetProperty("id").GetInt32(),
                mediaType = "movie",
                title = movie.GetProperty("title").GetString(),
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
            id = movie.GetProperty("id").GetInt32(),
            mediaType = "movie",
            title = movie.GetProperty("title").GetString(),
            poster = movie.GetProperty("poster_path").GetString(),
            rating = movie.GetProperty("vote_average").GetDouble()
        });

    return Results.Ok(movies);
});

app.MapGet("/random", async (
    string? type,
    string? genre,
    string? year,
    string? provider,
    string? rating) =>
{
    var tmdb = new TMDB();

    try
    {
        string json = await tmdb.DiscoverRandom(type, genre, year, provider, rating);

        var data = JsonDocument.Parse(json);

        if (!data.RootElement.TryGetProperty("results", out var results) ||
            results.GetArrayLength() == 0)
        {
            return Results.NotFound(new { message = "No movies found." });
        }

        var movies = results.EnumerateArray()
            .Select(item => new
            {
                title = item.TryGetProperty("title", out var title)
                    ? title.GetString()
                    : item.TryGetProperty("name", out var name)
                        ? name.GetString()
                        : "Untitled",

                overview = item.TryGetProperty("overview", out var overview)
                    ? overview.GetString()
                    : "",

                poster = item.TryGetProperty("poster_path", out var poster) &&
                         poster.ValueKind != JsonValueKind.Null
                    ? poster.GetString()
                    : "",

                rating = item.TryGetProperty("vote_average", out var vote)
                    ? vote.GetDouble()
                    : 0,
                
                releaseDate = item.TryGetProperty("release_date", out var release)
                    ? release.GetString()
                    : item.TryGetProperty("first_air_date", out var airDate)
                        ? airDate.GetString()
                        : "",
                    
                genreIds = item.GetProperty("genre_ids")
                .EnumerateArray()
                .Select(g => g.GetInt32())
                .ToList()
            })
            .ToList();

        var rng = new Random();
        var pick = movies[rng.Next(movies.Count)];

        return Results.Ok(pick);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    
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

app.MapGet("/api/details", async (string title) =>
{
    var tmdb = new TMDB();

    try
    {
        var result = await tmdb.GetTitleByName(title);

        if (result == null)
        {
            return Results.NotFound(new { error = "Title not found" });
        }

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/details-data", async (int id, string type) =>
{
    var tmdb = new TMDB();
    var result = await tmdb.GetTitleById(id, type);
    if (result == null)
        return Results.NotFound(new { error = "Title not found" });

    return Results.Ok(result);
});



app.Run();

internal static class TestRunner
{
   public static System.Threading.Tasks.Task<string> RunAllTestsAsync(string connectionString)
   {
       return System.Threading.Tasks.Task.FromResult("Test runner is unavailable in this build.");
   }
}