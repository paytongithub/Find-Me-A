using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Find_Me_A
{
    public class Search
    {
        private readonly string _connectionString;

        public Search(string connectionString)
        {
            _connectionString = connectionString;
        }

        private List<Title> ExecuteTitleQuery(string sql, params SqlParameter[] parameters)
        {
            var result = new List<Title>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var title = new Title
                            {
                                TitleName = reader["TitleName"].ToString(),
                                TitleType = reader["TitleType"].ToString(),
                                ReleaseDate = Convert.ToDateTime(reader["ReleaseDate"]),
                                AverageRating = Convert.ToDecimal(reader["AverageRating"]),
                                NumberOfRatings = Convert.ToInt32(reader["NumberOfRatings"]),
                                EpisodeCount = reader["EpisodeCount"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["EpisodeCount"]),
                                Genres = new List<string>(reader["Genres"].ToString().Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)),
                                Actors = new List<string>(reader["Actors"].ToString().Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                            };

                            result.Add(title);
                        }
                    }
                }
            }

            return result;
        }

        public List<Title> SearchByTitle(string titleName)
        {
            string sql = @"
                SELECT 
                    t.TitleName,
                    t.TitleType,
                    t.ReleaseDate,
                    t.Rating AS AverageRating,
                    t.NumberOfRatings,
                    ts.EpisodeCount,
                    (SELECT STRING_AGG(g2.GenreName, ', ')
                     FROM TitleGenres tg2
                     JOIN Genres g2 ON tg2.GenreID = g2.GenreID
                     WHERE tg2.TitleID = t.TitleID) AS Genres,
                    (SELECT STRING_AGG(a2.ActorName, ', ')
                     FROM TitleActors ta2
                     JOIN Actors a2 ON ta2.ActorID = a2.ActorID
                     WHERE ta2.TitleID = t.TitleID) AS Actors
                FROM Titles t
                LEFT JOIN TVShows ts ON t.TitleID = ts.TitleID
                WHERE t.TitleName LIKE '%' + @TitleName + '%';";

            return ExecuteTitleQuery(sql, new SqlParameter("@TitleName", titleName));
        }
// It's probably smarter just to have a single search method that takes in the genre/actor name in as a string[] of any size, instead of overload

