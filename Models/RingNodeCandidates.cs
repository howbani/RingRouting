using RingRouting.Dataplane;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingRouting.Models
{
    public class RingNodeCandidates
    {
         public Sensor Node{get;set;}
         public double Distance { get; set; }

         public RingNodeCandidates(Sensor me, double distance)
        {
            Node = me;
            Distance = distance;
        }

        
    }
}
