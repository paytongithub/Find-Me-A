using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Find_Me_A.Recommendation;

namespace Find_Me_A
{
    public class Recommendation
    {
        public class WatchedDetail
        {
            public string TitleName { get; set; } = string.Empty;
            // Return actor and genre IDs instead of names
            public List<int> ActorIds { get; set; } = new List<int>();
            public List<int> GenreIds { get; set; } = new List<int>();
            public DateTime? ReleaseDate { get; set; }
            public int? UserRating { get; set; }
            public DateTime? WatchedDate { get; set; }
            // collection IDs (a title can belong to at most one collection in TMDB; stored as single-element list when present)
            public List<int> CollectionIDs { get; set; } = new List<int>();
        }

        public class ActorDetail
        {
            public int ActorID { get; set; }
            public List<int> TitleScores { get; set; } = new List<int>();
            public List<int> CollectionIDs { get; set; } = new List<int>();
            public List<DateTime?> WatchedDates { get; set; } = new List<DateTime?>();

            public List<DateTime?> ReleaseDates { get; set; } = new List<DateTime?>();

        }

        public class GenreDetail
        {
            public int GenreID { get; set; }
            public List<int> TitleScores { get; set; } = new List<int>();
            public List<int> CollectionIDs { get; set; } = new List<int>();
            public List<DateTime?> WatchedDates { get; set; } = new List<DateTime?>();

            public List<DateTime?> ReleaseDates { get; set; } = new List<DateTime?>();


        }

        public class ActorRecommendationProfile
        {
            // include a nullable release date in the tuple
            public List<Tuple<int, double, double>> ActorData { get; set; } = new List<Tuple<int, double, double>>(); //tuple is actorID, TitleRelevance, ActorScore
        }

        public class GenreRecommendationProfile
        {
            public List<Tuple<int, double, double>> GenreData { get; set; } = new List<Tuple<int, double, double>>(); //tuple is genreID, TitleRelevance, GenreScore

        }

