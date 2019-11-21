using RingRouting.Constructor;
using RingRouting.Dataplane;
using RingRouting.Dataplane.NOS;
using RingRouting.Dataplane.PacketRouter;
using RingRouting.Intilization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RingRouting.Models.MobileModel;
using RingRouting.Properties;

namespace RingRouting.Models.MobileSink
{
    public class MobileModel
    {
        private static int sinkDirection;
        private static double sinkInterval;
        private static DispatcherTimer timer_move = new DispatcherTimer();
        private static DispatcherTimer timer_changeInter = new DispatcherTimer();
        private static DispatcherTimer timer_changeDir = new DispatcherTimer();
        private Sensor sink = PublicParameters.SinkNode;
   

        public static int rootTreeID { get; set; }

        private static Point oldLocation { get; set; }
        private static Point currentLocation { get; set; }
        private static bool isInsideCuster { get; set; }

        private static Line lineBetweenTwo = new Line();
        private static Canvas sensingField { get; set; }
 

        public static Sensor Agent { get; set; }




        private static Sensor myAgent { get; set; }
        private static bool isOutOfBound = false;





        private double nodeCommRange = PublicParameters.CommunicationRangeRadius;
        //Here we change the sink 
        //First we put the function that's going to be called every tick 


        public static void checkDroppedPacket(Packet packet)
        {

        }

        private static void moveSink(Sensor sink, int direction)
        {
            double maxY = sensingField.ActualHeight;
            double maxX = sensingField.ActualWidth;

            double x = sink.Position.X;
            double y = sink.Position.Y;
            switch (direction)
            {
                case 0:
                    //Do nothing
                    break;
                case 1:
                    //Move to the right
                    x++;
                    break;
                case 2:
                    //Move to the left
                    x--;
                    break;
                case 3:
                    //Move up
                    y--;
                    break;
                case 4:
                    //Move down
                    y++;
                    break;
                case 5:
                    //up right
                    x++;
                    y--;
                    break;
                case 6:
                    //down right
                    x++;
                    y++;
                    break;
                case 7:
                    //left up
                    x--;
                    y--;
                    break;
                case 8:
                    //left down
                    x--;
                    y++;
                    break;
            }

            Point p = new Point(x, y);
            PublicParameters.SinkNode.Position = p;

            sink = PublicParameters.SinkNode;
            currentLocation = sink.CenterLocation;

            sinkInOrOut();
            updateNeighborsTable();
            checkDistanceWithAgent();
        }
        public static void StopSinkMovement()
        {
            timer_move.Stop();
            timer_changeInter.Stop();
            timer_changeDir.Stop();
        }
        public static void setInitialParameters()
        {
            updateNeighborsTable();
            checkDistanceWithAgent();
        }

        private static void checkDistanceWithAgent()
        {
            if (isOutOfBound)
            {
                outOfBound();
            }
            else
            {
                if (myAgent != null)
                {

                    double distance = Operations.DistanceBetweenTwoPoints(PublicParameters.SinkNode.CenterLocation, myAgent.CenterLocation);
                    double offset = (PublicParameters.SinkNode.ComunicationRangeRadius - 5);
                    if (distance > offset)
                    {
                        chooseAgent();
                    }
                }
                else
                {

                    chooseAgent();
                }
            }
            


        }


        private static void updateNeighborsTable()
        {
            PublicParameters.SinkNode.NeighborsTable.Clear();
            double offset = PublicParameters.CommunicationRangeRadius;

            foreach (Sensor sen in PublicParameters.myNetwork)
            {
                if (sen.ID != PublicParameters.SinkNode.ID)
                {
                    double distance = Operations.DistanceBetweenTwoPoints(sen.CenterLocation, PublicParameters.SinkNode.CenterLocation);
                    if (distance <= offset)
                    {
                        NeighborsTableEntry entry = new NeighborsTableEntry();
                        entry.NeiNode = sen;
                        PublicParameters.SinkNode.NeighborsTable.Add(entry);
                    }
                }
            }
            if(PublicParameters.SinkNode.NeighborsTable.Count > 7)
            {
                
            }

            if (PublicParameters.SinkNode.NeighborsTable.Count == 0)
            {
                isOutOfBound = true;
            }
        }


