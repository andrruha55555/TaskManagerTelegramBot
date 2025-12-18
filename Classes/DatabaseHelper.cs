using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TaskManagerTelegramBot_Pikulev.Classes
{
    public static class DatabaseHelper
    {
        private static string _connectionString = "server=localhost;Port=3306;database=taskmanagerbot;user=root;password=;";
        public static void InitializeDatabase()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
            }
        }
        public static void SaveUser(long idUser, string username)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var query = @"
                INSERT INTO Users (IdUser, Username) 
                VALUES (@IdUser, @Username)
                ON DUPLICATE KEY UPDATE Username = @Username";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@IdUser", idUser);
                    cmd.Parameters.AddWithValue("@Username", username ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void SaveEvent(long idUser, DateTime eventTime, string message)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var query = "INSERT INTO Events (IdUser, EventTime, Message) VALUES (@IdUser, @EventTime, @Message)";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@IdUser", idUser);
                    cmd.Parameters.AddWithValue("@EventTime", eventTime);
                    cmd.Parameters.AddWithValue("@Message", message);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void DeleteEvent(long idUser, string message)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var query = "DELETE FROM Events WHERE IdUser = @IdUser AND Message = @Message";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@IdUser", idUser);
                    cmd.Parameters.AddWithValue("@Message", message);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void DeleteAllUserEvents(long idUser)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var query = "DELETE FROM Events WHERE IdUser = @IdUser";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@IdUser", idUser);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
