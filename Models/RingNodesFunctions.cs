using RingRouting.Constructor;
using RingRouting.Dataplane;
using RingRouting.Dataplane.PacketRouter;
using RingRouting.Intilization;
using RingRouting.Models.MobileModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RingRouting.Models
{
    public class RingNodesFunctions
    {
        public Sensor node { get; set; }
        public Sensor myNextHop { get; set; }
        public List<Sensor> potentialCommonNeighbors = new List<Sensor>();
        public Sensor nextRingNode { get; set; }
        public static Sensor AnchorNode { get; set; }

        public RingNodesFunctions()
        {

        }

        public RingNodesFunctions(Sensor me, Sensor nei, bool isNext)
        {
            if (isNext)
            {
                node = me;
                myNextHop = nei;
            }
            else
            {
                node = me;
                nextRingNode = nei;
                myNextHop = null;
            }

        }
       

        private void findMoreCommonNeighbors(Sensor one, Sensor two)
        {
            //Enters this if the two sensors don't share any common neighbors
            //Find two then 
            PublicParameters.PreRingNodesHolder.Add(one);
         
            Sensor CompareOne = one;
            Sensor CompareTwo = two;
      
          //  do
           // {
                Sensor potentialOne = null;
                Sensor potentialTwo = null;
                bool foundOne = false;
                bool foundTwo = false;
                foreach (NeighborsTableEntry entry in CompareOne.NeighborsTable)
                {
                    Sensor candi = entry.NeiNode;
                    double angle = Operations.GetDirectionAngle(CompareOne.CenterLocation, CompareTwo.CenterLocation, candi.CenterLocation);
                    double pirDist = Operations.GetPerpindicularDistance(CompareOne.CenterLocation, CompareTwo.CenterLocation, candi.CenterLocation);
                    double candiDist = Operations.DistanceBetweenTwoPoints(candi.CenterLocation, two.CenterLocation);
                    double oneDist = Operations.DistanceBetweenTwoPoints(one.CenterLocation, two.CenterLocation);
                  // bool isClok = isClockwise2(one.CenterLocation, candi.CenterLocation, two.CenterLocation);
                   bool vice = isClockwise(two.CenterLocation, candi.CenterLocation, one.CenterLocation);
                bool farEnough = (Operations.DistanceBetweenTwoPoints(candi.CenterLocation, PublicParameters.networkCenter) <= PublicParameters.clusterRadius + 10) ;
                if (vice && (candiDist<oneDist && !foundOne) && farEnough)
                    {
                       // max = pirDist;
                        potentialOne = candi;
                        foundOne = true;
                    }
                }

                foreach (NeighborsTableEntry entry in potentialOne.NeighborsTable)
                {
                    Sensor candi = entry.NeiNode;
                    double pirDist = Operations.GetPerpindicularDistance(CompareOne.CenterLocation, CompareTwo.CenterLocation, candi.CenterLocation);
                    double angle = Operations.GetDirectionAngle(CompareOne.CenterLocation, CompareTwo.CenterLocation, candi.CenterLocation);
                    if (foundOne)
                    {
                        if(!foundTwo && Operations.isInMyComunicationRange(two,candi) && isClockwise(two.CenterLocation, candi.CenterLocation, potentialOne.CenterLocation))
                        {
                            potentialTwo = candi;
                            foundTwo = true;
                        }
                    }
                }
                //PublicParameters.PreRingNodesHolder.Add(potentialOne);
               // PublicParameters.PreRingNodesHolder.Add(potentialTwo);
                if (potentialOne == null || potentialTwo == null)
                {
                    //error
                    Console.WriteLine();
                }
                if (foundOne && foundTwo)
                {
                    PublicParameters.PreRingNodesHolder.Add(potentialOne);
                    PublicParameters.PreRingNodesHolder.Add(potentialTwo);

                }
                else
                {
                    findMoreCommonNeighbors(potentialOne, two);
                }
               // if (Operations.isInMyComunicationRange(potentialOne, potentialTwo))
                //{
                 //   found = true;
               // }
                //else
                //{
                 //   CompareOne = potentialOne;
                  //  CompareTwo = potentialTwo;
               // }

         //   } while (!found);
        }

        private void findPottentialNeighbors(Sensor one, Sensor two)
        {

            // They will have common neighbors
            List<Sensor> potentialNeighbors = new List<Sensor>();
            List<Sensor> commonNeighbors = new List<Sensor>();
            bool found = false;
            foreach (NeighborsTableEntry entryS1 in one.NeighborsTable)
            {
                foreach (NeighborsTableEntry entryS2 in two.NeighborsTable)
                {
                    if (entryS1.NeiNode.ID == entryS2.NeiNode.ID)
                    {
                        commonNeighbors.Add(entryS1.NeiNode);
                        found = true;
                    }
                }
            }

            if (!found)
            {

                findMoreCommonNeighbors(one, two);
            }
            else
            {
                findCommonNeighbor(one, two, commonNeighbors);
            }



        }

        private static void populateRingNodes()
        {
            Sensor point;
            Sensor clock;
            Sensor anti;
            RingNodes node;
            int clockwise = 0;
            int antiClockwise = 1;
            int counter = 0;
            for (int i = 0; i <= (PublicParameters.PreRingNodesHolder.Count() - 1); i++)
            {
                counter++;
                if (i == 0)
                {
                    clockwise = PublicParameters.PreRingNodesHolder.Count - 1;
                }
                else if (i == PublicParameters.PreRingNodesHolder.Count - 1)
                {
                    antiClockwise = 0;
                }
                point = PublicParameters.PreRingNodesHolder[i];
                clock = PublicParameters.PreRingNodesHolder[clockwise];
                anti = PublicParameters.PreRingNodesHolder[antiClockwise];
                node = new RingNodes(point, anti, clock);
                point.RingNodesRule = node;
                PublicParameters.RingNodes.Add(node);
                antiClockwise = counter + 1;
                clockwise = counter - 1;
            }

        }

        public static void doLastCheck()
        {
            RingNodesFunctions r = new RingNodesFunctions();
            r.lastCheck();
            populateRingNodes();
            markNeighboringRingNods();
            r.DeterminePositionofAllNodes();
        }

        private void lastCheck()
        {
            Stack<Sensor> ringNodes = new Stack<Sensor>();
            Queue<Sensor> holders = new Queue<Sensor>();
            AnchorNode = Ring.PointZero;

            foreach (Sensor sen in Ring.ConvexNodes)
            {
                ringNodes.Push(sen);
            }
            holders.Enqueue(ringNodes.Pop());
            int counter = 0;
            do
            {
                counter++;
                Sensor one = holders.Dequeue();
                Sensor two = ringNodes.Pop();
                if (one == null || two == null)
                {
                    //Error here
                }
                checkTwoNodes(one, two);
                holders.Enqueue(two);
                if (ringNodes.Count == 0)
                {
                    one = holders.Dequeue();
                    two = AnchorNode;
                    if (one == null || two == null)
                    {
                        // Error here
                    }
                    checkTwoNodes(one, two);
                }
            } while (ringNodes.Count > 0);

        }

        private void checkTwoNodes(Sensor one, Sensor two)
        {
            if (Operations.isInMyComunicationRange(one, two))
            {
                PublicParameters.PreRingNodesHolder.Add(one);
            }
            else
            {
                findPottentialNeighbors(one, two);
            }
        }
        private void findCommonNeighbor(Sensor one, Sensor two, List<Sensor> Common)
        {
            double max = 100;
            Sensor holder = null;
            foreach (Sensor candi in Common)
            {
                double pirDist = Operations.GetPerpindicularDistance(one.CenterLocation, two.CenterLocation, candi.CenterLocation);
                if (pirDist < max)
                {
                    max = pirDist;
                    holder = candi;
                }
            }
            PublicParameters.PreRingNodesHolder.Add(one);
            PublicParameters.PreRingNodesHolder.Add(holder);
        }

        private static List<int> RingNodeNeighborsIDS = new List<int>();
        private static void markNeighboringRingNods()
        {
            foreach (RingNodes ringNode in PublicParameters.RingNodes)
            {
                foreach (NeighborsTableEntry entry in ringNode.Node.NeighborsTable)
                {

                    Sensor nei = entry.NeiNode;
                    if (!nei.RingNodesRule.isRingNode)
                    {
                        nei.RingNeighborRule.isNeighbor = true;
                        nei.RingNeighborRule.NeighboringRingNodes.Add(ringNode.Node);
                        if (!RingNodeNeighborsIDS.Contains(nei.ID))
                        {
                            RingNodeNeighborsIDS.Add(nei.ID);
                        }
                    }

                }
            }

            foreach (int id in RingNodeNeighborsIDS)
            {
                PublicParameters.myNetwork[id].RingNeighborRule.getNearestRingNode();
            }
        }


        public void DeterminePositionofAllNodes()
        {
            List<Sensor> polygon = new List<Sensor>();
            List<Sensor> nodesInside = new List<Sensor>();
            foreach (RingNodes verticy in PublicParameters.RingNodes)
            {
                polygon.Add(verticy.Node);
            }

            foreach (Sensor sensor in PublicParameters.myNetwork)
            {
                if (!sensor.RingNodesRule.isRingNode && sensor.ID != PublicParameters.SinkNode.ID)
                {
                    bool isInside = Operations.PointInPolygon(polygon, sensor);
                    sensor.isInsideRing = isInside;
                    if (isInside)
                    {
                        nodesInside.Add(sensor);

                        sensor.isInsideRing = true;
                    }
                }
            }
        }




        #region RingNode Change
        private static bool isClockwise(Point a, Point b, Point c)
        {


            double value = ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));

            if (value >= 0)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        private static void ClearOldRingNodeData(RingNodes node,RingNodes newNode)
        {
            Sensor sen = node.Node;
            sen.RingNodesRule = new RingNodes();
            foreach (NeighborsTableEntry neighbor in node.Node.NeighborsTable)
            {
                neighbor.NeiNode.RingNeighborRule = new RingNeighbor(neighbor.NeiNode);
            }
          //  AddRemoveArrows(node, newNode);

        }

        private static void AddRemoveArrows(RingNodes oldNode, RingNodes newNode)
        {
            Sensor newSen = newNode.Node;
            Sensor oldSen = oldNode.Node;
            Sensor clockSen = oldNode.ClockWiseNeighbor;
            Sensor antiClockSen = oldNode.AntiClockWiseNeighbor;
            Ring.removeOldLine(clockSen,oldSen);
            Ring.removeOldLine(oldSen,antiClockSen);

            Ring.addNewLine(newSen,antiClockSen);
            Ring.addNewLine(clockSen, newSen);
        }
       /* private static void AddRemoveArrows(RingNodes oldNode, RingNodes newNode)
        {
            Sensor oldSen = oldNode.Node;
            Sensor newSen = newNode.Node;

            Sensor clockWise = oldNode.ClockWiseNeighbor;
            Sensor AnticlockWise = oldNode.AntiClockWiseNeighbor;

            Arrow fromClockToNode = clockWise.MyArrows.Where(i => i.To.ID == oldSen.ID).First();

            int IndexForClock = clockWise.MyArrows.IndexOf(fromClockToNode);
            if (IndexForClock != -1)
            {
                clockWise.MyArrows[IndexForClock].Stroke = Brushes.Gray;
                clockWise.MyArrows[IndexForClock].StrokeThickness = 0.2;
                clockWise.MyArrows[IndexForClock].HeadHeight = 0.2;
                clockWise.MyArrows[IndexForClock].HeadWidth = 0.2;
            }
    
            Arrow fromNodetoAntiClock = oldSen.MyArrows.Where(i => i.To.ID == AnticlockWise.ID).First();
            int IndexForOld = oldSen.MyArrows.IndexOf(fromNodetoAntiClock);
            if (IndexForOld != -1)
            {
                oldSen.MyArrows[IndexForOld].Stroke = Brushes.Gray;
                oldSen.MyArrows[IndexForOld].StrokeThickness = 0.2;
                oldSen.MyArrows[IndexForOld].HeadHeight = 0.2;
                oldSen.MyArrows[IndexForOld].HeadWidth = 0.2;
            }
            //---------

            Arrow fromClockToNNode;
            Arrow fromNNodetoAntiClock;

            try
            {
                fromClockToNNode = clockWise.MyArrows.Where(i => i.To.ID == newSen.ID).First();
                int IndexForNAnti = clockWise.MyArrows.IndexOf(fromClockToNNode);
                if (IndexForNAnti != -1)
                {
                    clockWise.MyArrows[IndexForNAnti].Stroke = Brushes.Black;
                    clockWise.MyArrows[IndexForNAnti].StrokeThickness = 1;
                    clockWise.MyArrows[IndexForNAnti].HeadHeight = 0.2;
                    clockWise.MyArrows[IndexForNAnti].HeadWidth = 0.2;
                }
            }
            catch
            {
                fromClockToNNode = null;
                Ring.addNewArrow(clockWise, newSen);
            }

            try
            {
                fromNNodetoAntiClock = newSen.MyArrows.Where(i => i.To.ID == AnticlockWise.ID).First();
                int IndexForNOld = newSen.MyArrows.IndexOf(fromNNodetoAntiClock);
                if (IndexForNOld != -1)
                {
                    newSen.MyArrows[IndexForNOld].Stroke = Brushes.Black;
                    newSen.MyArrows[IndexForNOld].StrokeThickness = 1;
                    newSen.MyArrows[IndexForNOld].HeadHeight = 0.2;
                    newSen.MyArrows[IndexForNOld].HeadWidth = 0.2;
                }
            }
            catch
            {
                fromNNodetoAntiClock = null;
                Ring.addNewArrow(newSen, AnticlockWise);
            }
            


        }
        */

        private static Sensor FindNewRingNodeReplacement(RingNodes RN)
        {
            if (!RN.Node.isExpanding)
            {
                ShouldNodeExpand(RN.Node);
            }
            Sensor cand = null;
            GetTheSetofClosestNodes(RN);
            for(int i=0; i < 4; i++)
            {
                if (!RN.Node.isExpanding)
                {
                    i = 1;
                    cand = GetNewRingNode(RN, i);
                    return cand;
                }
               
                cand = GetNewRingNode(RN, i);
                if(cand != null)
                {
                    return cand;
                }
            }
            return cand;
        }
        private static void ShouldNodeExpand(Sensor node){

            double distanceToCenter = Operations.DistanceBetweenTwoPoints(node.CenterLocation, PublicParameters.networkCenter);
            if (distanceToCenter < (PublicParameters.clusterRadius))
            {
                node.isExpanding = true;
            }

        }

        private static List<RingNodeCandidates> ClosestNodestoRN = new List<RingNodeCandidates>();

        private static void GetTheSetofClosestNodes(RingNodes RN){
            ClosestNodestoRN.Clear();
            List<RingNodeCandidates> holder = new List<RingNodeCandidates>();
            foreach (NeighborsTableEntry nei in RN.Node.NeighborsTable)
            {
                double distance = Operations.DistanceBetweenTwoSensors(nei.NeiNode, RN.AntiClockWiseNeighbor);
                distance += Operations.DistanceBetweenTwoSensors(nei.NeiNode, RN.ClockWiseNeighbor);
                RingNodeCandidates cand = new RingNodeCandidates(nei.NeiNode, distance);
                holder.Add(cand);
            }
            ClosestNodestoRN = holder.OrderBy(i => i.Distance).ToList();
        }



        private static Sensor GetNewRingNode(RingNodes RN,int i)
        {
            Sensor clockWiseNei = RN.ClockWiseNeighbor;
            Sensor AntiClockWiseNei = RN.AntiClockWiseNeighbor;
            Sensor candi = null;
            double oldDistance = Operations.DistanceBetweenTwoPoints(RN.Node.CenterLocation, PublicParameters.networkCenter);
            foreach (RingNodeCandidates nei in ClosestNodestoRN)
            {
                double candiDistance = Operations.DistanceBetweenTwoPoints(nei.Node.CenterLocation, PublicParameters.networkCenter);
                bool conditionOne = (Operations.Orientation(AntiClockWiseNei.CenterLocation, nei.Node.CenterLocation,clockWiseNei.CenterLocation) > 1) ;
                //int  orientMe = (Operations.Orientation(RN.AntiClockWiseNeighbor.CenterLocation, RN.Node.CenterLocation, RN.ClockWiseNeighbor.CenterLocation));
                 int orientCandi = (Operations.Orientation(AntiClockWiseNei.CenterLocation, nei.Node.CenterLocation, clockWiseNei.CenterLocation));
                bool conditionTwo;//= false;
                bool notRingNode = (!nei.Node.RingNodesRule.isRingNode);
                bool notAgentNode = (!nei.Node.isSinkAgent);
                if (i ==0)
                {
                    conditionTwo = (candiDistance) > (oldDistance);
                }
                else if(i==1)
                {
                    conditionOne = true;
                    conditionTwo = (candiDistance) < (oldDistance);
                }else if (i == 2)
                {
                    conditionTwo = (candiDistance) > (oldDistance);
                    conditionOne = !(nei.Node.RingNodesRule.isRingNode);
                }else
                {
                    conditionTwo = true;
                }

                if (conditionOne && conditionTwo && notRingNode && notAgentNode)
                {
                    candi = nei.Node;
                    if (i == 1)
                    {
                        candi.isExpanding = false;
                    }
                    
                    return candi;
                }
            }


            return candi;
        }


       
        private static void CheckNewRingNode(RingNodes newNode)
        {
            Sensor me = newNode.Node;
            Sensor clock = newNode.ClockWiseNeighbor;
            Sensor antiClock = newNode.AntiClockWiseNeighbor;
            if (!Operations.isInMyComunicationRange(me, clock))
            {

            }
            if (!Operations.isInMyComunicationRange(me, antiClock))
            {

            }
        }
        private static void ReplaceTwoNodes(RingNodes currentRN , RingNodes b)
        {
            currentRN.AntiClockWiseNeighbor.RingNodesRule.ClockWiseNeighbor = b.Node;
            currentRN.ClockWiseNeighbor.RingNodesRule.AntiClockWiseNeighbor = b.Node;
            try
            {
                RingNodes bring = PublicParameters.RingNodes.Where(i => i.Node.ID == currentRN.Node.ID).First();
                int index = PublicParameters.RingNodes.IndexOf(bring);
                if (index != -1)
                {
                    PublicParameters.RingNodes[index] = b;
                }
            }
            catch
            {
                Console.WriteLine("Replace Two Nodes returned exception");
            }
            
            
        }


      

        public static void ChangeRingNode(RingNodes CurrentRN, bool isExpanding)
        {
            //First we find a new node (Expand or Collapse we start with expand)
            //Change this node with the other and change the clockwise and anticlockwise
            //Determine poisition of nodes
            //Sensor oldRingNode = CurrentRN.Node;
            Sensor newRingNode = FindNewRingNodeReplacement(CurrentRN);
            Sensor clock = CurrentRN.ClockWiseNeighbor;
            Sensor anti = CurrentRN.AntiClockWiseNeighbor;
            if (newRingNode != null)
            {
                //me,anti,clock
                RingNodes newNo = new RingNodes(newRingNode, CurrentRN.AntiClockWiseNeighbor, CurrentRN.ClockWiseNeighbor);
                newNo.AnchorNode = CurrentRN.AnchorNode;
                newRingNode.RingNodesRule = newNo;
                ReplaceTwoNodes(CurrentRN, newNo);
                ClearOldRingNodeData(CurrentRN,newNo);
                AddRemoveArrows(CurrentRN, newNo);
                markNeighboringRingNods();
                RingNodesFunctions r = new RingNodesFunctions();
                r.DeterminePositionofAllNodes();
                Console.WriteLine("Change {0} to {1}",CurrentRN.Node.ID,newRingNode.ID);

            }
            else
            {
                Console.WriteLine("Failed to change node {0}", CurrentRN.Node.ID);
                //Failed
            }
        }

    }
        #endregion

}
