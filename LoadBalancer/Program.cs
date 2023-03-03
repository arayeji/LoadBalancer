using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LoadBalancer
{
    internal class Program
    {
        static System.Timers.Timer RateUpdate = new System.Timers.Timer(1000);
        static public Config config = new Config();
        static void Main(string[] args)
        {
            config.Load();

            foreach(var sg in config.ServerGroups)
            {
                sg.Start();
                sg.Binding();
            }

            RateUpdate.Elapsed += RateUpdate_Elapsed;
            RateUpdate.Enabled = true;
            RateUpdate.Start();
            Read();

        }
         

        private static void RateUpdate_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                

                long Requests=0;
                long Responses=0;
                long Sends=0;
                long SendBacks=0;

                foreach (LoadBalancerBase group in config.ServerGroups)
                {
                    Requests += group.SendBackPerSecond;
                    Responses += group.ResponsePerSecond;
                    Sends += group.SendPerSecond;
                    SendBacks += group.SendBackPerSecond;

                }

                Console.Title = "Request TPS=(" + Requests + ") Send TPS=(" + Sends + ") Response TPS=(" + Responses + ") SendBack TPS=(" +SendBacks+ ")";
            }
            catch  
            {
                 
            }
        }
        static bool StressTesting = false;
        static void StressTest()
        {
            try
            {
                StressTesting = true;
                Task.Run(delegate ()
                {
                    while (StressTesting)
                    {
                        Task.Run(delegate () { Test("0Check"); });
                    }
                    int x = 0;
                });

             
            }
            catch (Exception ex)
            {
                Console.WriteLine("TestError: " + ex);
            }
        }
        static void Test(string Message)
        {
            try
            {


                UdpClient udp = new UdpClient();
                udp.Client.ReceiveTimeout = 2000;
                byte[] data = Encoding.UTF8.GetBytes(Message);
                udp.Send(data, data.Length, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1812));

                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, 0);
                byte[] recbt = udp.Receive(ref ipe);

                string rec = Encoding.UTF8.GetString(recbt).ToLower();
                Console.WriteLine("TestReposen: "+rec);
            }
            catch (Exception ex)
            {
                Console.WriteLine("TestError: " + ex);
            }
        }

       static  void Read()
        {
            string cmd= Console.ReadLine();
            if (cmd != null)
            {
                if (cmd.ToLower().StartsWith("save "))
                {
                    config.Save(cmd.ToLower().Replace("save ", ""));
                }
                else
                if (cmd.ToLower() == "save")
                {
                    config.Save("Config.cfg");
                }
                else
                 if (cmd.ToLower().StartsWith("reload "))
                {
                    config.Reload(cmd.ToLower().Replace("reload ", ""));
                }
                else
                if (cmd.ToLower() == "reload")
                {
                    config.Reload("Config.cfg");
                }
                else
               if (cmd.ToLower() == "cls")
                {
                    Console.Clear();
                }
                else
                    if (cmd.ToLower() == "stat")
                {
                    foreach (LoadBalancerBase group in config.ServerGroups)
                    {
                        Console.WriteLine(group.GroupName+": Request TPS=("+group.RequestPerSecond+ ") Response TPS=(" + group.ResponsePerSecond + ") Send TPS=(" + group.SendPerSecond + ") SendBack TPS=(" + group.SendBackPerSecond + ")\r\n\r\n");
                        Console.WriteLine("Request To LoadBalaner= ("+group.Requests+ ")\r\nSent To Servers=(" + group.Sends + ")\r\nResponse From Servers=(" + group.Responses + ")\r\nSentBack To Requesters=(" + group.SendBacks + ")\r\nFailed To Receive From Servers=(" + group.Faileds + ")\r\n\r\n");

                        foreach (Server server in group.Servers)
                        {
                            Console.WriteLine(server.IPAddress +" ("+server.Status+")\r\nRequests: "+server.Statistics.Requests+" , Responses: "+server.Statistics.Responses +" , NoResponse: "+(server.Statistics.Requests-server.Statistics.Responses) + " , TimedOut: " + server.Statistics.TimeOuts + " , ResponseTime: " + server.Statistics.ResponseTime);
                            foreach (HealthChecker checker in server.HealthCheckers.Values)
                            {
                                Console.WriteLine(checker.Type+" - "+ checker.Status +" - ResponseTime: "+checker.Statistics.ResponseTime);
                            }
                            Console.WriteLine("");
                        }
                        Console.WriteLine("--------------------------------------------------------------------");
                    }
                }
                else
                    if (cmd.StartsWith("test "))
                {
                    Test(cmd.Remove(0, 5));
                }
                else
                    if (cmd.StartsWith("stress test"))
                {
                    if (!StressTesting)
                        StressTest();
                    else
                        StressTesting = false;
                }
            }
            else
            {
                Console.WriteLine("Please enter \"exit\" to exit");
            }
            Read();
        }
    }
}