using System;
using System.Net.Http;
using System.Threading.Tasks;


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
}