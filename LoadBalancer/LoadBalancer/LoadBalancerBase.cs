
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;

namespace LoadBalancer
{
    public abstract class LoadBalancerBase
    {
        bool _started;
        public LoadModes LoadMode;
        public bool Pooling = false;
        public int PoolCount = 3000;
        public enum LoadModes
        {
            Equal = 0, Weighted = 1, Auto = 2, LeastResponseTime = 3, FirstActive = 4
        }
        public int RequestTimeout = 10000;
        [JsonIgnore]
        ConcurrentDictionary<long, long> RequestRates = new ConcurrentDictionary<long, long>();
        [JsonIgnore]
        ConcurrentDictionary<long, long> ResponseRates = new ConcurrentDictionary<long, long>();
        ConcurrentDictionary<long, long> SendRates = new ConcurrentDictionary<long, long>();
        ConcurrentDictionary<long, long> SendBackRates = new ConcurrentDictionary<long, long>();
        System.Timers.Timer WeightCalculation = new System.Timers.Timer(1000);
        void CalculateWights()
        {
            if (LoadMode == LoadModes.Auto)
            {
                decimal SumResponse = 0;
                foreach (Server srv in Servers)
                {
                    SumResponse += srv.ResponseTime;
                }

                if (SumResponse > 0)
                {
                    foreach (Server srv in Servers)
                    {
                        if (srv.ResponseTime > 0)
                            srv.Statistics.Weight = Convert.ToInt32(50 - ((srv.ResponseTime * 50) / SumResponse));
                    }
                }
            }
        }
        [JsonIgnore]
        public long Requests
        {
            get { return _Requests; }
        }
        [JsonIgnore]
        public long Responses
        {
            get { return _Responses; }
        }
        [JsonIgnore]
        public long Sends
        {
            get { return _Sends; }
        }
        [JsonIgnore]
        public long SendBacks
        {
            get { return _SendBacks; }
        }
        [JsonIgnore]
        public long Faileds
        {
            get { return _Faileds; }
        }

        long _Requests;
        long _Responses;
        long _Sends;
        long _SendBacks;
        internal  long _Faileds;