        // Returns watched details for a user: for each title include up to 20 actors, up to 5 genres, user rating and watched date.
        public static List<WatchedDetail> GetUserWatchedDetails(string connectionString, string username)
        {
            var result = new List<WatchedDetail>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // aggregate IDs instead of names; use TOP to limit results in subqueries
                string sql = @"
                    SELECT
                        t.TitleName,
                        t.ReleaseDate,
                        uwt.WatchedDate,
                        uwt.UserRating,
                        (
                            SELECT STRING_AGG(CAST(a.ActorID AS NVARCHAR(20)), ', ')
                            FROM (
                                SELECT TOP (20) a2.ActorID
                                FROM TitleActors ta2
                                JOIN Actors a2 ON ta2.ActorID = a2.ActorID
                                WHERE ta2.TitleID = t.TitleID
                                ORDER BY a2.ActorID
                            ) a
                        ) AS ActorIds,
                        (
                            SELECT STRING_AGG(CAST(g.GenreID AS NVARCHAR(20)), ', ')
                            FROM (
                                SELECT TOP (5) g2.GenreID
                                FROM TitleGenres tg2
                                JOIN Genres g2 ON tg2.GenreID = g2.GenreID
                                WHERE tg2.TitleID = t.TitleID
                                ORDER BY g2.GenreID
                            ) g
                        ) AS GenreIds
                    FROM UserWatchedTitles uwt
                    JOIN Users u ON uwt.UserID = u.UserID
                    JOIN Titles t ON uwt.TitleID = t.TitleID
                    WHERE u.Username = @Username
                    ORDER BY uwt.WatchedDate DESC;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var actorsRaw = reader["ActorIds"] == DBNull.Value ? string.Empty : reader["ActorIds"].ToString() ?? string.Empty;
                            var genresRaw = reader["GenreIds"] == DBNull.Value ? string.Empty : reader["GenreIds"].ToString() ?? string.Empty;

                            var detail = new WatchedDetail
                            {
                                TitleName = reader["TitleName"]?.ToString() ?? string.Empty,
                                ReleaseDate = reader["ReleaseDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ReleaseDate"]),
                                ActorIds = string.IsNullOrWhiteSpace(actorsRaw) ? new List<int>() : ParseIdList(actorsRaw),
                                GenreIds = string.IsNullOrWhiteSpace(genresRaw) ? new List<int>() : ParseIdList(genresRaw),
                                UserRating = reader["UserRating"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["UserRating"]),
                                WatchedDate = reader["WatchedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["WatchedDate"]),
                                CollectionIDs = new List<int>()
                            };

                            // fetch collection id from TMDB on demand (do not store in DB)
                            try
                            {
                                var tmdb = new TMDB();
                                var collId = tmdb.GetCollectionIdByName(detail.TitleName).GetAwaiter().GetResult();
                                if (collId.HasValue)
                                {
                                    detail.CollectionIDs.Add(collId.Value);
                                }
                            }
                            catch
                            {
                                // ignore TMDB failures; leave CollectionIDs empty
                            }

                            // Ensure limits (defensive)
                            if (detail.ActorIds.Count > 20) detail.ActorIds = detail.ActorIds.GetRange(0, 20);
                            if (detail.GenreIds.Count > 5) detail.GenreIds = detail.GenreIds.GetRange(0, 5);

                            result.Add(detail);
                        }
                    }
                }
            }

            return result;
        }

        private static List<int> ParseIdList(string csv)
        {
            var outList = new List<int>();
            if (string.IsNullOrWhiteSpace(csv)) return outList;

            var parts = csv.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p, out var id)) outList.Add(id);
            }
            return outList;
        }

        // returns a list of each actor's id and the scores for titles with the actor rated by the user
        public static List<ActorDetail> GetActorDetails(List<WatchedDetail> watchedDetails)
        {
            var result = new List<ActorDetail>();

            foreach (var watchedDetail in watchedDetails)
            {
                foreach (var actorId in watchedDetail.ActorIds)
                {
                    var actorDetail = result.FirstOrDefault(a => a.ActorID == actorId);
                    if (actorDetail == null)
                    {
                        actorDetail = new ActorDetail { ActorID = actorId, TitleScores = new List<int>() };
                        result.Add(actorDetail);
                    }

                    if (watchedDetail.UserRating.HasValue)
                    {
                        actorDetail.TitleScores.Add(watchedDetail.UserRating.Value);
                    }
                    // add collection ids
                    foreach (var cid in watchedDetail.CollectionIDs)
                    {
                        if (!actorDetail.CollectionIDs.Contains(cid))
                            actorDetail.CollectionIDs.Add(cid);
                    }
                    // add release date
                    if (watchedDetail.ReleaseDate.HasValue)
                    {
                        actorDetail.ReleaseDates.Add(watchedDetail.ReleaseDate.Value);
                    }
                    if (watchedDetail.WatchedDate.HasValue)
                    {
                        actorDetail.WatchedDates.Add(watchedDetail.WatchedDate.Value);
                    }
                }
            }
            return result;
        }

        // returns a list of each genre's id and the scores for titles with the genre rated by the user
        public static List<GenreDetail> GetGenreDetails(List<WatchedDetail> watchedDetails)
        {
            var result = new List<GenreDetail>();

            foreach (var watchedDetail in watchedDetails)
            {
                foreach (var genreId in watchedDetail.GenreIds)
                {
                    var genreDetail = result.FirstOrDefault(a => a.GenreID == genreId);
                    if (genreDetail == null)
                    {
                        genreDetail = new GenreDetail { GenreID = genreId, TitleScores = new List<int>() };
                        result.Add(genreDetail);
                    }

                    if (watchedDetail.UserRating.HasValue)
                    {
                        genreDetail.TitleScores.Add(watchedDetail.UserRating.Value);
                    }
                    // add collection ids
                    foreach (var cid in watchedDetail.CollectionIDs)
                    {
                        if (!genreDetail.CollectionIDs.Contains(cid))
                            genreDetail.CollectionIDs.Add(cid);
                    }
                    // add release date
                    if (watchedDetail.ReleaseDate.HasValue)
                    {
                        genreDetail.ReleaseDates.Add(watchedDetail.ReleaseDate.Value);
                    }
                    if (watchedDetail.WatchedDate.HasValue)
                    {
                        genreDetail.WatchedDates.Add(watchedDetail.WatchedDate.Value);
                    }
                }
            }
            return result;
        }

        /* Logic for recommendation algorithm 
         * We want to generate a score for each actor and genre, based on the following logic
         * The rating the user gave the title with the actor/genre should be the basis for the score
         * The position which the actor/genre is listed is factored into score (first is the lead, so most important)
         * The amount of times an actor/genre appears in the user's watched history is recorded, lets say actorCount / genreCount
         * If a title is part of a collection that has already been rated by the user, the effect of each subsequent title is 1/n+1 (first is full, second half, third is third) 
         *    From this, lets do Iron Man as an example. Since the user is watching the movies in release order probably, the lastest recorded is first, 
         *    so full points, this is Iron Man 3. Let's say IM3 is rated 5, IM2 is a 6, and IM1 is a 9
         * Iron Man's Collection score is 5, 6, 9, but since they're reduced past the most recent, its (5*1/1 + 6*1/2 + 9*1/3) / (1 + 1/2 + 1/3) = 6
         * 
         * TitleRelevance algorithm:
         *    The above position algorithm will count toward the total number of titles that the user has watched with the actor. So if they've seen 8 films with RDJ, but 5 were MCU films,
         *    so the total number of films seen with RDJ (actorCount) becomes 3 + 1 (first MCU) + 1/2 + 1/3 + 1/4 + 1/5 = 5.283
         *    However, we should be taking into account whether the actor in question is the lead of their films. so we use an individual TitleRelevance score for each title.
         *    TitleRelevance will equal (total actors - position + 1) / total actors, so in the Iron Man movies, it's always 1, but for others, let's say all have 20 actors for
         *    simplicity, for the 5 MCU films, the 3 most recent are Iron Man, and all he's 1, then hes 3, then 2, with the 2 being the least recently watched. 
         *    For the other 3 non MCU: 1, 5, and 14th. 
         *    So the TitleRelevance equation becomes ((20-1+1)/20 + (20-5+1)/20 + (20-14+1)/20) + ((20-1+1)/20/1 + (20-1+1)/20/2 + (20-1+1)/20/3 + (20-3+1)/20/4 + (20-1+1)/20/5)) = 4.408
         *    TitleRelevance = 4.408, despite him being in 8 films, because he's not always the lead and not always in unique films
         *    
         * ActorScore algorithm:
         *    This algorithm is for the rating of the actor in each of the films
         *    We use the same logic for relevance to a film, as well as collection bonus, but now we factor in the user rating of the film
         *    Using the ratings from the Iron Man films from above, (5*(20-1+1/20)/1 + 6*(20-1+1/20)/2 + 9*(20-1+1/20)/3) / (1 + 1/2 + 1/3) = 6
         *    For the other 2 MCUs, and the 3 non MCUs, let's say the ratings were 7 7 for MCUs, and others 10 4 5, with his position still being 3 2 for MCUS, and others 1 5 14
         *    This algorithm is essentially TitleRelevance, but with score multiplied to each number
         *    ActorScore = (10*(20-1+1)/20 + 4*(20-5+1)/20 + 5*(20-14+1)/20) + (5*(20-1+1)/20/1 + 6*(20-1+1)/20/2 + 9*(20-1+1)/20/3 + 7*(20-3+1)/20/4 + 7*(20-1+1)/20/5)) = 28.925
         *    Now, we divide this value by TitleRelevance to get the final ActorScore, so 28.925 / 4.408 = 6.56
         */
        // Build genre recommendation profile from watched details using the same algorithm as actors.
        // Returns tuples of (genreID, TitleRelevance, GenreScore).
        public static GenreRecommendationProfile GetGenreRecommendationProfileFromWatched(List<WatchedDetail> watchedDetails)
        {
            var profile = new GenreRecommendationProfile();

            var map = new Dictionary<int, List<(int? rating, DateTime? watchedDate, DateTime? releaseDate, int? collectionId, int position, int totalGenres)>>();

            foreach (var wd in watchedDetails)
            {
                for (int i = 0; i < wd.GenreIds.Count; i++)
                {
                    int genreId = wd.GenreIds[i];
                    int position = i + 1; // 1-based
                    int total = Math.Max(1, wd.GenreIds.Count);
                    int? coll = (wd.CollectionIDs != null && wd.CollectionIDs.Count > 0) ? (int?)wd.CollectionIDs[0] : null;
                    int? rating = wd.UserRating;
                    if (!map.TryGetValue(genreId, out var list))
                    {
                        list = new List<(int?, DateTime?, DateTime?, int?, int, int)>();
                        map[genreId] = list;
                    }
                    list.Add((rating, wd.WatchedDate, wd.ReleaseDate, coll, position, total));
                }
            }

            foreach (var kv in map)
            {
                int genreId = kv.Key;
                var entries = kv.Value;

                var groups = entries.GroupBy(e => e.collectionId);

                double titleRelevance = 0.0;
                double weightedScoreSum = 0.0;

                foreach (var g in groups)
                {
                    var groupEntries = g.OrderByDescending(e => e.watchedDate ?? DateTime.MinValue).ToList();

                    if (g.Key.HasValue)
                    {
                        for (int idx = 0; idx < groupEntries.Count; idx++)
                        {
                            var e = groupEntries[idx];
                            double weight = 1.0 / (idx + 1);
                            double posFactor = (double)(e.totalGenres - e.position + 1) / e.totalGenres;
                            double rel = posFactor * weight;
                            titleRelevance += rel;
                            if (e.rating.HasValue)
                                weightedScoreSum += rel * e.rating.Value;
                        }
                    }
                    else
                    {
                        foreach (var e in groupEntries)
                        {
                            double weight = 1.0;
                            double posFactor = (double)(e.totalGenres - e.position + 1) / e.totalGenres;
                            double rel = posFactor * weight;
                            titleRelevance += rel;
                            if (e.rating.HasValue)
                                weightedScoreSum += rel * e.rating.Value;
                        }
                    }
                }

                double genreScore = titleRelevance > 0 ? (weightedScoreSum / titleRelevance) : 0.0;

                profile.GenreData.Add(Tuple.Create(genreId, titleRelevance, genreScore));
            }

            return profile;
        }

        // Build actor recommendation profile from watched details using the algorithm described.
        // Returns tuples of (actorID, TitleRelevance, ActorScore).
        public static ActorRecommendationProfile GetActorRecommendationProfileFromWatched(List<WatchedDetail> watchedDetails)
        {
            var profile = new ActorRecommendationProfile();

            // Build per-actor list of entries
            var map = new Dictionary<int, List<(int? rating, DateTime? watchedDate, DateTime? releaseDate, int? collectionId, int position, int totalActors)>>();

            foreach (var wd in watchedDetails)
            {
                for (int i = 0; i < wd.ActorIds.Count; i++)
                {
                    int actorId = wd.ActorIds[i];
                    int position = i + 1; // 1-based
                    int total = Math.Max(1, wd.ActorIds.Count);
                    int? coll = (wd.CollectionIDs != null && wd.CollectionIDs.Count > 0) ? (int?)wd.CollectionIDs[0] : null;
                    int? rating = wd.UserRating;
                    if (!map.TryGetValue(actorId, out var list))
                    {
                        list = new List<(int?, DateTime?, DateTime?, int?, int, int)>();
                        map[actorId] = list;
                    }
                    list.Add((rating, wd.WatchedDate, wd.ReleaseDate, coll, position, total));
                }
            }

            foreach (var kv in map)
            {
                int actorId = kv.Key;
                var entries = kv.Value;

                // Group entries by collectionId (null treated separately)
                var groups = entries.GroupBy(e => e.collectionId);

                double titleRelevance = 0.0;
                double weightedScoreSum = 0.0;

                foreach (var g in groups)
                {
                    var groupEntries = g.OrderByDescending(e => e.watchedDate ?? DateTime.MinValue).ToList();

                    if (g.Key.HasValue)
                    {
                        // collection entries: apply diminishing weights 1/(idx+1)
                        for (int idx = 0; idx < groupEntries.Count; idx++)
                        {
                            var e = groupEntries[idx];
                            double weight = 1.0 / (idx + 1);
                            double posFactor = (double)(e.totalActors - e.position + 1) / e.totalActors;
                            double rel = posFactor * weight;
                            titleRelevance += rel;
                            if (e.rating.HasValue)
                                weightedScoreSum += rel * e.rating.Value;
                        }
                    }
                    else
                    {
                        // non-collection entries: full weight 1
                        foreach (var e in groupEntries)
                        {
                            double weight = 1.0;
                            double posFactor = (double)(e.totalActors - e.position + 1) / e.totalActors;
                            double rel = posFactor * weight;
                            titleRelevance += rel;
                            if (e.rating.HasValue)
                                weightedScoreSum += rel * e.rating.Value;
                        }
                    }
                }

                double actorScore = titleRelevance > 0 ? (weightedScoreSum / titleRelevance) : 0.0;
                actorScore = actorScore * titleRelevance;
                profile.ActorData.Add(Tuple.Create(actorId, titleRelevance, actorScore));
            }

            return profile;
        }
    }




}
