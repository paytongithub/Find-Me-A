using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Find_Me_A
{
    public class WatchList
    {
        private readonly string _connectionString;

        public WatchList(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Add to watch list, no rating attached
        public void AddToWatchList(string Username, string TitleName, DateTime WatchedDate)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Get UserID
                int UserID = GetUserId(conn, Username);

                // Get TitleID
                int TitleID = GetTitleId(conn, TitleName);

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

                // Get UserID
                int UserID = GetUserId(conn, Username);

                // Get TitleID
                int TitleID = GetTitleId(conn, TitleName);

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
    }
}