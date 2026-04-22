using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Find_Me_A
{
    // not sure where to put this atm, but this is the overhead needed to access the searches
    public class Title
    {
        public string TitleName { get; set; }
        public string TitleType { get; set; } // Movie / TV
        public DateTime ReleaseDate { get; set; }
        public decimal AverageRating { get; set; }
        public int NumberOfRatings { get; set; }
        public int? EpisodeCount { get; set; } // null for movies
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Actors { get; set; } = new List<string>();
        public string? PosterPath { get; set; }
        public string? Overview { get; set; }
        public string? ImdbId { get; set; }
    }
    class Connection
    {
        /*
        static void Main(string[] args)
        {
            // below is test code for adding to the database, for layla's account
            string connectionString = "Server=147.126.2.58;Database=Find_Me_A;User ID=tpayton1;Password=p22817;TrustServerCertificate=True;";
            WatchList watchList = new WatchList(connectionString);

            // Add a movie to Layla's watch list
            watchList.AddToWatchList("Layla", "Test Film", new DateTime(2026, 3, 15), 7);

            // Update the rating
            watchList.UpdateRating("Layla", "Test Film", 8);

            // Remove a title
            watchList.RemoveFromWatchList("Layla", "Test Film");
        }
        
        static void Main2(string[] args)
        {
            Console.WriteLine("Server running...");
        }
    }
        */
        }
}