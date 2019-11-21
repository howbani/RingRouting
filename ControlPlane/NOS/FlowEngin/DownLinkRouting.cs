using RingRouting.Dataplane;
using RingRouting.Dataplane.NOS;
using RingRouting.Dataplane.PacketRouter;
using RingRouting.Intilization;
using RingRouting.Properties;
using RingRouting.ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RingRouting.ControlPlane.NOS.FlowEngin
{
    public class MiniFlowTableSorterDownLinkPriority : IComparer<MiniFlowTableEntry>
    {

        public int Compare(MiniFlowTableEntry y, MiniFlowTableEntry x)
        {
            return x.DownLinkPriority.CompareTo(y.DownLinkPriority);
        }
    }


    public class DownlinkFlowEnery
    {
        public Sensor Current { get; set; }
        public Sensor Next { get; set; }
        public Sensor Target { get; set; }

        // Elementry values:
        public double D { get; set; } // direction value tworads the end node
        public double DN { get; set; } // R NORMALIZEE value of To. 
        public double DP { get; set; } // defual.

        public double L { get; set; } // remian energy
        public double LN { get; set; } // L normalized
        public double LP { get; set; } // L value of To.

        public double R { get; set; } // riss
        public double RN { get; set; } // R NORMALIZEE value of To. 
        public double RP { get; set; } // R NORMALIZEE value of To. 

        //Perpendicular Distance
        public double pirDis { get; set; }
        public double pirDisNorm { get; set; }



        //
        public double Pr
        {
            get;
            set;
        }

        // return:
        public double Mul
        {
            get
            {
                return LP * DP * RP;
            }
        }

        public int IindexInMiniFlow { get; set; }
        public MiniFlowTableEntry MiniFlowTableEntry { get; set; }
    }



    public class DownLinkRouting
    {
        public static double srcPerDis { get; set; }

        public static MiniFlowTableEntry getBiggest(List<MiniFlowTableEntry> table)
        {
            double offset = 0;
            MiniFlowTableEntry biggest = null;
            foreach (MiniFlowTableEntry entry in table)
            {
                if (entry.DownLinkPriority > offset)
                {
                    offset = entry.DownLinkPriority;
                    biggest = entry;
                }

            }
            return biggest;
        }
        public static MiniFlowTableEntry getSmallest(List<MiniFlowTableEntry> table)
        {
            double offset = table[0].DownLinkPriority + PublicParameters.CommunicationRangeRadius;
            MiniFlowTableEntry biggest = null;
            foreach (MiniFlowTableEntry entry in table)
            {
                if (entry.DownLinkPriority < offset)
                {
                    offset = entry.DownLinkPriority;
                    biggest = entry;
                }

            }
            return biggest;
        }
        public static void sortTable(Sensor sender)
        {

            List<MiniFlowTableEntry> beforeSort = sender.MiniFlowTable;
            List<MiniFlowTableEntry> afterSort = new List<MiniFlowTableEntry>();
            do
            {
                MiniFlowTableEntry big = null;
                try
                {
                    big = getSmallest(beforeSort);
                    afterSort.Add(big);
                    beforeSort.Remove(big);
                }
                catch
                {
                    big = null;
                    Console.WriteLine();
                }
              


            } while (beforeSort.Count > 0);
            sender.MiniFlowTable.Clear();
            sender.MiniFlowTable = afterSort;

        }
        /// <summary>
        /// This will be change per sender.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="endNode"></param>


        public static void GetD_Distribution(Sensor sender, Packet packet)
        {
            sender.MiniFlowTable.Clear();
            List<int> PacketPath = Operations.PacketPathToIDS(packet.Path);

            Sensor sourceNode = packet.Source;
            Point endNodePosition;
            if (packet.PacketType == PacketType.QReq || packet.PacketType == PacketType.ANPI)
            {
                endNodePosition = packet.PointDestination;
            }
            else
            {
                endNodePosition = packet.Destination.CenterLocation;
            }
            double distSrcToEnd = Operations.DistanceBetweenTwoPoints(sender.CenterLocation, endNodePosition);
            double ENDifference = distSrcToEnd - sender.ComunicationRangeRadius;

            foreach (NeighborsTableEntry neiEntry in sender.NeighborsTable)
            {
                if (neiEntry.NeiNode.ResidualEnergyPercentage > 0)
                {
                    if (neiEntry.ID != PublicParameters.SinkNode.ID)
                    {
                        MiniFlowTableEntry MiniEntry = new MiniFlowTableEntry();
                        MiniEntry.SID = sender.ID;
                        MiniEntry.NeighborEntry = neiEntry;
                        MiniEntry.DownLinkPriority = Operations.DistanceBetweenTwoPoints(endNodePosition, MiniEntry.NeighborEntry.CenterLocation);
                        sender.MiniFlowTable.Add(MiniEntry);
                    }

                }
            }

            sortTable(sender);
            int minus = 0;
            List<int> path = Operations.PacketPathToIDS(packet.Path);
            if (path.Count < 2)
            {
                minus = 1;
            }
            else
            {
                minus = 2;
            }

            int lastForwarder = path[path.Count - minus];
            foreach (MiniFlowTableEntry MiniEntry in sender.MiniFlowTable)
            {
                if (MiniEntry.NID != PublicParameters.SinkNode.ID)
                {
                    double srcEnd = Operations.DistanceBetweenTwoPoints(sender.CenterLocation, endNodePosition);
                    double candEnd = Operations.DistanceBetweenTwoPoints(MiniEntry.NeighborEntry.CenterLocation, endNodePosition);

                    if((path.Contains(MiniEntry.NID) && (candEnd < srcEnd))){
                        MiniEntry.DownLinkAction = FlowAction.Forward;
                    }
                    else
                    {
                        if (!(path.Contains(MiniEntry.NID)))
                        {
                            MiniEntry.DownLinkAction = FlowAction.Forward;
                        }
                        else
                        {
                            MiniEntry.DownLinkAction = FlowAction.Drop;
                        }
                    }
                    
                }


            }
          


        }


    }
}
