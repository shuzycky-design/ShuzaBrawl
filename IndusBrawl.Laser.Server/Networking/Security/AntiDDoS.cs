namespace IndusBrawl.Laser.Server.Networking.Security
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text.Json;
    using System.Threading;

    public static class AntiDDoS
    {
        private class Bucket
        {
            public long Tokens;
            public long LastRefillMs;
            public int Strikes;
            public Bucket(long tokens, long last) { Tokens = tokens; LastRefillMs = last; Strikes = 0; }
        }

        private static readonly ConcurrentDictionary<string, Bucket> TcpBuckets = new ConcurrentDictionary<string, Bucket>();
        private static readonly ConcurrentDictionary<string, Bucket> UdpBuckets = new ConcurrentDictionary<string, Bucket>();
        private static readonly ConcurrentDictionary<string, bool> Banned = new ConcurrentDictionary<string, bool>();

        // Tunable limits
        private const long TCP_MAX_TOKENS = 6; // allowed new connections per window
        private const long UDP_MAX_TOKENS = 800; // allowed packets per window
        private const int WINDOW_MS = 1000; // refill window
        private const int BAN_THRESHOLD = 3; // strikes before ban
        private static readonly string BanFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ipconfig.json");

        static AntiDDoS()
        {
            LoadBans();
            // Cleanup thread to remove stale entries
            Thread cleanup = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        foreach (var key in TcpBuckets.Keys)
                        {
                            if (TcpBuckets.TryGetValue(key, out var b) && now - b.LastRefillMs > 60000)
                                TcpBuckets.TryRemove(key, out _);
                        }
                        foreach (var key in UdpBuckets.Keys)
                        {
                            if (UdpBuckets.TryGetValue(key, out var b) && now - b.LastRefillMs > 60000)
                                UdpBuckets.TryRemove(key, out _);
                        }
                        
                        // also cleanup banned list entries that are stale (not removing by default)
                    }
                    catch { }
                    Thread.Sleep(30000);
                }
            }) { IsBackground = true };
            cleanup.Start();
        }

        private static void LoadBans()
        {
            try
            {
                if (!File.Exists(BanFilePath)) return;
                string json = File.ReadAllText(BanFilePath);
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list == null) return;
                foreach (var ip in list)
                {
                    Banned.TryAdd(ip, true);
                }
            }
            catch { }
        }

        private static void SaveBans()
        {
            try
            {
                var list = new List<string>(Banned.Keys);
                string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(BanFilePath, json);
            }
            catch { }
        }

        public static bool IsBanned(EndPoint remote)
        {
            try
            {
                string key = GetKey(remote);
                return Banned.ContainsKey(key);
            }
            catch { return false; }
        }

        public static void BanIp(string ip)
        {
            try
            {
                if (string.IsNullOrEmpty(ip)) return;
                if (Banned.TryAdd(ip, true))
                {
                    SaveBans();
                    try { IndusBrawl.Laser.Server.Logger.Warning("BANNED IP: " + ip); } catch { }
                }
            }
            catch { }
        }

        private static string GetKey(EndPoint remote)
        {
            if (remote is IPEndPoint ip)
                return ip.Address.ToString();
            return remote?.ToString() ?? "unknown";
        }

        public static bool AllowConnection(EndPoint remote)
        {
            try
            {
                if (IsBanned(remote))
                {
                    try { IndusBrawl.Laser.Server.Logger.Warning("Rejected connection from banned IP: " + GetKey(remote)); } catch { }
                    return false;
                }
                string key = GetKey(remote);
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var bucket = TcpBuckets.GetOrAdd(key, _ => new Bucket(TCP_MAX_TOKENS, now));
                lock (bucket)
                {
                    long elapsed = now - bucket.LastRefillMs;
                    if (elapsed >= WINDOW_MS)
                    {
                        long refill = (elapsed / WINDOW_MS) * TCP_MAX_TOKENS;
                        bucket.Tokens = Math.Min(TCP_MAX_TOKENS, bucket.Tokens + refill);
                        bucket.LastRefillMs = now;
                    }

                    if (bucket.Tokens <= 0)
                    {
                        bucket.Strikes++;
                        if (bucket.Strikes >= BAN_THRESHOLD)
                        {
                            BanIp(key);
                            return false;
                        }
                        return false;
                    }

                    bucket.Tokens--;
                    // reset strikes on successful acceptance
                    bucket.Strikes = 0;
                    return true;
                }
            }
            catch { return true; }
        }

        public static bool AllowUdpPacket(EndPoint remote)
        {
            try
            {
                if (IsBanned(remote))
                {
                    try { IndusBrawl.Laser.Server.Logger.Warning("Dropped UDP from banned IP: " + GetKey(remote)); } catch { }
                    return false;
                }
                string key = GetKey(remote);
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var bucket = UdpBuckets.GetOrAdd(key, _ => new Bucket(UDP_MAX_TOKENS, now));
                lock (bucket)
                {
                    long elapsed = now - bucket.LastRefillMs;
                    if (elapsed >= WINDOW_MS)
                    {
                        long refill = (elapsed / WINDOW_MS) * UDP_MAX_TOKENS;
                        bucket.Tokens = Math.Min(UDP_MAX_TOKENS, bucket.Tokens + refill);
                        bucket.LastRefillMs = now;
                    }

                    if (bucket.Tokens <= 0)
                    {
                        bucket.Strikes++;
                        if (bucket.Strikes >= BAN_THRESHOLD)
                        {
                            BanIp(key);
                            return false;
                        }
                        return false;
                    }

                    bucket.Tokens--;
                    bucket.Strikes = 0;
                    return true;
                }
            }
            catch { return true; }
        }
    }
}
