
using System.Collections.Concurrent;

namespace LoadBalancer
{
    public class Statistic
    {
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

    }
}