        [JsonIgnore]
        public long RequestPerSecond
        {
            get
            {

                DateTime dt = DateTime.Now;
                DateTime ndt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-1);
                if (RequestRates.ContainsKey(ndt.Ticks))
                {
                    return RequestRates[ndt.Ticks];
                }
                return 0;
            }
        }
        [JsonIgnore]
        public long ResponsePerSecond
        {
            get
            {
                DateTime dt = DateTime.Now;
                DateTime ndt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-1);
                if (ResponseRates.ContainsKey(ndt.Ticks))
                {
                    return ResponseRates[ndt.Ticks];
                }
                return 0;
            }
        }
        [JsonIgnore]
        public long SendPerSecond
        {
            get
            {
                DateTime dt = DateTime.Now;
                DateTime ndt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-1);
                if (SendRates.ContainsKey(ndt.Ticks))
                {
                    return SendRates[ndt.Ticks];
                }
                return 0;
            }
        }
        [JsonIgnore]
        public long SendBackPerSecond
        {
            get
            {
                DateTime dt = DateTime.Now;
                DateTime ndt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-1);
                if (SendBackRates.ContainsKey(ndt.Ticks))
                {
                    return SendBackRates[ndt.Ticks];
                }
                return 0;
            }
        }
        internal void AddRequestPerSecond()
        {
            Interlocked.Increment(ref _Requests);
            DateTime dt = DateTime.Now;
            DateTime ndt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-1);
            if (RequestRates.ContainsKey(ndt.Ticks))
            {
                RequestRates[ndt.Ticks]++;
            }
            else
            {
                RequestRates.TryAdd(ndt.Ticks, 0);
                RequestRates[ndt.Ticks]++;
            }
        }

        internal void AddResponsePerSecond()
        {
            Interlocked.Increment(ref _Responses);
            DateTime dt = DateTime.Now;
            DateTime ndt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-1);
            if (ResponseRates.ContainsKey(ndt.Ticks))
            {
                ResponseRates[ndt.Ticks]++;

            }
            else
            {
                ResponseRates.TryAdd(ndt.Ticks, 0);
                ResponseRates[ndt.Ticks]++;
            }
        }

        internal  void AddSendBackPerSecond()
        {
            Interlocked.Increment(ref _SendBacks);
            DateTime dt = DateTime.Now;
            DateTime ndt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-1);
            if (SendBackRates.ContainsKey(ndt.Ticks))
            {
                SendBackRates[ndt.Ticks]++;
            }
            else
            {
                SendBackRates.TryAdd(ndt.Ticks, 0);
                SendBackRates[ndt.Ticks]++;
            }
        }

        internal void AddSendPerSecond()
        {
            Interlocked.Increment(ref _Sends);
            DateTime dt = DateTime.Now;
            DateTime ndt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(-1);
            if (SendRates.ContainsKey(ndt.Ticks))
            {
                SendRates[ndt.Ticks]++;
            }
            else
            {
                SendRates.TryAdd(ndt.Ticks, 0);
                SendRates[ndt.Ticks]++;
            }
        }

        BlockingCollection<NetworkHandler.NetworkEventHandler> NetworkHandlers = new BlockingCollection<NetworkHandler.NetworkEventHandler>();
        [JsonIgnore]
        public bool Started
        {
            get { return _started; }
        }
        public enum Protocols
        {
            IP, UDP, TCP, Radius
        }
        public Protocols Protocol;

        public List<IPEndPoint> Endpoints = new List<IPEndPoint>();

        public abstract void AddEndpoint(IPEndPoint endPoint);

        public abstract void RemoveEndpoint(IPEndPoint endPoint);


        public string GroupName;
        public List<Server> Servers = new List<Server>();
        public List<HealthChecker> HealthCheckers = new List<HealthChecker>();
        public void AddServer(Server server)
        {

            if (Started)
            {
                foreach (HealthChecker healthChecker in HealthCheckers)
                {
                    HealthChecker checker = (HealthChecker)healthChecker.Clone();
                    server.HealthCheckers.Add(healthChecker.Id, checker);
                    checker.ServerAddress = server.IPAddress;
                    checker.Start();
                }
            }
            Servers.Add(server);
        }
        public void AddHealthChecker(HealthChecker healthChecker)
        {

            if (Started)
            {
                foreach (Server server in Servers)
                {
                    HealthChecker checker = (HealthChecker)healthChecker.Clone();
                    server.HealthCheckers.Add(healthChecker.Id, checker);
                    checker.ServerAddress = server.IPAddress;
                    checker.Start();
                }
            }
            HealthCheckers.Add(healthChecker);
        }

        public abstract NetworkHandler.NetworkBase GetNetwork(int ReleaseAfter, IPEndPoint endPoint);
        public void Start()
        {

            StartCheckers();
            WeightCalculation.Elapsed += UpdateWeight_Elapsed;
            WeightCalculation.Enabled = true;
            WeightCalculation.Start();

            _started = true;
        }
         

        private void UpdateWeight_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            CalculateWights();
        }

        public void StartCheckers()
        {
            foreach (HealthChecker healthChecker in HealthCheckers)
            {
                foreach (Server server in Servers)
                {
                    HealthChecker checker = (HealthChecker)healthChecker.Clone();
                    server.HealthCheckers.Add(healthChecker.Id, checker);
                    checker.ServerAddress = server.IPAddress;
                    checker.Statistics = new Statistic();
                    checker.Start();
                }
            }
        }
        public void StopCheckers()
        {

            foreach (HealthChecker checker in Servers.SelectMany(x => x.HealthCheckers.Values))
            {
                checker.Stop();
            }

        }
        public void Binding()
        {
            foreach (EndPoint endPoint in Endpoints)
            {
                Bind(endPoint);
            }
        }

        public abstract void Bind(EndPoint endPoint);
         
        void SortServers(List<Server> ActiveServers)
        {
            List<Server> sortedServers = new List<Server>();

        }

       

       

        public void Stop()
        {
            StopCheckers();
            _started = false;
        }
    }
}