        private static void sendFollowUpToAgent(Sensor oldAgent, Sensor newAgent)
        {          
                if (newAgent.ID == oldAgent.ID)
                {
                    PublicParameters.SinkNode.GenerateANS(oldAgent, newAgent);
                    newAgent.AgentNode = new Agent(PublicParameters.SinkNode, oldAgent, newAgent);
                }
                else
                {
                    PublicParameters.SinkNode.GenerateANS(oldAgent, newAgent);
                    PublicParameters.SinkNode.GenerateOldANS(oldAgent,newAgent);
                    newAgent.AgentNode = new Agent(PublicParameters.SinkNode, oldAgent, newAgent);
                    oldAgent.AgentNode.ChangeAgentFM(newAgent);
                }   
            
        }


        private static Point getDirection(int direction)
        {
            double x = 0;
            double y = 0;
            switch (direction)
            {
                case 0:
                    //Do nothing
                    break;
                case 1:
                    //Move to the right
                    x++;
                    break;
                case 2:
                    //Move to the left
                    x--;
                    break;
                case 3:
                    //Move up
                    y--;
                    break;
                case 4:
                    //Move down
                    y++;
                    break;
                case 5:
                    //right up
                    x++;
                    y--;
                    break;
                case 6:
                    //right down
                    x++;
                    y++;
                    break;
                case 7:
                    //left up
                    x--;
                    y--;
                    break;
                case 8:
                    //left down
                    x--;
                    y++;
                    break;
            }

            return new Point(x, y);

        }

        private static void inBound()
        {
            //Ask for the data after coming back again
        }
        private static bool agentBuffering = false;
        private static void outOfBound()
        {
            if (!agentBuffering)
            {
                //Send a buffer message to agent

                agentBuffering = true;
            }
        }
        private static Sensor groupNeighbors()
        {
            //MobileModel model = new MobileModel();
            Sensor sink = PublicParameters.SinkNode;
            Point currentSinkLocation = sink.CenterLocation;
            double smallest = 100;
            Sensor cand = null;
            Point destination = getDirection(sinkDirection);
            double futureMoves = sinkInterval + 5;
            destination.X *= futureMoves;
            destination.Y *= futureMoves;
            Point futureSinkLocation = new Point(currentSinkLocation.X + destination.X, currentSinkLocation.Y + destination.Y); // If sink is going up the location will decrease 

            if (sink.NeighborsTable.Count == 0)
            {
                //Sink is out of bound send to the CurrentSinkAgent to buffer
                outOfBound();
                cand = null;
                return cand;
            }
            else
            {
               
                foreach (NeighborsTableEntry neiEntry in sink.NeighborsTable)
                {
                    if (neiEntry.NeiNode.ResidualEnergyPercentage > 0)
                    {
                        MiniFlowTableEntry MiniEntry = new MiniFlowTableEntry();
                        MiniEntry.NeighborEntry = neiEntry;
                        MiniEntry.NeighborEntry.E = Operations.DistanceBetweenTwoPoints(futureSinkLocation, MiniEntry.NeighborEntry.CenterLocation);
                        MiniEntry.NeighborEntry.EN = MiniEntry.NeighborEntry.E / sink.ComunicationRangeRadius;
                       // MiniEntry.NeighborEntry.D = Operations.GetDirectionAngle(futureSinkLocation,RootCellHeader, MiniEntry.NeighborEntry.CenterLocation);
                        /*if (currentSinkLocation == futureSinkLocation)
                        {
                            MiniEntry.NeighborEntry.D = 0;
                        }*/
                      //  MiniEntry.NeighborEntry.DN = MiniEntry.NeighborEntry.DN = (MiniEntry.NeighborEntry.D / Math.PI);
                        double estimation = MiniEntry.NeighborEntry.E;// +MiniEntry.NeighborEntry.DN;
                        if (estimation < smallest && !(neiEntry.NeiNode.RingNodesRule.isRingNode))
                        {
                            if (!(MiniEntry.NeighborEntry.NeiNode.ClusterTable.isContainedIn))
                            {
                                smallest = MiniEntry.NeighborEntry.E;
                                cand = MiniEntry.NeighborEntry.NeiNode;
                            }

                        }


                    }
                }
            }

            return cand;
        }

