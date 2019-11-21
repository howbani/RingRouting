using RingRouting.Dataplane;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingRouting.Models.MobileModel
{
    public class RingNodes
    {
        public Sensor Node { get; set; }
        public Sensor ClockWiseNeighbor { get; set; }
        public Sensor AntiClockWiseNeighbor { get; set; }
        public bool isRingNode { get; set; }
        public Sensor AnchorNode { get; set; }

        public RingNodes()
        {
            isRingNode = false;
        }
        
        public RingNodes(Sensor me, Sensor next, Sensor prev)
        {
            Node = me;
            AntiClockWiseNeighbor = next;
            ClockWiseNeighbor = prev;
            isRingNode = true;
            AnchorNode = null;
        }

        public void checkForRecive()
        {
            foreach (RingNodes node in PublicParameters.RingNodes)
            {
                Console.WriteLine("RingNode: {0} has the anchor {1}", node.Node.ID, node.AnchorNode.ID);
            }
        }
       
    }
}
