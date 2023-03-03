

using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace LoadBalancer
{
    public class LoadbalancStatistics
    {
        public long Requests;
        public long Responses;
        public long TimeOuts;
        decimal _ResponseTime;
        public decimal ResponseTime
        {
            get
            {
                return _ResponseTime;

            }
            set
            {
                _ResponseTime = value;
            }
        }
        int Sequence = 10;
        ConcurrentBag<decimal> ResponseTimes = new ConcurrentBag<decimal>();
        long _LastUsed = DateTime.Now.AddMinutes(-1).Ticks;
        public int Weight = 0;
        [JsonIgnore]
        public long LastUsed
        {
            get
            {
                return _LastUsed;
            }
            set
            {
                if (Weight <= 0)
                {
                    _LastUsed = value;

                }
                else
                {
                    Interlocked.Decrement(ref Weight);
                }
            }
        }
    }
}
