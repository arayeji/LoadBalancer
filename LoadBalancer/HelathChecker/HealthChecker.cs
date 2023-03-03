
using Newtonsoft.Json;

namespace LoadBalancer
{
    public abstract class HealthChecker : ICloneable
    {
        Guid _id = Guid.NewGuid();
        public Guid Id { get { return _id; } }
        public enum Types
        {
            UDP, TCP, HTTP, Radius, ICMP
        }
        public Types Type;
        public Statuses Status = Statuses.FirstRun;
        public enum Statuses
        {
            Disable, Ok, Failed, FirstRun
        }
        public string ServerAddress;
        public int RequestTimeout = 3000;
        public int RetryInterval = 3000;
        public int RecheckInterval = 10000;
        int _MinSuccessful = 3;
        int _RetryCount = 3;

        public int RetryCount
        {
            set
            {
                if (value < 1)
                    throw new Exception("RetryCount can't be lower than 1");

                _RetryCount = value;
                if (MinSuccessful > _RetryCount)
                    _MinSuccessful = _RetryCount;
            }
            get
            {
                return _RetryCount;
            }
        }

        public int MinSuccessful
        {
            set
            {
                if (value < 1)
                    throw new Exception("MinSuccessful can't be lower than 1");

                if (_RetryCount < value)
                    _RetryCount = value;
                _MinSuccessful = value;
            }
            get
            {
                return _MinSuccessful;
            }
        }
        [JsonIgnore]
        public Statistic? Statistics;

        internal int Failures = 0;
        internal int Successes = 0;



        internal System.Timers.Timer CheckingTimer;
        public void Start()
        {
            if (ServerAddress != null)
            {
                CheckingTimer = new System.Timers.Timer();
                CheckingTimer.Interval = RetryInterval;
                CheckingTimer.Enabled = true;
                CheckingTimer.Elapsed += CheckingTimer_Elapsed;
                CheckingTimer.Start();
            }
            else
                throw new Exception("ServerAddress is required");
        }
        public void Stop()
        {
            CheckingTimer.Stop();
            CheckingTimer.Enabled = false;
            CheckingTimer.Dispose();
        }

        private void CheckingTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Check();
        }

        public abstract void Check();
        public abstract bool SendRequest();

        public object Clone()
        {
            HealthChecker healthChecker = ((HealthChecker)this.MemberwiseClone());
            healthChecker.Statistics = new Statistic();
            return healthChecker;
        }
    }


}
