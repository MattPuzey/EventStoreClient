using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace EventstoreClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running demo code...");

            var settings = ConnectionSettings.Create()
                .KeepReconnecting()
                .SetReconnectionDelayTo(TimeSpan.FromSeconds(10))
                .UseConsoleLogger();
            //
            // Attempt to connect to single (unreachable) IP 
            //
            var singleInstanceConnection = EventStoreConnection.Create(settings, CreateIPEndPoint(1));
            singleInstanceConnection.ConnectAsync().Wait();
            singleInstanceConnection.Disconnected += singleInstanceConnection_Disconnected;


            //
            // Attempt to connect to (unreachable) cluster of IPs
            //
            var clusterSettings = ClusterSettings.Create()
                .DiscoverClusterViaGossipSeeds()
                .SetGossipSeedEndPoints(GetGossipSeeds())
                .SetMaxDiscoverAttempts(int.MaxValue)
                .Build();

            // Connection to cluster appears to ignore settings. Reconnects 10 times then stops.
            var clusteredInstanceConnection = EventStoreConnection.Create(settings, clusterSettings);
            clusteredInstanceConnection.ConnectAsync().Wait();

            //
            // ReadEventAsync
            //

            Console.WriteLine("Started trying to connect...");
            Console.ReadLine();
        }

        static void singleInstanceConnection_Disconnected(object sender, ClientConnectionEventArgs e)
        {
            Console.WriteLine("Diconnected from {0}", e);
        }

        private static IPEndPoint CreateIPEndPoint(int port)
        {
            var address = IPAddress.Parse("1");
            return new IPEndPoint(address, port);
        }

        private static IPEndPoint[] GetGossipSeeds()
        {
            //assumes a format like "192.168.147.51:6113#192.168.147.52:6113#192.168.147.51:6113"
            var gossipSeedConfiguration = ConfigurationManager.AppSettings["EventStoreGossipSeeds"];
            var endPoints = new List<IPEndPoint>();
            foreach (var gossipSeed in gossipSeedConfiguration.Split('#'))
            {
                var ipAddress = gossipSeed.Split(':')[0];
                var port = Convert.ToInt32(gossipSeed.Split(':')[1]);
                endPoints.Add(new IPEndPoint(IPAddress.Parse(ipAddress), port));
            }
            return endPoints.ToArray();
        }
    }
}

