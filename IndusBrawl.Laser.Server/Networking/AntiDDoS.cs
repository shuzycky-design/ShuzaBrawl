namespace IndusBrawl.Laser.Server.Networking
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;

    internal static class AntiDDoS
    {
        private static readonly object Lock = new object();
        private static Dictionary<IPAddress, (int Attempts, DateTime WindowStart)> Attempts = new Dictionary<IPAddress, (int, DateTime)>();
        private static HashSet<IPAddress> Blacklist = new HashSet<IPAddress>();
        private static string BlacklistFilePath = "blocked_ips.txt";
        private static Timer ReloadTimer;
        private static bool Enabled = false;
        private static TimeSpan Window = TimeSpan.FromSeconds(60);
        private static int Threshold = 10;

        public static void Init(string blacklistPath, bool enabled, int threshold = 10, TimeSpan? window = null)
        {
            Enabled = enabled;
            if (!string.IsNullOrEmpty(blacklistPath)) BlacklistFilePath = blacklistPath;
            Threshold = threshold;
            if (window.HasValue) Window = window.Value;

            if (Enabled)
            {
                LoadBlacklist();
                ReloadTimer = new Timer(_ => { LoadBlacklist(); }, null, TimeSpan.FromSeconds(100), TimeSpan.FromSeconds(100));
            }
        }

        public static bool AllowConnection(IPAddress ip)
        {
            if (!Enabled) return true;

            lock (Lock)
            {
                if (Blacklist.Contains(ip)) return false;

                var now = DateTime.UtcNow;
                if (!Attempts.TryGetValue(ip, out var entry))
                {
                    Attempts[ip] = (1, now);
                    return true;
                }

                if (now - entry.WindowStart > Window)
                {
                    Attempts[ip] = (1, now);
                    return true;
                }

                entry.Attempts++;
                Attempts[ip] = (entry.Attempts, entry.WindowStart);

                if (entry.Attempts > Threshold)
                {
                    Blacklist.Add(ip);
                    try
                    {
                        var existing = File.Exists(BlacklistFilePath) ? new HashSet<string>(File.ReadAllLines(BlacklistFilePath)) : new HashSet<string>();
                        if (!existing.Contains(ip.ToString()))
                        {
                            File.AppendAllText(BlacklistFilePath, ip + Environment.NewLine);
                        }
                    }
                    catch
                    {
                        // ignore file errors
                    }

                    return false;
                }

                return true;
            }
        }

        public static void LoadBlacklist()
        {
            if (File.Exists(BlacklistFilePath))
            {
                lock (Lock)
                {
                    Blacklist.Clear();
                    foreach (var line in File.ReadAllLines(BlacklistFilePath))
                    {
                        if (IPAddress.TryParse(line.Trim(), out var ip))
                        {
                            Blacklist.Add(ip);
                        }
                    }
                }
            }
        }
    }
}
