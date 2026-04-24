using Microsoft.Data.SqlClient;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Find_Me_A
{
    public class Login
    {
        private readonly string _connectionString;

        public Login(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Registers a new user account
        /// </summary>
        public (bool Success, string Message) RegisterUser(string username, string password)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Enter a Username");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Enter a Password");

            // Check if user already exists
            if (UserExists(username))
                return (false, "Username already exists.");

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO Users (Username, Password)
                        VALUES (@Username, @Password)";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", password);

                        int result = cmd.ExecuteNonQuery();
                        if (result > 0)
                            return (true, "Registration successful! You can now log in.");
                        else
                            return (false, "Registration failed. Please try again.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Registration error: {ex.Message}");
            }
        }

        /// <summary>
        /// Authenticates a user with username and password
        /// </summary>
        public (bool Success, string Message, int? UserId) AuthenticateUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "Username and password are required.", null);

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = "SELECT UserID, Password FROM Users WHERE Username = @Username";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = (int)reader["UserID"];
                                string storedPassword = reader["Password"]?.ToString() ?? "";

                                // Compare password directly
                                if (password == storedPassword)
                                {
                                    return (true, "Login successful!", userId);
                                }
                                else
                                {
                                    return (false, "Invalid username or password.", null);
                                }
                            }
                            else
                            {
                                return (false, "Invalid username or password.", null);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Login error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Checks if a username already exists
        /// </summary>
        public bool UserExists(string username)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets user ID by username
        /// </summary>
        public int? GetUserId(string username)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT UserID FROM Users WHERE Username = @Username";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return (int)reader["UserID"];
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Changes user password
        /// </summary>
        public (bool Success, string Message) ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return (false, "Please enter a password");

            // First authenticate with old password
            var (authSuccess, _, _) = AuthenticateUser(username, oldPassword);
            if (!authSuccess)
                return (false, "Current password is incorrect.");

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = "UPDATE Users SET Password = @Password WHERE Username = @Username";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Password", newPassword);
                        cmd.Parameters.AddWithValue("@Username", username);

                        int result = cmd.ExecuteNonQuery();
                        if (result > 0)
                            return (true, "Password changed successfully.");
                        else
                            return (false, "Failed to change password.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error changing password: {ex.Message}");
            }
        }
    }
}
