using System.Configuration;
using System.Data;
using Npgsql;

namespace MarkTogether.Server.Database
{
    /// <summary>
    /// Factory tạo kết nối tới PostgreSQL.
    /// Connection string được đọc từ App.config.
    /// </summary>
    public static class DbConnectionFactory
    {
        private static readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["MarkTogetherDb"]?.ConnectionString
            ?? "Host=localhost;Port=5432;Database=marktogether;Username=postgres;Password=postgres";

        /// <summary>
        /// Tạo và trả về một NpgsqlConnection mới (chưa Open).
        /// Người gọi có trách nhiệm gọi Open() và Dispose().
        /// </summary>
        public static IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
