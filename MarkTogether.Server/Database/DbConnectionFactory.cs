using System;
using System.Configuration;
using System.Data;
using Npgsql;

namespace MarkTogether.Server.Database
{
    /// <summary>
    /// Factory tạo kết nối tới PostgreSQL.
    /// Production ưu tiên biến môi trường MARKTOGETHER_DB_CONNECTION để tránh hardcode secret.
    /// App.config vẫn được giữ làm fallback nhằm bảo toàn backward compatibility cho môi trường dev hiện tại.
    /// </summary>
    public static class DbConnectionFactory
    {
        private const string DbConnectionEnvName = "MARKTOGETHER_DB_CONNECTION";

        private static readonly string _connectionString =
            Environment.GetEnvironmentVariable(DbConnectionEnvName)
            ?? ConfigurationManager.ConnectionStrings["MarkTogetherDb"]?.ConnectionString
            ?? "Host=localhost;Port=5432;Database=myapp_db;Username=postgres;Password=123";

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
