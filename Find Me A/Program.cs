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

// Rendered watchlist page (server-side C# HTML)
app.MapGet("/watchlist", async (string? username) =>
{
    // if username not provided, show a simple form to enter username
    if (string.IsNullOrWhiteSpace(username))
    {
        var form = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Enter username</title></head><body>" +
                   "<h1>Enter username</h1>" +
                   "<form method=\"get\" action=\"/watchlist\">" +
                   "<input name=\"username\" placeholder=\"Username\"/>" +
                   "<button type=\"submit\">View Watchlist</button>" +
                   "</form></body></html>";
        return Results.Content(form, "text/html");
    }

    string? cs = Environment.GetEnvironmentVariable("DB_CONNECTION");
    if (string.IsNullOrWhiteSpace(cs)) return Results.Problem("DB_CONNECTION not set");

    var wl = new WatchList(cs);
    var items = wl.GetWatchedList(username);

    var tmdb = new TMDB();

    // Build simple HTML showing posters only
    var sb = new System.Text.StringBuilder();
    sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Watchlist for ");
    sb.Append(System.Net.WebUtility.HtmlEncode(username));
    sb.Append("</title><link rel=\"stylesheet\" href=\"WatchlistPage.css\"></head><body class=\"wl-body\">");
    sb.Append($"<header class=\"wl-header\"><h1 class=\"wl-logo\">Find Me A</h1></header><main class=\"wl-main\"><section class=\"wl-section\"><h2 class=\"wl-section-title\">Watchlist</h2><div class=\"wl-carousel-container\"><div class=\"wl-carousel\">");

    foreach (var wi in items)
    {
        var titleName = wi.Title?.TitleName ?? string.Empty;
        Title? details = null;
        try
        {
            details = await tmdb.GetTitleByName(titleName);
        }
        catch { }

        var poster = details?.PosterPath != null ? $"https://image.tmdb.org/t/p/w300{details.PosterPath}" : "https://via.placeholder.com/150x220?text=No+Image";

        var href = $"/details?title={System.Net.WebUtility.UrlEncode(titleName)}";
        sb.Append($"<a class=\"wl-poster-link\" href=\"{href}\"><img class=\"wl-poster\" src=\"{System.Net.WebUtility.HtmlEncode(poster)}\" alt=\"{System.Net.WebUtility.HtmlEncode(titleName)}\"></a>");
    }

    sb.Append("</div></div></section></main><nav class=\"wl-bottom-nav\"><a href=\"/\">Home</a><a href=\"/searchpage.html\">Search</a><a class=\"active\" href=\"/watchlist\">Watchlist</a></nav></body></html>");

    return Results.Content(sb.ToString(), "text/html");
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
        sb.Append($"<p class=\"dt-subinfo\">{(t.ReleaseDate==DateTime.MinValue?string.Empty:t.ReleaseDate.Year.ToString())} • {(t.TitleType??"")}</p>\n");
        sb.Append($"<p class=\"dt-summary\">{System.Net.WebUtility.HtmlEncode(t.Overview ?? "No summary available.")}</p>\n");
        // use single quotes around the onclick attribute so serialized title (which has double quotes) is valid
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