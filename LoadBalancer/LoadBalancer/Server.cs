

using Newtonsoft.Json;

namespace LoadBalancer
{
    public class Server
    {
        public string Name;
        public string Description;
        public string IPAddress;
        public Guid Id = Guid.NewGuid();
        public int Port;
        [JsonIgnore]
        public LoadbalancStatistics Statistics = new LoadbalancStatistics();
        [JsonIgnore]
        public Dictionary<Guid, HealthChecker> HealthCheckers = new Dictionary<Guid, HealthChecker>();
        [JsonIgnore]
        public decimal ResponseTime
        {
            get
            {
                decimal max = 0;

                foreach (HealthChecker checker in HealthCheckers.Values)
                {
                    if (checker.Statistics != null)
                    {
                        if (checker.Statistics.ResponseTime > max)
                            max = checker.Statistics.ResponseTime;
                    }
                    else
                        checker.Statistics = new Statistic();
                }

                return max;
            }
        }
        [JsonIgnore]
        public Statuses Status
        {
            get
            {
                Statuses status = Statuses.Ok;
                foreach (HealthChecker healthChecker in HealthCheckers.Values)
                {
                    if (healthChecker.Status != HealthChecker.Statuses.Ok)
                        status = Statuses.Failed;
                }
                return status;
            }
        }

        public enum Statuses
        {
            Disable, Ok, Failed
        }
    }
}
