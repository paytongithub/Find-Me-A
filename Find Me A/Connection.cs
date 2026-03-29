using System;
using System.Data;
using System.Data.SqlClient;

namespace Find_Me_A
{
    class Connection
    {
        static void Main(string[] args)
        {
            // below is test code for adding to the database, for layla's account
            string connectionString = "Server=147.126.2.58;Database=Find_Me_A;User ID=tpayton1;Password=p22817;";
            UserWatchList watchList = new UserWatchList(connectionString);

            // Add a movie to Layla's watch list
            watchList.AddToWatchList("Layla", "Test Film", new DateTime(2026, 3, 15), 7);

            // Update the rating
            watchList.UpdateRating("Layla", "Test Film", 8);

            // Remove a title
            watchList.RemoveFromWatchList("Layla", "Test Film");
        }
    }
}