namespace IndusBrawl.Laser.Server.Networking
{
    using System;
    using System.Collections.Generic;

    public class RateLimiter
    {
        private readonly int MaxRequestsPerMinute;
        private readonly Queue<DateTime> RequestTimes;
        private readonly object LockObject;

        public RateLimiter(int maxRequestsPerMinute = 30)
        {
            MaxRequestsPerMinute = maxRequestsPerMinute;
            RequestTimes = new Queue<DateTime>();
            LockObject = new object();
        }

        public bool CheckLimit()
        {
            lock (LockObject)
            {
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);

                // Eski istekleri temizle
                while (RequestTimes.Count > 0 && RequestTimes.Peek() < oneMinuteAgo)
                {
                    RequestTimes.Dequeue();
                }

                // Limit kontrolü
                if (RequestTimes.Count >= MaxRequestsPerMinute)
                {
                    return false;
                }

                // Yeni isteği ekle
                RequestTimes.Enqueue(now);
                return true;
            }
        }

        public void Reset()
        {
            lock (LockObject)
            {
                RequestTimes.Clear();
            }
        }
    }
}
