
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace LoadBalancer
{
    public class ICMPHealthChecker : HealthChecker
    {

        public int RequestLength = 32;
        public int TTL = 30;
        public bool Fragment = false;
        public string LastError;

        override public bool SendRequest()
        {
            try
            {


                Ping ping = new Ping();
                PingReply reply = ping.Send(ServerAddress, RequestTimeout, new byte[RequestLength], new PingOptions(TTL, !Fragment));
                if (reply.Status != IPStatus.Success)
                {
                    LastError = reply.Status.ToString();
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                return false;
            }
        }


        override public void Check()
        {

            if (SendRequest())
            {
                Successes++;
            }
            else
            {
                Failures++;
            }

            if (Failures + Successes == RetryCount)
            {
                if (Successes >= MinSuccessful)
                {
                    Status = Statuses.Ok;
                }
                else
                {
                    Status = Statuses.Failed;
                }
                Failures = 0;
                Successes = 0;
            }

            if (Status == Statuses.Failed)
            {
                CheckingTimer.Interval = RecheckInterval;
            }
            else
            {
                CheckingTimer.Interval = RetryInterval;
            }
        }

    }
}