/* 
        public List<Title> SearchByGenre(string genreName)
        {
            string sql = @"
                SELECT DISTINCT
                    t.TitleName,
                    t.TitleType,
                    t.ReleaseDate,
                    t.Rating AS AverageRating,
                    t.NumberOfRatings,
                    ts.EpisodeCount,
                    (SELECT STRING_AGG(g2.GenreName, ', ')
                     FROM TitleGenres tg2
                     JOIN Genres g2 ON tg2.GenreID = g2.GenreID
                     WHERE tg2.TitleID = t.TitleID) AS Genres,
                    (SELECT STRING_AGG(a2.ActorName, ', ')
                     FROM TitleActors ta2
                     JOIN Actors a2 ON ta2.ActorID = a2.ActorID
                     WHERE ta2.TitleID = t.TitleID) AS Actors
                FROM Titles t
                LEFT JOIN TVShows ts ON t.TitleID = ts.TitleID
                JOIN TitleGenres tg ON t.TitleID = tg.TitleID
                JOIN Genres g ON tg.GenreID = g.GenreID
                WHERE g.GenreName = @GenreName;";

            return ExecuteTitleQuery(sql, new SqlParameter("@GenreName", genreName));
        }

        public List<Title> SearchByActor(string actorName)
        {
            string sql = @"
                SELECT DISTINCT
                    t.TitleName,
                    t.TitleType,
                    t.ReleaseDate,
                    t.Rating AS AverageRating,
                    t.NumberOfRatings,
                    ts.EpisodeCount,
                    (SELECT STRING_AGG(g2.GenreName, ', ')
                     FROM TitleGenres tg2
                     JOIN Genres g2 ON tg2.GenreID = g2.GenreID
                     WHERE tg2.TitleID = t.TitleID) AS Genres,
                    (SELECT STRING_AGG(a2.ActorName, ', ')
                     FROM TitleActors ta2
                     JOIN Actors a2 ON ta2.ActorID = a2.ActorID
                     WHERE ta2.TitleID = t.TitleID) AS Actors
                FROM Titles t
                LEFT JOIN TVShows ts ON t.TitleID = ts.TitleID
                JOIN TitleActors ta ON t.TitleID = ta.TitleID
                JOIN Actors a ON ta.ActorID = a.ActorID
                WHERE a.ActorName = @ActorName;";

            return ExecuteTitleQuery(sql, new SqlParameter("@ActorName", actorName));
        }

        // overload method for multiple genres 
        public List<Title> SearchByGenre(string[] genreNames)
        {
            if (genreNames == null || genreNames.Length == 0)
                return new List<Title>();

            var paramNames = new List<string>();
            var parameters = new List<SqlParameter>();
            for (int i = 0; i < genreNames.Length; i++)
            {
                string param = "@Genre" + i;
                paramNames.Add(param);
                parameters.Add(new SqlParameter(param, genreNames[i]));
            }

            string sql = $@"
                SELECT DISTINCT
                    t.TitleName,
                    t.TitleType,
                    t.ReleaseDate,
                    t.Rating AS AverageRating,
                    t.NumberOfRatings,
                    ts.EpisodeCount,
                    (SELECT STRING_AGG(g2.GenreName, ', ')
                     FROM TitleGenres tg2
                     JOIN Genres g2 ON tg2.GenreID = g2.GenreID
                     WHERE tg2.TitleID = t.TitleID) AS Genres,
                    (SELECT STRING_AGG(a2.ActorName, ', ')
                     FROM TitleActors ta2
                     JOIN Actors a2 ON ta2.ActorID = a2.ActorID
                     WHERE ta2.TitleID = t.TitleID) AS Actors
                FROM Titles t
                LEFT JOIN TVShows ts ON t.TitleID = ts.TitleID
                JOIN TitleGenres tg ON t.TitleID = tg.TitleID
                JOIN Genres g ON tg.GenreID = g.GenreID
                WHERE g.GenreName IN ({string.Join(", ", paramNames)});";

            return ExecuteTitleQuery(sql, parameters.ToArray());
        }
        // overload method for multiple actors
        public List<Title> SearchByActor(string[] actorNames)
        {
            if (actorNames == null || actorNames.Length == 0)
                return new List<Title>();

            var paramNames = new List<string>();
            var parameters = new List<SqlParameter>();
            for (int i = 0; i < actorNames.Length; i++)
            {
                string param = "@Actor" + i;
                paramNames.Add(param);
                parameters.Add(new SqlParameter(param, actorNames[i]));
            }

            string sql = $@"
                SELECT DISTINCT
                    t.TitleName,
                    t.TitleType,
                    t.ReleaseDate,
                    t.Rating AS AverageRating,
                    t.NumberOfRatings,
                    ts.EpisodeCount,
                    (SELECT STRING_AGG(g2.GenreName, ', ')
                     FROM TitleGenres tg2
                     JOIN Genres g2 ON tg2.GenreID = g2.GenreID
                     WHERE tg2.TitleID = t.TitleID) AS Genres,
                    (SELECT STRING_AGG(a2.ActorName, ', ')
                     FROM TitleActors ta2
                     JOIN Actors a2 ON ta2.ActorID = a2.ActorID
                     WHERE ta2.TitleID = t.TitleID) AS Actors
                FROM Titles t
                LEFT JOIN TVShows ts ON t.TitleID = ts.TitleID
                JOIN TitleActors ta ON t.TitleID = ta.TitleID
                JOIN Actors a ON ta.ActorID = a.ActorID
                WHERE a.ActorName IN ({string.Join(", ", paramNames)});";

            return ExecuteTitleQuery(sql, parameters.ToArray());
        }
        */
        // switch for any of the methods
        //public object SearchQuery(string SearchBy, string Data)//
        public async Task<object> SearchQuery(string SearchBy, string[] Data)
        {
            switch (SearchBy)
            {
                // unplugging searches from the database, as API is in now
                /* 
                case "Title":
                    return SearchByTitle(Data);
                case "Genre":
                    return SearchByGenre(Data);
                case "Actor":
                    return SearchByActor(Data);
                */
                case "TMDB":
                    return await SearchFromTMDB((Data != null && Data.Length > 0) ? Data[0] : string.Empty);
                case "TMDBTitle":
                    return await SearchByTitleTMDB((Data != null && Data.Length > 0) ? Data[0] : string.Empty);
                case "TMDBActor":
                    return await SearchByActorTMDB(Data ?? Array.Empty<string>());
                case "TMDBGenre":
                    return await SearchByGenreTMDB(Data ?? Array.Empty<string>());
                case "All":
                    var dbResults = (Data != null && Data.Length > 0) ? SearchByTitle(Data[0]) : new List<Title>();
                    var apiResults = await SearchFromTMDB((Data != null && Data.Length > 0) ? Data[0] : string.Empty);
                    return new { dbResults, apiResults };
                default:
                    //Console.WriteLine("Invalid search criteria.");
                    //break;
                    return null;
            }
        }

        public async Task<string> SearchFromTMDB(string query)
        {
            var tmdb = new TMDB();
            return await tmdb.SearchMovies(query);
        }

        public async Task<string> SearchByTitleTMDB(string title)
        {
            return await SearchFromTMDB(title);
        }

        public async Task<string> SearchByActorTMDB(string[] actorNames)
        {
            var tmdb = new TMDB();
            return await tmdb.DiscoverByActor(actorNames);
        }

        public async Task<string> SearchByGenreTMDB(string[] genreNames)
        {
            var tmdb = new TMDB();
            return await tmdb.DiscoverByGenre(genreNames);
        }


    }
}
