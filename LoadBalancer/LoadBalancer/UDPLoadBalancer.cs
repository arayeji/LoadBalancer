using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer
{
    public class UDPLoadBalancer : LoadBalancerBase
    {
        public override void AddEndpoint(IPEndPoint endPoint)
        {
            if (Started)
            {
                switch (Protocol)
                {
                    case Protocols.UDP:
                        Bind(endPoint);
                        break;
                }
            }
            Endpoints.Add(endPoint);
        }

        public override void RemoveEndpoint(IPEndPoint endPoint)
        {
            if (Started)
            {
                Bind(endPoint);
            }
            Endpoints.Remove(endPoint);
        }
        public override NetworkHandler.NetworkBase GetNetwork(int ReleaseAfter, IPEndPoint endPoint)
        {

            NetworkHandler.NetworkBase network = new NetworkHandler.UDPNetwork(new IPEndPoint(IPAddress.Any, 0), NetworkHandler.NetworkBase.Modes.Client, AddressFamily.InterNetwork);

            return network;
        }

        public override void Bind(EndPoint endPoint)
        {
            NetworkHandler.NetworkBase nh = new NetworkHandler.UDPNetwork((IPEndPoint)endPoint, NetworkHandler.NetworkBase.Modes.Server, AddressFamily.InterNetwork);
            nh.OnPacketReceived += Nh_OnPacketReceived;
            nh.Start();

        }
        private void Nh_OnPacketReceived(object sender, NetworkHandler.NetworkEventHandler e)
        {
            try
            {

                AddRequestPerSecond();
                List<Server> ActiveServers = Servers.FindAll(x => x.Status == Server.Statuses.Ok);
                if (ActiveServers.Count > 0)
                {
                    Server server;
                    switch (LoadMode)
                    {
                        case LoadModes.Equal:
                            server = ActiveServers.OrderBy(x => x.Statistics.LastUsed).First();
                            break;
                        case LoadModes.LeastResponseTime:
                            server = ActiveServers.OrderBy(x => x.ResponseTime).First();
                            break;
                        case LoadModes.FirstActive:
                            server = ActiveServers.First();
                            break;
                        case LoadModes.Auto:
                            server = ActiveServers.OrderBy(x => x.ResponseTime).First();
                            break;
                        default:
                            server = ActiveServers.First();
                            break;
                    }



                    //Server server = ActiveServers.OrderBy(x=> x.HealthCheckers.Select(x=>x.Value.ResponseTime)).First();

                    server.Statistics.LastUsed = DateTime.Now.Ticks;
                    switch (Protocol)
                    {
                        case Protocols.UDP:


                            Stopwatch sw = new Stopwatch();
                            sw.Start();

                            NetworkHandler.NetworkBase nh = GetNetwork(RequestTimeout, new IPEndPoint(IPAddress.Parse(server.IPAddress), server.Port));

                            //if (!Pooling)
                            //{
                            bool Cancelled = false;
                            CancellationTokenSource token = new CancellationTokenSource();
                            token.CancelAfter(RequestTimeout);
                            token.Token.Register(() =>
                            {
                                //if (Pooling)
                                //{
                                //    NetworkPool.Release(nh, new IPEndPoint(IPAddress.Parse(server.IPAddress), server.Port));
                                //}

                                if (token != null)
                                {
                                    if (nh.Stop())
                                    {
                                        if (!Cancelled)
                                        {
                                            Interlocked.Increment(ref _Faileds);
                                            Interlocked.Increment(ref server.Statistics.TimeOuts);
                                        }
                                    }

                                    token.Dispose();
                                    e.Packet = null;

                                    nh = null;
                                }


                            });

                            //}

                            nh.OnPacketReceived += delegate (object sender, NetworkHandler.NetworkEventHandler e1)
                            {

                                AddResponsePerSecond();
                                Cancelled = true;
                                token.Cancel(false);
                                token.Dispose();
                                //if (Pooling)
                                //    NetworkPool.Release(nh, new IPEndPoint(IPAddress.Parse(server.IPAddress), server.Port));
                                //else
                                //{
                                //    nh.Stop();
                                //    nh = null;
                                //    token.Dispose();
                                //}

                                e.Network.Send(e.EndPoint, e1.Packet, e1.TransferedBytes, false);
                                // e.Packet = null;
                                Interlocked.Increment(ref server.Statistics.Responses);
                                sw.Stop();
                                server.Statistics.ResponseTime = sw.ElapsedMilliseconds;
                                AddSendBackPerSecond();


                            };


                            nh.Start();
                            nh.Send(new IPEndPoint(IPAddress.Parse(server.IPAddress), server.Port), e.Packet, e.TransferedBytes, true);
                            AddSendPerSecond();
                            Interlocked.Increment(ref server.Statistics.Requests);

                             

                            break;
                    }
                }
                else
                    Console.WriteLine("No avilable active server");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            e.Packet = new byte[0];
        }
    }
}
