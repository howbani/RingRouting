using RingRouting.Intilization;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using RingRouting.Properties;
namespace RingRouting.Dataplane.NOS
{
    public enum PacketType { Beacon, Preamble, ACK, Data, ANS, OldANS, ANPI, QReq, QResp, ANPISClock,ANPISAntiClock }

    public class Packet
    {
        //: Packet section:
        public long PID { get; set; } // SEQ ID OF PACKET.
        public PacketType PacketType { get; set; }
        public bool isDelivered { get; set; }
        public double PacketLength { get; set; }
   
        public int TimeToLive { get; set; }
        public int Hops { get; set; }
        public string Path { get; set; }
        public double RoutingDistance { get; set; }
        public double Delay = 0;
        public double UsedEnergy_Joule { get; set; }
        public int WaitingTimes { get; set; }
        public int ReTransmissionTry { get; set; }

        public string DroppedReason { get; set; }

        public void ComputeDelay()
        {
            List<int> myPath = Operations.PacketPathToIDS(Path);
            int j = 1;
            for (int i = 0; i <= myPath.Count - 2; i++)
            {
                Sensor tx = PublicParameters.myNetwork[myPath[i]];
                Sensor rx = PublicParameters.myNetwork[myPath[j]];
                Delay += DelayModel.DelayModel.Delay(tx, rx);
                j++;
            }
            Delay += (Settings.Default.QueueTime * WaitingTimes);

        }

        public double EuclideanDistance
        { 
           
            get {
                if (PacketType == PacketType.QReq || PacketType == PacketType.ANPI)
                    {
                        return Operations.DistanceBetweenTwoPoints(Source.CenterLocation, PointDestination);
                    }
                    else
                    {
                        return Operations.DistanceBetweenTwoSensors(Source, Destination); 
                    }
                }
        }

        /// <summary>
        /// eff 100%
        /// </summary>
        public double RoutingDistanceEfficiency
        {
            get
            {
                return 100 * (EuclideanDistance / RoutingDistance);
            }
        }

        /// <summary>
        /// Average Transmission Distance (ATD): for〖 P〗_b^s (g_k ), we define average transmission distance per hop as shown in (28).
        /// </summary>
        public double AverageTransDistrancePerHop
        {
            get
            {
                return (RoutingDistance / Hops);
            }
        }


        public double TransDistanceEfficiency
        {
            get
            {
                return 100 * (1 - (RoutingDistance / (PublicParameters.CommunicationRangeRadius * Hops * (Hops + 1))));
            }
        }


        /// <summary>
        /// RoutingEfficiency
        /// </summary>
        public double RoutingEfficiency
        {
            get
            {
                return (RoutingDistanceEfficiency + TransDistanceEfficiency) / 2;
            }
        }

        public bool isAdvirtismentPacket()
        {
            if(this.PacketType != PacketType.Data && this.PacketType !=PacketType.ACK && this.PacketType !=PacketType.Preamble
                && this.PacketType != PacketType.Beacon)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Sensor Source { get; set; }
        public Sensor Destination { get; set; }
        public Sensor OldAgent { get; set; }
        public Sensor SinkAgent { get; set; }
        public Sensor Root { get; set; }

        public Point PointDestination { get; set; }
    }
}
