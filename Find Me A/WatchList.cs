using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Find_Me_A
{
    public class WatchList
    {
        private readonly string _connectionString;

        public class WatchedItem
        {
            public Title Title { get; set; } = null!;
            public DateTime WatchedDate { get; set; }
            public int? UserRating { get; set; }
        }

        public WatchList(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Returns the list of watched items for the given username (most recent first)
        public List<WatchedItem> GetWatchedList(string username)
        {
            var result = new List<WatchedItem>();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT 
                        t.TitleName,
                        t.TitleType,
                        t.ReleaseDate,
                        t.Rating AS AverageRating,
                        t.NumberOfRatings,
                        ts.EpisodeCount,
                        (SELECT STRING_AGG(g2.GenreName, ', ') FROM TitleGenres tg2 JOIN Genres g2 ON tg2.GenreID = g2.GenreID WHERE tg2.TitleID = t.TitleID) AS Genres,
                        (SELECT STRING_AGG(a2.ActorName, ', ') FROM TitleActors ta2 JOIN Actors a2 ON ta2.ActorID = a2.ActorID WHERE ta2.TitleID = t.TitleID) AS Actors,
                        uwt.WatchedDate,
                        uwt.UserRating
                    FROM UserWatchedTitles uwt
                    JOIN Users u ON uwt.UserID = u.UserID
                    JOIN Titles t ON uwt.TitleID = t.TitleID
                    LEFT JOIN TVShows ts ON t.TitleID = ts.TitleID
                    WHERE u.Username = @Username
                    ORDER BY uwt.WatchedDate DESC;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var title = new Title
                            {
                                TitleName = reader["TitleName"].ToString() ?? string.Empty,
                                TitleType = reader["TitleType"].ToString() ?? string.Empty,
                                ReleaseDate = Convert.ToDateTime(reader["ReleaseDate"]),
                                AverageRating = Convert.ToDecimal(reader["AverageRating"]),
                                NumberOfRatings = Convert.ToInt32(reader["NumberOfRatings"]),
                                EpisodeCount = reader["EpisodeCount"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["EpisodeCount"]),
                                Genres = new System.Collections.Generic.List<string>((reader["Genres"] == DBNull.Value ? string.Empty : reader["Genres"].ToString()).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)),
                                Actors = new System.Collections.Generic.List<string>((reader["Actors"] == DBNull.Value ? string.Empty : reader["Actors"].ToString()).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                            };

                            var watchedDate = reader["WatchedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["WatchedDate"]);
                            var userRating = reader["UserRating"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["UserRating"]);

                            result.Add(new WatchedItem
                            {
                                Title = title,
                                WatchedDate = watchedDate,
                                UserRating = userRating
                            });
                        }
                    }
                }
            }

            return result;
        }

        // Backwards-compatible wrappers for MyWatchList methods
        public void UpdateUserRating(string username, string titleName, int newRating)
        {
            UpdateRating(username, titleName, newRating);
        }

        public void RemoveWatchedItem(string username, string titleName)
        {
            RemoveFromWatchList(username, titleName);
        }

        // Add to watch list, no rating attached
        public void AddToWatchList(string Username, string TitleName, DateTime WatchedDate)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Ensure title exists in Titles table (insert from TMDB if missing)
                int TitleID = EnsureTitleExists(conn, TitleName);

                // Get UserID
                int UserID = GetUserId(conn, Username);

                // Insert into UserWatchedTitles
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO UserWatchedTitles (UserID, TitleID, WatchedDate)
                    VALUES (@UserID, @TitleID, @WatchedDate)", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", UserID);
                    cmd.Parameters.AddWithValue("@TitleID", TitleID);
                    cmd.Parameters.AddWithValue("@WatchedDate", WatchedDate);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // same as above but with rating
        public void AddToWatchList(string Username, string TitleName, DateTime WatchedDate, int UserRating)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Ensure title exists in Titles table (insert from TMDB if missing)
                int TitleID = EnsureTitleExists(conn, TitleName);

                // Get UserID
                int UserID = GetUserId(conn, Username);

                // Insert into UserWatchedTitles
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO UserWatchedTitles (UserID, TitleID, WatchedDate, UserRating)
                    VALUES (@UserID, @TitleID, @WatchedDate, @UserRating)", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", UserID);
                    cmd.Parameters.AddWithValue("@TitleID", TitleID);
                    cmd.Parameters.AddWithValue("@WatchedDate", WatchedDate);
                    cmd.Parameters.AddWithValue("@UserRating", UserRating);

                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void RemoveFromWatchList(string Username, string TitleName)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                int UserID = GetUserId(conn, Username);
                int TitleID = GetTitleId(conn, TitleName);

                using (SqlCommand cmd = new SqlCommand(@"
                    DELETE FROM UserWatchedTitles
                    WHERE UserID = @UserID AND TitleID = @TitleID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", UserID);
                    cmd.Parameters.AddWithValue("@TitleID", TitleID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateRating(string Username, string TitleName, int NewRating)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                int UserID = GetUserId(conn, Username);
                int TitleID = GetTitleId(conn, TitleName);

                using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE UserWatchedTitles
                    SET UserRating = @UserRating
                    WHERE UserID = @UserID AND TitleID = @TitleID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserRating", NewRating);
                    cmd.Parameters.AddWithValue("@UserID", UserID);
                    cmd.Parameters.AddWithValue("@TitleID", TitleID);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private int GetUserId(SqlConnection conn, string Username)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT UserID FROM Users WHERE Username = @Username", conn))
            {
                cmd.Parameters.AddWithValue("@Username", Username);
                object result = cmd.ExecuteScalar();
                if (result == null) throw new Exception($"User '{Username}' not found.");
                return Convert.ToInt32(result);
            }
        }
        private int GetTitleId(SqlConnection conn, string TitleName)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT TitleID FROM Titles WHERE TitleName = @TitleName", conn))
            {
                cmd.Parameters.AddWithValue("@TitleName", TitleName);
                object result = cmd.ExecuteScalar();
                if (result == null) throw new Exception($"Title '{TitleName}' not found.");
                return Convert.ToInt32(result);
            }
        }

        // Try get title id without throwing
        private int? TryGetTitleId(SqlConnection conn, string TitleName)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT TitleID FROM Titles WHERE TitleName = @TitleName", conn))
            {
                cmd.Parameters.AddWithValue("@TitleName", TitleName);
                object result = cmd.ExecuteScalar();
                if (result == null) return null;
                return Convert.ToInt32(result);
            }
        }

        // Ensure title exists in Titles table. If missing, fetch from TMDB and insert all related rows.
        private int EnsureTitleExists(SqlConnection conn, string titleName)
        {
            // check existing
            var existing = TryGetTitleId(conn, titleName);
            if (existing.HasValue) return existing.Value;

            // fetch from TMDB
            var tmdb = new TMDB();
            Title? tmdbTitle = null;
            try
            {
                tmdbTitle = tmdb.GetTitleByName(titleName).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to fetch title from TMDB: " + ex.Message);
            }

            if (tmdbTitle == null)
                throw new Exception($"Title '{titleName}' not found in database or TMDB.");

            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // Insert into Titles
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO Titles (TitleName, ReleaseDate, Rating, NumberOfRatings, TitleType)
                        VALUES (@TitleName, @ReleaseDate, @Rating, @NumberOfRatings, @TitleType);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@TitleName", tmdbTitle.TitleName);
                        if (tmdbTitle.ReleaseDate == DateTime.MinValue)
                            cmd.Parameters.AddWithValue("@ReleaseDate", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@ReleaseDate", tmdbTitle.ReleaseDate);
                        cmd.Parameters.AddWithValue("@Rating", tmdbTitle.AverageRating);
                        cmd.Parameters.AddWithValue("@NumberOfRatings", tmdbTitle.NumberOfRatings);
                        cmd.Parameters.AddWithValue("@TitleType", tmdbTitle.TitleType ?? "Movie");

                        var newIdObj = cmd.ExecuteScalar();
                        int newTitleId = Convert.ToInt32(newIdObj);

                        // Insert into Movies or TVShows
                        if (string.Equals(tmdbTitle.TitleType, "TV", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var c = new SqlCommand("INSERT INTO TVShows (TitleID, EpisodeCount) VALUES (@TitleID, @EpisodeCount)", conn, tran))
                            {
                                c.Parameters.AddWithValue("@TitleID", newTitleId);
                                if (tmdbTitle.EpisodeCount.HasValue)
                                    c.Parameters.AddWithValue("@EpisodeCount", tmdbTitle.EpisodeCount.Value);
                                else
                                    c.Parameters.AddWithValue("@EpisodeCount", DBNull.Value);
                                c.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var c = new SqlCommand("INSERT INTO Movies (TitleID) VALUES (@TitleID)", conn, tran))
                            {
                                c.Parameters.AddWithValue("@TitleID", newTitleId);
                                c.ExecuteNonQuery();
                            }
                        }

                        // Genres
                        foreach (var g in tmdbTitle.Genres ?? new System.Collections.Generic.List<string>())
                        {
                            int genreId;
                            using (var sc = new SqlCommand("SELECT GenreID FROM Genres WHERE GenreName = @Name", conn, tran))
                            {
                                sc.Parameters.AddWithValue("@Name", g);
                                var res = sc.ExecuteScalar();
                                if (res == null)
                                {
                                    using (var ic = new SqlCommand("INSERT INTO Genres (GenreName) VALUES (@Name); SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
                                    {
                                        ic.Parameters.AddWithValue("@Name", g);
                                        genreId = Convert.ToInt32(ic.ExecuteScalar());
                                    }
                                }
                                else
                                {
                                    genreId = Convert.ToInt32(res);
                                }
                            }

                            using (var tg = new SqlCommand("IF NOT EXISTS(SELECT 1 FROM TitleGenres WHERE TitleID=@TitleID AND GenreID=@GenreID) INSERT INTO TitleGenres(TitleID,GenreID) VALUES(@TitleID,@GenreID)", conn, tran))
                            {
                                tg.Parameters.AddWithValue("@TitleID", newTitleId);
                                tg.Parameters.AddWithValue("@GenreID", genreId);
                                tg.ExecuteNonQuery();
                            }
                        }

                        // Actors
                        foreach (var a in tmdbTitle.Actors ?? new System.Collections.Generic.List<string>())
                        {
                            int actorId;
                            using (var sa = new SqlCommand("SELECT ActorID FROM Actors WHERE ActorName = @Name", conn, tran))
                            {
                                sa.Parameters.AddWithValue("@Name", a);
                                var res = sa.ExecuteScalar();
                                if (res == null)
                                {
                                    using (var ia = new SqlCommand("INSERT INTO Actors (ActorName) VALUES (@Name); SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tran))
                                    {
                                        ia.Parameters.AddWithValue("@Name", a);
                                        actorId = Convert.ToInt32(ia.ExecuteScalar());
                                    }
                                }
                                else
                                {
                                    actorId = Convert.ToInt32(res);
                                }
                            }

                            using (var ta = new SqlCommand("IF NOT EXISTS(SELECT 1 FROM TitleActors WHERE TitleID=@TitleID AND ActorID=@ActorID) INSERT INTO TitleActors(TitleID,ActorID) VALUES(@TitleID,@ActorID)", conn, tran))
                            {
                                ta.Parameters.AddWithValue("@TitleID", newTitleId);
                                ta.Parameters.AddWithValue("@ActorID", actorId);
                                ta.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        return newTitleId;
                    }
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }
    }
}