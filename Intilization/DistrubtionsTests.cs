using RingRouting.ControlPlane.NOS;
using RingRouting.ControlPlane.NOS.FlowEngin;
using RingRouting.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RingRouting.Charts.Intilization 
{
   public class DistrubtionsTests
    {
        /// <summary>
        ///  en.H = (i * Hpiovot);
        /// </summary>
        /// <param name="neigCount"></param>
        /// <param name="Hpiovot"></param>
        /// <returns></returns>


        /// <summary>
        /// 5, 200, 10
        /// </summary>
        /// <param name="neiCount"></param>
        /// <param name="disPiovot"></param>
        /// <returns></returns>
        public static List<DownlinkFlowEnery> TestDvalue(int neiCount,int step, int disPiovot) 
        {
            List<DownlinkFlowEnery> table = new List<DownlinkFlowEnery>();
            // normalized values.

            for (int i = 1; i <= neiCount; i++)
            {
                DownlinkFlowEnery en = new DownlinkFlowEnery();
                en.D = step + (disPiovot * i);
                en.DN = (en.D) / ((step + (disPiovot * (neiCount + 1))));
                table.Add(en);
            }

            // pro sum
            double DpSum = 0;

            foreach (DownlinkFlowEnery en in table)
            {
                DpSum += (Math.Pow((1 - Math.Sqrt(en.DN)), 1 + Settings.Default.ExpoDirCnt));
            }

            foreach (DownlinkFlowEnery en in table)
            {
                en.DP = (Math.Pow((1 - Math.Sqrt(en.DN)), 1 + Settings.Default.ExpoDirCnt)) / DpSum;
            }
            return table;
        }

    


        }
}