        public static void chooseAgent()
        {
            if (myAgent != null)
            {
                //Changing the agent from to 
                Sensor newAgent = groupNeighbors();
                try
                {
                    sendFollowUpToAgent(myAgent, newAgent);
                    myAgent = newAgent;
                }
                catch
                {
                    newAgent = null;
                    return;
                }
  
            }
            else
            {
                //First time choice
                myAgent = groupNeighbors();
                sendFollowUpToAgent(myAgent, myAgent);

            }



        }


        public static void passField(Canvas sensingFiel)
        {
            sensingField = sensingFiel;
        }

        private static void sinkInOrOut()
        {
            Cluster root = Cluster.getClusterWithID(rootTreeID);
            double distanceWithRoot = Operations.DistanceBetweenTwoPoints(currentLocation, root.clusterActualCenter);
            double radius = PublicParameters.clusterRadius;
            if (distanceWithRoot < (radius / 2))
            {
                isInsideCuster = true;
            }
            else
            {
                isInsideCuster = false;
            }

        }

        //start moving the sink here 

        //Changes the interval between each one pixel move of the sink

        public void startMoving()
        {
            
            RandomeNumberGenerator.SetSeedFromSystemTime();

            timer_changeInter.Interval = TimeSpan.FromSeconds(3);
            timer_changeInter.Start();
            timer_changeInter.Tick += timer_tick_speed;
            //Moves the sink according to its speed and direction
            timer_move.Interval = TimeSpan.FromSeconds(0.5);
            timer_move.Start();
            timer_move.Tick += timer_tick_move;
            //Changes the direction of the sink
            timer_changeDir.Interval = TimeSpan.FromSeconds(2);
            timer_changeDir.Start();
            timer_changeDir.Tick += timer_tick_direction;
        }

   


        

        private void getSinkInterval()
        {
            int MaxSinkSpeed = Settings.Default.SinkSpeed;
            double speedInKmph = RandomeNumberGenerator.uniformMaxSpeed(MaxSinkSpeed);
            sinkInterval = Operations.kmphToTimerInterval(speedInKmph);
            if (sinkInterval > 0)
            {
                timer_move.Interval = TimeSpan.FromSeconds(sinkInterval);
            }
            
        }

        private static int directionMean = 270;
        private static int sinkAngle;
        private static int oldSinkDirection { get; set; }
        private static int oldDirectionMean { get; set; }
        private static bool firstInitialize = false;

        private void changeDirectionMean()
        {
            /*For Border Node 
             * [0] Smallest Y
             * [1] Smallest X
             * [2] Biggest Y
             * [3] Biggest X
             * */
            Point sinkPos = PublicParameters.SinkNode.CenterLocation;

            if (!firstInitialize)
            {
                firstInitialize = true;
                oldSinkDirection = sinkDirection;
            }
            else
            {
                if ((PublicParameters.BorderNodes[3].Position.X - sinkPos.X) < 40)
                {
                    directionMean = 180;
                }
                else if (((PublicParameters.BorderNodes[2].Position.Y - sinkPos.Y) < 40))
                {
                    directionMean = 90;
                }
                else if ((sinkPos.Y - PublicParameters.BorderNodes[0].Position.Y) < 40)
                {
                    directionMean = 270;
                }
                else if (((sinkPos.X - PublicParameters.BorderNodes[1].Position.X) < 40))
                {
                    directionMean = 360;
                }
                 else
                 {
                     if (oldSinkDirection != sinkDirection)
                     {
                         //directionMean = sinkAngle;
                         //oldSinkDirection = sinkDirection;
                     }
                 }
            }



        }


        private void changeDirection()
        {

            sinkAngle = RandomeNumberGenerator.uniformMaxDirection(directionMean);
            sinkDirection = Operations.ConvertAngleToDirection(sinkAngle);
            changeDirectionMean();

        }
        private void moveSink()
        {

            // Console.WriteLine("Direction is: {0}", sinkDirection);
            MobileModel.moveSink(PublicParameters.SinkNode, sinkDirection);

        }

        private void timer_tick_move(Object sender, EventArgs e)
        {

            //this.Dispatcher.Invoke(() => getSinkDirection());

            moveSink();

        }
        private void timer_tick_speed(Object sender, EventArgs e)
        {
            // this.Dispatcher.Invoke(() => getSinkInterval());
            getSinkInterval();
        }
        private void timer_tick_direction(Object sender, EventArgs e)
        {
            changeDirection();
        }
    }
}









