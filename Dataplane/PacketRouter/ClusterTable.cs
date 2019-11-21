using RingRouting.Constructor;
using RingRouting.Intilization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RingRouting.Dataplane.NOS;

namespace RingRouting.Dataplane.PacketRouter
{
    public partial class ClusterHeaderTable
    {
        

        public Sensor headerSensor { get; set; }
        public Point headerCenterLocation { get; set; }
        public int headerID { get; set; }

        public Queue<Packet> CellHeaderBuffer = new Queue<Packet>();

        //For the other clusters
        public Sensor sourceHeader { get; set; }

        public Sensor sourceNode { get; set; }

        public Sensor parentHeaderSen { get; set; }

        public List<Sensor> childrenHeadersSen = new List<Sensor>();

        public Point SinkPosition { get; set; }
        public bool hasSinkPosition = false;
        public bool isRootHeader = false;

        public Sensor SinkAgent { get; set; }

        public int atTreeDepth { get; set; }
        

        public double getDistanceFromRoot()
        {
            double radius = PublicParameters.clusterRadius;
            double offset = radius;
            return offset * atTreeDepth;

        }

        public static void populateHeaderInformation()
        {
            
            foreach (Cluster cluster in PublicParameters.networkClusters)
            {

                if (cluster.getID() == Tree.rootClusterID)
                {
                    cluster.clusterHeader.isRootHeader = true;
                    
                }
                else
                {
                    cluster.clusterHeader.isRootHeader = false;
                    cluster.clusterHeader.parentHeaderSen = cluster.parentCluster.clusterHeader.headerSensor;
                }
                if (cluster.childrenClusters.Count > 0)
                {
                    foreach (Cluster child in cluster.childrenClusters)
                    {
                        cluster.clusterHeader.childrenHeadersSen.Add(child.clusterHeader.headerSensor);
                    }
                }

            }

        }

        public void StoreInCellHeaderBuffer(Packet packet)
        {
            
            CellHeaderBuffer.Enqueue(packet);
        }

        public void DelieverPacketsInBuffer()
        {
            if (CellHeaderBuffer.Count > 0) {

                do
                {
                    Packet pkt = CellHeaderBuffer.Dequeue();
                    if (hasSinkPosition)
                    {
                        headerSensor.GenerateQueryResponse(pkt.Source);
                    }
                } while (CellHeaderBuffer.Count > 0);
            }
            
        }

        public void ClearBuffer()
        {
            if (CellHeaderBuffer.Count > 0)
            {
                if (!headerSensor.ClusterHeader.isRootHeader)
                {
                    do
                    {
                        Packet pkt = CellHeaderBuffer.Dequeue();
                      //  pkt.Destination = headerSensor.getQueryDest();
                        pkt.TimeToLive += headerSensor.maxHopsForQuery(headerSensor);
                        headerSensor.SendQRequest(pkt);
                    } while (CellHeaderBuffer.Count > 0);
                   
                }
                
            }
        }
        

    }


    public class ClusterTable
    {

        public bool isContainedIn = false;


        public int nearestClusterID { get; set; }
        public Sensor nearestClusterHeader { get; set; }

        public Sensor myClusterHeader { get; set; }

        public static void fillOutsideSensors()
        {
            foreach (Sensor sen in PublicParameters.myNetwork)
            {
                if (sen.inCluster == -1)
                {
                    //Check the nearest cluster for it
                    double offset = 120;
                    int nearestID = 0;
                    foreach (Cluster cluster in PublicParameters.networkClusters)
                    {
                        double distance = Operations.DistanceBetweenTwoPoints(sen.CenterLocation, cluster.clusterActualCenter);
                        if (distance < offset)
                        {
                            nearestID = cluster.getID();
                            offset = distance;
                        }
                    }
                    sen.ClusterTable = new ClusterTable();
                    sen.ClusterTable.nearestClusterID = nearestID;
                    sen.ClusterTable.nearestClusterHeader = Cluster.getClusterWithID(nearestID).clusterHeader.headerSensor;

                }
            }
        }


        /*  public  void populateClusterTable()
          {
              if (isContainedIn) {
                  inClusterID = inCluster.getID();
                  if (inCluster.parentCluster != null)
                  {
                      parentClusterID = inCluster.parentCluster.getID();
                      inCluster.clusterHeader.parentHeader = Cluster.getClusterWithID(parentClusterID).clusterHeader.headerSensor;
                  }
                  else
                  {
                      isInRoot = true;
                   
                  }
                  if (inCluster.childrenClusters.Count > 0)
                  {
                      foreach (Cluster child in inCluster.childrenClusters)
                      {
                          childrenClustersPos.Add(child.clusterHeadCenterLocation);
                          childrenClustersID.Add(child.getID());
                      }
                  }
                  else
                  {
                      isLeafNode = true;
                  }
              }
              else
              {
                

                
              }
           
            
            



          }*/






    }

}
