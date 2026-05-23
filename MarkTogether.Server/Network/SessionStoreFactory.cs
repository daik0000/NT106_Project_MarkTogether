using System;

namespace MarkTogether.Server.Network
{
    public static class SessionStoreFactory
    {
        public static ISessionStore Create()
        {
            string redisConn = Environment.GetEnvironmentVariable("MARKTOGETHER_REDIS_CONNECTION");
            int ttlSeconds = ParseTtl(Environment.GetEnvironmentVariable("MARKTOGETHER_SESSION_TTL_SECONDS"));

            if (string.IsNullOrWhiteSpace(redisConn))
            {
                Console.WriteLine("[SessionStore] Redis chưa cấu hình. Dùng in-memory session store.");
                return new InMemorySessionStore();
            }

            try
            {
                var store = new RedisSessionStore(redisConn, ttlSeconds);
                Console.WriteLine("[SessionStore] Dùng Redis session store.");
                return store;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SessionStore] Redis init lỗi, fallback in-memory: " + ex.Message);
                return new InMemorySessionStore();
            }
        }

        private static int ParseTtl(string raw)
        {
            if (int.TryParse(raw, out int ttl) && ttl > 0)
            {
                return ttl;
            }

            return 86400;
        }
    }
}
