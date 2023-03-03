
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LoadBalancer
{
    public class UDPHealthChecker : HealthChecker, ICloneable
    {

        public string LastError;
        public int Port;
        public string RequestMessage;
        public string SuccessfulMessage;

        override public bool SendRequest()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                UdpClient udp = new UdpClient();
                udp.Client.ReceiveTimeout = RequestTimeout;

                udp.Send(Encoding.UTF8.GetBytes(RequestMessage), Encoding.UTF8.GetByteCount(RequestMessage), new IPEndPoint(IPAddress.Parse(ServerAddress), Port));

                IPEndPoint ipe = new IPEndPoint(IPAddress.Any, 0);
                byte[] recbt = udp.Receive(ref ipe);

                string rec = Encoding.UTF8.GetString(recbt).ToLower();
                sw.Stop();
                if (rec != SuccessfulMessage)
                {
                    LastError = rec;
                    return false;
                }
                else
                {
                    Statistics.ResponseTime = sw.ElapsedMilliseconds;
                    return true;
                }

            }
            catch (Exception ex)
            {
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
