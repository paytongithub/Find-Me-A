using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Find_Me_A;


public class TMDB
{
    private readonly string? apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY");
    private readonly string baseUrl = "https://api.themoviedb.org/3";

    private readonly HttpClient client = new HttpClient();

    public async Task<string> SearchMovies(string query)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"{baseUrl}/search/movie?api_key={apiKey}&query={encodedQuery}";

        var response = await client.GetStringAsync(url);
        return response;
    }

    // Fetch a Title object (your project's Title model) from TMDB by name.
    // Returns null if no matching movie or TV show is found.
    public async Task<Title?> GetTitleByName(string titleName)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("TMDB_API_KEY environment variable is not set.");

        if (string.IsNullOrWhiteSpace(titleName))
            return null;

        // Try movie search first
        var encoded = Uri.EscapeDataString(titleName);
        var movieSearchUrl = $"{baseUrl}/search/movie?api_key={apiKey}&query={encoded}";
        var movieSearchJson = await client.GetStringAsync(movieSearchUrl);
        using (var doc = JsonDocument.Parse(movieSearchJson))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                var id = first.GetProperty("id").GetInt32();
                // Get details and credits
                var detailsUrl = $"{baseUrl}/movie/{id}?api_key={apiKey}&language=en-US";
                var creditsUrl = $"{baseUrl}/movie/{id}/credits?api_key={apiKey}";
                var detailsJson = await client.GetStringAsync(detailsUrl);
                var creditsJson = await client.GetStringAsync(creditsUrl);

                using var ddoc = JsonDocument.Parse(detailsJson);
                using var cdoc = JsonDocument.Parse(creditsJson);

                var droot = ddoc.RootElement;
                var title = new Title();
                title.TitleName = droot.GetProperty("title").GetString() ?? titleName;
                title.TitleType = "Movie";
                if (droot.TryGetProperty("release_date", out var rel) && DateTime.TryParse(rel.GetString(), out var rd))
                    title.ReleaseDate = rd;
                else
                    title.ReleaseDate = DateTime.MinValue;
                title.AverageRating = droot.TryGetProperty("vote_average", out var va) ? Convert.ToDecimal(va.GetDouble()) : 0m;
                title.NumberOfRatings = droot.TryGetProperty("vote_count", out var vc) ? vc.GetInt32() : 0;
                title.EpisodeCount = null;

                // genres
                title.Genres = new List<string>();
                if (droot.TryGetProperty("genres", out var garr))
                {
                    foreach (var g in garr.EnumerateArray())
                    {
                        if (g.TryGetProperty("name", out var gname))
                            title.Genres.Add(gname.GetString() ?? string.Empty);
                    }
                }

                // actors (top 5)
                title.Actors = new List<string>();
                if (cdoc.RootElement.TryGetProperty("cast", out var castArr))
                {
                    // int added = 0;
                    foreach (var p in castArr.EnumerateArray())
                    {
                        if (p.TryGetProperty("name", out var pname))
                        {
                            title.Actors.Add(pname.GetString() ?? string.Empty);
                            //added++;
                            //if (added >= 5) break;
                        }
                    }
                }

                return title;
            }
        }

        // Try TV search
        var tvSearchUrl = $"{baseUrl}/search/tv?api_key={apiKey}&query={encoded}";
        var tvSearchJson = await client.GetStringAsync(tvSearchUrl);
        using (var doc = JsonDocument.Parse(tvSearchJson))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                var id = first.GetProperty("id").GetInt32();
                var detailsUrl = $"{baseUrl}/tv/{id}?api_key={apiKey}&language=en-US";
                var creditsUrl = $"{baseUrl}/tv/{id}/credits?api_key={apiKey}";
                var detailsJson = await client.GetStringAsync(detailsUrl);
                var creditsJson = await client.GetStringAsync(creditsUrl);

                using var ddoc = JsonDocument.Parse(detailsJson);
                using var cdoc = JsonDocument.Parse(creditsJson);

                var droot = ddoc.RootElement;
                var title = new Title();
                title.TitleName = droot.GetProperty("name").GetString() ?? titleName;
                title.TitleType = "TV";
                if (droot.TryGetProperty("first_air_date", out var rel) && DateTime.TryParse(rel.GetString(), out var rd))
                    title.ReleaseDate = rd;
                else
                    title.ReleaseDate = DateTime.MinValue;
                title.AverageRating = droot.TryGetProperty("vote_average", out var va) ? Convert.ToDecimal(va.GetDouble()) : 0m;
                title.NumberOfRatings = droot.TryGetProperty("vote_count", out var vc) ? vc.GetInt32() : 0;
                title.EpisodeCount = droot.TryGetProperty("number_of_episodes", out var ne) ? (int?)ne.GetInt32() : null;

                // genres
                title.Genres = new List<string>();
                if (droot.TryGetProperty("genres", out var garr))
                {
                    foreach (var g in garr.EnumerateArray())
                    {
                        if (g.TryGetProperty("name", out var gname))
                            title.Genres.Add(gname.GetString() ?? string.Empty);
                    }
                }

                // actors (all returned by TMDB credits)
                title.Actors = new List<string>();
                if (cdoc.RootElement.TryGetProperty("cast", out var castArr))
                {
                    //int added = 0;
                    foreach (var p in castArr.EnumerateArray())
                    {
                        if (p.TryGetProperty("name", out var pname))
                        {
                            title.Actors.Add(pname.GetString() ?? string.Empty);
                            // added++;
                            // if (added >= 5) break;
                        }
                    }
                }

                return title;
            }
        }

        return null;
    }

    // Fetch the TMDB collection ID for a title by name (returns null if none found)
    public async Task<int?> GetCollectionIdByName(string titleName)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("TMDB_API_KEY environment variable is not set.");

        if (string.IsNullOrWhiteSpace(titleName))
            return null;

        var encoded = Uri.EscapeDataString(titleName);

        // Try movie search first
        var movieSearchUrl = $"{baseUrl}/search/movie?api_key={apiKey}&query={encoded}";
        var movieSearchJson = await client.GetStringAsync(movieSearchUrl);
        using (var doc = JsonDocument.Parse(movieSearchJson))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                var id = first.GetProperty("id").GetInt32();
                var detailsUrl = $"{baseUrl}/movie/{id}?api_key={apiKey}&language=en-US";
                var detailsJson = await client.GetStringAsync(detailsUrl);
                using var ddoc = JsonDocument.Parse(detailsJson);
                var droot = ddoc.RootElement;
                if (droot.TryGetProperty("belongs_to_collection", out var coll) && coll.ValueKind != JsonValueKind.Null)
                {
                    if (coll.TryGetProperty("id", out var cid))
                        return cid.GetInt32();
                }
                return null;
            }
        }

        // Try TV search (collections typically don't apply to TV, but keep for completeness)
        var tvSearchUrl = $"{baseUrl}/search/tv?api_key={apiKey}&query={encoded}";
        var tvSearchJson = await client.GetStringAsync(tvSearchUrl);
        using (var doc2 = JsonDocument.Parse(tvSearchJson))
        {
            var root2 = doc2.RootElement;
            if (root2.TryGetProperty("results", out var results2) && results2.GetArrayLength() > 0)
            {
                var first = results2[0];
                var id = first.GetProperty("id").GetInt32();
                var detailsUrl = $"{baseUrl}/tv/{id}?api_key={apiKey}&language=en-US";
                var detailsJson = await client.GetStringAsync(detailsUrl);
                using var ddoc = JsonDocument.Parse(detailsJson);
                var droot = ddoc.RootElement;
                if (droot.TryGetProperty("belongs_to_collection", out var coll) && coll.ValueKind != JsonValueKind.Null)
                {
                    if (coll.TryGetProperty("id", out var cid))
                        return cid.GetInt32();
                }
                return null;
            }
        }

        return null;
    }

    // Discover movies by actor names (comma-separated cast ids)
    public async Task<string> DiscoverByActor(string[] actorNames)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("TMDB_API_KEY environment variable is not set.");

        if (actorNames == null || actorNames.Length == 0)
            return "{}";

        var ids = new List<int>();
        foreach (var name in actorNames)
        {
            var encoded = Uri.EscapeDataString(name);
            var searchUrl = $"{baseUrl}/search/person?api_key={apiKey}&query={encoded}";
            var searchJson = await client.GetStringAsync(searchUrl);
            using var doc = JsonDocument.Parse(searchJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("results", out var results))
                continue;

            foreach (var p in results.EnumerateArray())
            {
                if (p.TryGetProperty("id", out var idProp))
                {
                    ids.Add(idProp.GetInt32());
                    break;
                }
            }
        }

        if (ids.Count == 0)
            return "{}";

        var discoverUrl = $"{baseUrl}/discover/movie?api_key={apiKey}&with_cast={string.Join(',', ids)}";
        var discoverJson = await client.GetStringAsync(discoverUrl);
        return discoverJson;
    }

    // Discover movies genre names (maps names to ids then calls discover)
    public async Task<string> DiscoverByGenre(string[] genreNames)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("TMDB_API_KEY environment variable is not set.");

        if (genreNames == null || genreNames.Length == 0)
            return "{}";

        var genresUrl = $"{baseUrl}/genre/movie/list?api_key={apiKey}&language=en-US";
        var genresJson = await client.GetStringAsync(genresUrl);
        using var doc = JsonDocument.Parse(genresJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("genres", out var genres))
            return "{}";

        var ids = new List<int>();
        foreach (var target in genreNames)
        {
            foreach (var g in genres.EnumerateArray())
            {
                if (g.TryGetProperty("name", out var nameProp) &&
                    string.Equals(nameProp.GetString(), target, StringComparison.OrdinalIgnoreCase))
                {
                    ids.Add(g.GetProperty("id").GetInt32());
                    break;
                }
            }
        }

        if (ids.Count == 0)
            return "{}";

        var discoverUrl = $"{baseUrl}/discover/movie?api_key={apiKey}&with_genres={string.Join(',', ids)}";
        var discoverJson = await client.GetStringAsync(discoverUrl);
        return discoverJson;
    }

    public async Task<string> GetPopularMovies()
    {
        var url = $"{baseUrl}/movie/popular?api_key={apiKey}";
        var response = await client.GetStringAsync(url);
        return response;
    }

    public async Task<string> GetTopRatedMovies()
    {
        var url = $"{baseUrl}/movie/top_rated?api_key={apiKey}";
        var response = await client.GetStringAsync(url);
        return response;
    }

    public async Task<string> GetTrendingMovies()
    {
        var url = $"{baseUrl}/trending/movie/week?api_key={apiKey}";
        var response = await client.GetStringAsync(url);
        return response;
    }

    public async Task<string> DiscoverMoviesByGenre(int genreId)
    {
        var url = $"{baseUrl}/discover/movie?api_key={apiKey}&with_genres={genreId}&sort_by=popularity.desc";
        var response = await client.GetStringAsync(url);
        return response;
    }

    public async Task<string> DiscoverMoviesByActor(int actorId)
    {
        var url = $"{baseUrl}/discover/movie?api_key={apiKey}&with_cast={actorId}&sort_by=popularity.desc";
        var response = await client.GetStringAsync(url);
        return response;
    }
}