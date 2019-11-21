using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingRouting.Dataplane.PacketRouter
{
    /// <summary>
    /// TABLE 2: NEIGHBORING NODES INFORMATION TABLE (NEIGHBORS-TABLE)
    /// </summary>
    public class NeighborsTableEntry
    {
        public int ID { get { return NeiNode.ID; } } // id of candidate.
                                                     // Elementry values:      
        public double RP { get; set; } // RSSI.

        public double L { get; set; }
        public double LN { get; set; }
        public double LP { get; set; } // battry level.
        public double batteryProb { get; set; }

      
        // closer to the sender and closer to the sink.
  
        
        // rssi:
     
        public double E { get; set; } //  IDRECTION TO THE SINK
        public double EN { get; set; } // // NORMLIZED
        public double EP { get; set; } // ECLIDIAN DISTANCE

      

        //
        public double D { get; set; } // 
        public double DN { get; set; } // D normalized 
        public double DP { get; set; } // distance from the me Candidate to target node.


        public double pirDis { get; set; }
        public double pirDisNorm { get; set; }
        public double pirDisProb { get; set; }


        public System.Windows.Point CenterLocation { get { return NeiNode.CenterLocation; } }
        //: The neighbor Node
        public Sensor NeiNode { get; set; }
    }

}
