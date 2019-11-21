using RingRouting.Dataplane;
using RingRouting.Dataplane.NOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using RingRouting.Intilization;
using System.Windows;
using RingRouting.Dataplane.PacketRouter;
using System.Windows.Media;

namespace RingRouting.Models.MobileModel
{
    public class Agent
    {
        public Sensor sinkNode { get; set; }
        public Sensor OldAgent { get; set; }

        public Sensor NewAgent { get; set; }
        public Sensor Node { get; set; }
        public Queue<Packet> AgentBuffer { get; set; }
        public DispatcherTimer OldAgentTimer;
        public DispatcherTimer SinkOutOfRangeTimer = new DispatcherTimer();
      
        public bool hasStoredPackets { get { return (AgentBuffer.Count > 0); } }

        public Agent()
        {
            sinkNode = null;
            OldAgent = null;
  
            NewAgent = null;
        }

        public Agent(Sensor sink , Sensor oldagent,Sensor self)
        {
            sinkNode = sink;
            OldAgent = oldagent;

            Node = self;
            if (AgentBuffer == null)
            {
                AgentBuffer = new Queue<Packet>();
            }
            if (SinkOutOfRangeTimer.IsEnabled)
            {
                SinkOutOfRangeTimer.Stop();
            }
            self.isSinkAgent = true;
            NeighborsTableEntry sinkEntry = new NeighborsTableEntry();
            sinkEntry.NeiNode = PublicParameters.SinkNode;
            self.NeighborsTable.Add(sinkEntry);
            self.Ellipse_HeaderAgent_Mark.Stroke = new SolidColorBrush(Colors.Black);
            self.MainWindow.Dispatcher.Invoke(() => self.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Visible);
         
            
        }

        public void initiateNewAgentTimer()
        {
            sinkNode = null;
            OldAgentTimer = new DispatcherTimer();
            OldAgentTimer.Interval = TimeSpan.FromSeconds(15);
           // OldAgentTimer.Start();
            //OldAgentTimer.Tick += NewAgentTimer_Tick;
        }
        private void initiateSinkOutOfRangeTimer()
        {
            if (!SinkOutOfRangeTimer.IsEnabled)
            {
                SinkOutOfRangeTimer.Interval = TimeSpan.FromSeconds(1);
                SinkOutOfRangeTimer.Start();
                SinkOutOfRangeTimer.Tick += OutOfRangeTimer_Tick;
                waitingTimesForSink =0;
            }
         
        }

        private int waitingTimesForSink{get;set;}

        private void OutOfRangeTimer_Tick(object sender, EventArgs e)
        {
            if(waitingTimesForSink > 7){

                Node.isSinkAgent = false;
                PublicParameters.SinkNode.MainWindow.Dispatcher.Invoke(() => Node.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Hidden);
                Node.NeighborsTable.RemoveAll(sinkItem => sinkItem.NeiNode.ID == PublicParameters.SinkNode.ID);
                SinkOutOfRangeTimer.Stop();
                if (AgentBuffer.Count > 0)
                {
                    do
                    {
                        Packet packet = AgentBuffer.Dequeue();
                        packet.isDelivered = false;
                        Node.updateStates(packet);
                    } while (AgentBuffer.Count > 0);
                }

            }else if(AgentBuffer.Count >0){
                if(isSinkInRange()){
                    SinkOutOfRangeTimer.Stop();
                     do
                    {
                        Packet packet = AgentBuffer.Dequeue();
                        packet.Destination = PublicParameters.SinkNode;
                        packet.TimeToLive += PublicParameters.HopsErrorRange;
                        Node.sendDataPack(packet);
                    } while (AgentBuffer.Count > 0);
                }
                else
                {
                    try
                    {
                        if (NewAgent != null)
                        {
                            do
                            {
                                Packet packet = AgentBuffer.Dequeue();
                                packet.Destination = NewAgent;
                                packet.TimeToLive += Node.maxHopsForDestination(NewAgent.CenterLocation);
                                Node.sendDataPack(packet);
                            } while (AgentBuffer.Count > 0);
                            
                        }
                    }
                    catch (NullReferenceException excep)
                    {
                        Console.WriteLine(excep.Message + " new agent is null");
                    }
                   
                }
            }
            else if (isSinkInRange() && AgentBuffer.Count ==0)
            {
                SinkOutOfRangeTimer.Stop();
            }

            waitingTimesForSink++;
         }
            

        public static void checkDroppedPacket(Packet packet){

        }


        public void ChangeAgentFM(Sensor newAgent)
        {
            sinkNode = null;
            OldAgent = null;
            Node.isSinkAgent = false;
            Node.MainWindow.Dispatcher.Invoke(() => Node.Ellipse_HeaderAgent_Mark.Visibility = Visibility.Hidden, DispatcherPriority.Send);
            Node.NeighborsTable.RemoveAll(sinkItem => sinkItem.NeiNode.ID == PublicParameters.SinkNode.ID);
            Node.AgentNode.NewAgent = newAgent;
        }



        private void reSendToOldAgent()
        {
            if (OldAgent == null)
            {
                UnIdentifiedInformation();
            }
            else
            {
                // send a packet to the old agent


            }
        }

        public void UnIdentifiedInformation()
        {
            //Ask the sink for more information 
        }

        public void AgentStorePacket(Packet packet)
        {
           
            initiateSinkOutOfRangeTimer();
            AgentBuffer.Enqueue(packet);
            if (AgentBuffer.Count > 20)
            {
                Console.WriteLine();
            }
        }

        public bool isSinkInRange()
        {
            double distance = Operations.DistanceBetweenTwoSensors(PublicParameters.SinkNode, Node);
            double offset = PublicParameters.CommunicationRangeRadius;
            if (distance >= offset)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void NewAgentTimer_Tick(Object sender, EventArgs e)
        {
            OldAgentTimer.Stop();
            OldAgent = null;
            NewAgent = null;         
            OldAgentTimer = null;
            if (AgentBuffer.Count > 0)
            {
                foreach (Packet packet in AgentBuffer)
                {
                    packet.isDelivered = false;
                    packet.DroppedReason = "New Agent reset";
                    Node.updateStates(packet);
                }
            }
            AgentBuffer.Clear();
        }
       
    }
}
