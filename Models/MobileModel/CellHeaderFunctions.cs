using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using RingRouting.Constructor;
using RingRouting.Dataplane;
using RingRouting.Dataplane.PacketRouter;
using RingRouting.Intilization;

namespace RingRouting.Models.MobileModel
{
    public class CellHeaderFunctions
    {
        public static void assignClusterHead(Cluster Cell, bool isRechange)
        {
            double offset = PublicParameters.clusterRadius;
            Sensor holder = null;

            if (!isRechange)
            {
                foreach (Sensor sen in Cell.clusterNodes)
                {
                    double distance = Operations.DistanceBetweenTwoPoints(Cell.clusterCenterComputed, sen.CenterLocation);
                    if (distance < offset)
                    {
                        offset = distance;
                        holder = sen;
                    }
                }
               
                
            }
            else
            {
                // check according to remaining enery not according to distance
                double sum = 0;
                foreach (Sensor sen in Cell.clusterNodes)
                {
                    sum += sen.ResidualEnergyPercentage;
                }
                double max = 0;
            
                foreach (Sensor sen in Cell.clusterNodes)
                {
                    sen.CellHeaderProbability = sen.ResidualEnergyPercentage / sum;
                    if (sen.CellHeaderProbability > max)
                    {
                        max = sen.CellHeaderProbability;
                        holder = sen;
                    }
                }

                if (holder.ID != Cell.clusterHeader.headerSensor.ID)
                {
                    PublicParameters.SinkNode.MainWindow.Dispatcher.Invoke(() => Cell.clusterHeader.headerSensor.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Hidden);
                }
             
            }
            

            try
            {
                Cell.clusterHeader.headerSensor = holder;
                Cell.clusterHeader.headerID = holder.ID;
                Cell.clusterHeader.headerCenterLocation = holder.CenterLocation;
            }
            catch
            {
                holder = null;
                MessageBox.Show("Error in assiging Cluster Header");
                return;
            }
            holder.Ellipse_HeaderAgent_Mark.Stroke = new SolidColorBrush(Colors.Red);
            PublicParameters.SinkNode.MainWindow.Dispatcher.Invoke(() => holder.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Visible);
            Cell.clusterHeader.atTreeDepth = Cell.clusterLevel;
            Cell.clusterHeader.headerSensor.ClusterHeader = Cell.clusterHeader;
            ClusterHeaderTable.populateHeaderInformation();
        }

        
    }
}
