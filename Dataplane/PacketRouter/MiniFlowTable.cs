using RingRouting.Dataplane.NOS;
using System;

namespace RingRouting.Dataplane.PacketRouter
{
    public enum FlowAction { Forward, Drop }

    public class MiniFlowTableEntry
    {
        public int SID { get; set; } 
        public int NID { get { return NeighborEntry.NeiNode.ID; } }


        public double DownLinkPriority { get; set; }
        public FlowAction DownLinkAction { get; set; }
        public double DownLinkStatistics { get; set; }

        public SensorState SensorState { get { return NeighborEntry.NeiNode.CurrentSensorState; } }
        public bool SensorBufferHasSpace { get { return (NeighborEntry.NeiNode.CanRecievePacket); } }
        public double Statistics { get { return DownLinkStatistics; } }
        public  NeighborsTableEntry NeighborEntry { get; set; }
        public NeighborsTableEntry ForwardersEnry { get; set; }
        public bool isNull = true;

        public void printEntry()
        {
            Console.WriteLine("ID: {0}",this.NeighborEntry.ID);
            Console.WriteLine("E: {0} , EN: {1} , EP: {2}", this.NeighborEntry.E, this.NeighborEntry.EN, this.NeighborEntry.EP);
            Console.WriteLine("D: {0} , DN: {1} , DP: {2}", this.NeighborEntry.D, this.NeighborEntry.DN, this.NeighborEntry.DP);
            Console.WriteLine("P: {0} , PN: {1} , PP: {2}", this.NeighborEntry.pirDis, this.NeighborEntry.pirDisNorm, this.NeighborEntry.pirDisProb);
            Console.WriteLine("Link Estimation: {0}", this.DownLinkPriority);
            Console.WriteLine("***");
        }
    }
}
