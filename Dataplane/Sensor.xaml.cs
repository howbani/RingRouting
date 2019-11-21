using RingRouting.Intilization;
using RingRouting.Energy;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RingRouting.ui;
using RingRouting.Properties;
using System.Windows.Threading;
using System.Threading;
using RingRouting.ControlPlane.NOS;
using RingRouting.ui.conts;
using RingRouting.ControlPlane.NOS.FlowEngin;
using RingRouting.Forwarding;
using RingRouting.Dataplane.PacketRouter;
using RingRouting.Dataplane.NOS;
using RingRouting.Models.MobileSink;
using RingRouting.Constructor;
using System.Diagnostics;
using RingRouting.Models.MobileModel;
using RingRouting.Models.Energy;
using RingRouting.Models;

namespace RingRouting.Dataplane
{
    public enum SensorState { initalized, Active, Sleep } // defualt is not used. i 
    public enum EnergyConsumption { Transmit, Recive } // defualt is not used. i 


    /// <summary>
    /// Interaction logic for Node.xaml
    /// </summary>
    public partial class Sensor : UserControl
    {
        #region Common parameters.
        
        public Radar Myradar; 
        public List<Arrow> MyArrows = new List<Arrow>();
        public MainWindow MainWindow { get; set; } // the mian window where the sensor deployed.
        public static double SR { get; set; } // the radios of SENSING range.
        public double SensingRangeRadius { get { return SR; } }
        public static double CR { get; set; }  // the radios of COMUNICATION range. double OF SENSING RANGE
        public double ComunicationRangeRadius { get { return CR; } }
        public double BatteryIntialEnergy; // jouls // value will not be changed
        private double _ResidualEnergy; //// jouls this value will be changed according to useage of battery
        public List<int> DutyCycleString = new List<int>(); // return the first letter of each state.
        public BoXMAC Mac { get; set; } // the mac protocol for the node.
        public SensorState CurrentSensorState { get; set; } // state of node.
        public List<RoutingLog> Logs = new List<RoutingLog>();
        public List<NeighborsTableEntry> NeighborsTable = null; // neighboring table.
        public List<MiniFlowTableEntry> MiniFlowTable = new List<MiniFlowTableEntry>(); //flow table.
     
        public int NumberofPacketsGeneratedByMe = 0; // the number of packets sent by this packet.
        public FirstOrderRadioModel EnergyModel = new FirstOrderRadioModel(); // energy model.
        public int ID { get; set; } // the ID of sensor.
        private BatteryLevelThresh BT = new BatteryLevelThresh();
        public bool trun { get; set; }// this will be true if the node is already sent the beacon packet for discovering the number of hops to the sink.
        private DispatcherTimer SendPacketTimer = new DispatcherTimer();// 
        private DispatcherTimer QueuTimer = new DispatcherTimer();// to check the packets in the queue right now.
        public Queue<Packet> WaitingPacketsQueue = new Queue<Packet>(); // packets queue.
        public DispatcherTimer OldAgentTimer = new DispatcherTimer();
        public List<BatRange> BatRangesList = new List<Energy.BatRange>();

        //RingRouting Parameters
        public RingNodes RingNodesRule = new RingNodes(); // Ring Nodes Rule
        public RingNeighbor RingNeighborRule { get; set; } // Neighboring Nodes of the Ring Nodes
        public Point NetworkCenter { get; set; }
        public bool isInsideRing { get; set; } //will be null for ring nodes like that 
        public bool isExpanding = true;
        public int inCluster = -1;
        public ClusterTable ClusterTable = new ClusterTable();
        public ClusterHeaderTable ClusterHeader { get; set; }
        public Agent AgentNode = new Agent();
        public bool isSinkAgent = false;
        public Sensor SinkAdversary { get; set; }
        public Point SinkPosition { get; set; }
        public bool CanRecievePacket { get { return ((this.WaitingPacketsQueue.Count+agentBufferCount) < (PublicParameters.BufferSize - 2)); } }
        private Stopwatch QueryDelayStopwatch { get; set; }
        private int agentBufferCount { get {
            if (this.isSinkAgent)
            {
                return this.AgentNode.AgentBuffer.Count;
            }
            else
            {
                return 0;
            }
            } }
    
        public double CellHeaderProbability { get; set; }

        public Stopwatch DelayStopWatch = new Stopwatch(); 
        /// <summary>
        /// CONFROM FROM NANO NO JOUL
        /// </summary>
        /// <param name="UsedEnergy_Nanojoule"></param>
        /// <returns></returns>
        public double ConvertToJoule(double UsedEnergy_Nanojoule) //the energy used for current operation
        {
            double _e9 = 1000000000; // 1*e^-9
            double _ONE = 1;
            double oNE_DIVIDE_e9 = _ONE / _e9;
            double re = UsedEnergy_Nanojoule * oNE_DIVIDE_e9;
            return re;
        }

        /// <summary>
        /// in JOULE
        /// </summary>
        public double ResidualEnergy // jouls this value will be changed according to useage of battery
        {
            get { return _ResidualEnergy; }
            set
            {
                _ResidualEnergy = value;
                Prog_batteryCapacityNotation.Value = _ResidualEnergy;
            }
        } //@unit(JOULS);


        /// <summary>
        /// 0%-100%
        /// </summary>
        public double ResidualEnergyPercentage
        {
            get { return (ResidualEnergy / BatteryIntialEnergy) * 100; }
        }
        /// <summary>
        /// visualized sensing range and comuinication range
        /// </summary>
        public double VisualizedRadius
        {
            get { return Ellipse_Sensing_range.Width / 2; }
            set
            {
                // sensing range:
                Ellipse_Sensing_range.Height = value * 2; // heigh= sen rad*2;
                Ellipse_Sensing_range.Width = value * 2; // Width= sen rad*2;
                SR = VisualizedRadius;
                CR = SR * 2; // comunication rad= sensing rad *2;

                // device:
                Device_Sensor.Width = value * 4; // device = sen rad*4;
                Device_Sensor.Height = value * 4;
                // communication range
                Ellipse_Communication_range.Height = value * 4; // com rang= sen rad *4;
                Ellipse_Communication_range.Width = value * 4;

                // battery:
                Prog_batteryCapacityNotation.Width = 8;
                Prog_batteryCapacityNotation.Height = 2;
            }
        }

        /// <summary>
        /// Real postion of object.
        /// </summary>
        public Point Position
        {
            get
            {
                double x = Device_Sensor.Margin.Left;
                double y = Device_Sensor.Margin.Top;
                Point p = new Point(x, y);
                return p;
            }
            set
            {
                Point p = value;
                Device_Sensor.Margin = new Thickness(p.X, p.Y, 0, 0);
            }
        }

        /// <summary>
        /// center location of node.
        /// </summary>
        public Point CenterLocation
        {
            get
            {
                double x = Device_Sensor.Margin.Left;
                double y = Device_Sensor.Margin.Top;
                Point p = new Point(x + CR, y + CR);
                return p;
            }
        }

        bool StartMove = false; // mouse start move.
        private void Device_Sensor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    System.Windows.Point P = e.GetPosition(MainWindow.Canvas_SensingFeild);
                    P.X = P.X - CR;
                    P.Y = P.Y - CR;
                    Position = P;
                    StartMove = true;
                }
            }
        }

        private void Device_Sensor_MouseMove(object sender, MouseEventArgs e)
        {
            if (Settings.Default.IsIntialized == false)
            {
                if (StartMove)
                {
                    System.Windows.Point P = e.GetPosition(MainWindow.Canvas_SensingFeild);
                    P.X = P.X - CR;
                    P.Y = P.Y - CR;
                    this.Position = P;
                }
            }
        }

        private void Device_Sensor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StartMove = false;
        }

        private void Prog_batteryCapacityNotation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
            double val = ResidualEnergyPercentage;
            if (val <= 0)
            {
                MainWindow.RandomSelectSourceNodesTimer.Stop();
                
                // dead certificate:
                ExpermentsResults.Lifetime.DeadNodesRecord recod = new ExpermentsResults.Lifetime.DeadNodesRecord();
                recod.DeadAfterPackets = PublicParameters.NumberofGeneratedDataPackets;
                recod.DeadOrder = PublicParameters.DeadNodeList.Count + 1;
                recod.Rounds = PublicParameters.Rounds + 1;
                recod.DeadNodeID = ID;
                recod.NOS = PublicParameters.NOS;
                recod.NOP = PublicParameters.NOP;
                PublicParameters.DeadNodeList.Add(recod);

                Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));
                Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0));


                if (Settings.Default.StopeWhenFirstNodeDeid)
                {
                    MainWindow.TimerCounter.Stop();
                    MainWindow.RandomSelectSourceNodesTimer.Stop();
                    MainWindow.stopSimlationWhen = PublicParameters.SimulationTime;
                    MainWindow.top_menu.IsEnabled = true;
                    MobileModel.StopSinkMovement();
                }
                Mac.SwichToSleep();
                Mac.SwichOnTimer.Stop();
                Mac.ActiveSleepTimer.Stop();
                foreach (Sensor sen in PublicParameters.myNetwork)
                {
                    if (sen.WaitingPacketsQueue.Count > 0)
                    {
                        while (sen.WaitingPacketsQueue.Count > 0)
                        {
                            Packet pkt = sen.WaitingPacketsQueue.Dequeue();
                            pkt.isDelivered = false;
                            pkt.DroppedReason = "DeadNode";
                            updateStates(pkt);
                        }
                    }
                }
                    if (this.ResidualEnergy <= 0)
                {
                    while (this.WaitingPacketsQueue.Count > 0)
                    {
                        PublicParameters.NumberofDropedPackets += 1;
                        Packet pack = WaitingPacketsQueue.Dequeue();
                        pack.isDelivered = false;
                       // PublicParameters.FinishedRoutedPackets.Add(pack);
                        Console.WriteLine("PID:" + pack.PID + " has been droped.");
                        MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);

                    }
                    this.QueuTimer.Stop();
                    if (Settings.Default.ShowRadar) Myradar.StopRadio();
                    QueuTimer.Stop();
                    Console.WriteLine("NID:" + this.ID + ". Queu Timer is stoped.");
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                    MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);

                    return;
                }
                return;


            }
            if (val >= 1 && val <= 9)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
               Dispatcher.Invoke(()=> Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
            }

            if (val >= 10 && val <= 19)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col10_19)));
            }

            if (val >= 20 && val <= 29)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29)));
                Dispatcher.Invoke(() => Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col20_29))));
            }

            // full:
            if (val >= 30 && val <= 39)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col30_39)));
            }
            // full:
            if (val >= 40 && val <= 49)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col40_49)));
            }
            // full:
            if (val >= 50 && val <= 59)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col50_59)));
            }
            // full:
            if (val >= 60 && val <= 69)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col60_69)));
            }
            // full:
            if (val >= 70 && val <= 79)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col70_79)));
            }
            // full:
            if (val >= 80 && val <= 89)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col80_89)));
            }
            // full:
            if (val >= 90 && val <= 100)
            {
                Dispatcher.Invoke(() => Prog_batteryCapacityNotation.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100)));
                Dispatcher.Invoke(() => Ellipse_battryIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col90_100)));
            }


            /*
            // update the battery distrubtion.
            int battper = Convert.ToInt16(val);
            if (battper > PublicParamerters.UpdateLossPercentage)
            {
                int rangeIndex = battper / PublicParamerters.UpdateLossPercentage;
                if (rangeIndex >= 1)
                {
                    if (BatRangesList.Count > 0)
                    {
                        BatRange range = BatRangesList[rangeIndex - 1];
                        if (battper >= range.Rang[0] && battper <= range.Rang[1])
                        {
                            if (range.isUpdated == false)
                            {
                                range.isUpdated = true;
                                // update the uplink.
                                UplinkRouting.UpdateUplinkFlowEnery(this,);

                            }
                        }
                    }
                }
            }*/
        }


        /// <summary>
        /// show or hide the arrow in seperated thread.
        /// </summary>
        /// <param name="id"></param>
        public void ShowOrHideArrow(int id) 
        {
            Thread thread = new Thread(() =>
            {
                lock (MyArrows)
                {
                    Arrow ar = GetArrow(id);
                    if (ar != null)
                    {
                        lock (ar)
                        {
                            if (ar.Visibility == Visibility.Visible)
                            {
                                Action action = () => ar.Visibility = Visibility.Hidden;
                                Dispatcher.Invoke(action);
                            }
                            else
                            {
                                Action action = () => ar.Visibility = Visibility.Visible;
                                Dispatcher.Invoke(action);
                            }
                        }
                    }
                }
            }
            );
            thread.Start();
        }


        // get arrow by ID.
        private Arrow GetArrow(int EndPointID)
        {
            foreach (Arrow arr in MyArrows) { if (arr.To.ID == EndPointID) return arr; }
            return null;
        }



       

        #endregion



       
       

        /// <summary>
        /// 
        /// </summary>
        public void SwichToActive()
        {
            Mac.SwichToActive();

        }

        /// <summary>
        /// 
        /// </summary>
        private void SwichToSleep()
        {
            Mac.SwichToSleep();
        }

       
        public Sensor(int nodeID)
        {
            InitializeComponent();
            //: sink is diffrent:
            if (nodeID == 0) BatteryIntialEnergy = PublicParameters.BatteryIntialEnergyForSink; // the value will not be change
            else
                BatteryIntialEnergy = PublicParameters.BatteryIntialEnergy;
           
            
            ResidualEnergy = BatteryIntialEnergy;// joules. intializing.
            Prog_batteryCapacityNotation.Value = BatteryIntialEnergy;
            Prog_batteryCapacityNotation.Maximum = BatteryIntialEnergy;
            lbl_Sensing_ID.Content = nodeID;
            ID = nodeID;
            QueuTimer.Interval = PublicParameters.QueueTime;
            QueuTimer.Tick += DeliveerPacketsInQueuTimer_Tick;
            OldAgentTimer.Interval = TimeSpan.FromSeconds(3);
            OldAgentTimer.Tick += RemoveOldAgentTimer_Tick;
            //:

            SendPacketTimer.Interval = TimeSpan.FromSeconds(1);
           

        }

        private void QueryDelayStartStop(bool isFinished)
        {
            if (!isFinished)
            {
                this.QueryDelayStopwatch = new Stopwatch();
                this.QueryDelayStopwatch.Start();
            }
            else
            {
                this.QueryDelayStopwatch.Stop();
                PublicParameters.QueryDelay += this.QueryDelayStopwatch.Elapsed.TotalSeconds;
            }
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            

        }

        /// <summary>
        /// hide all arrows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            /*
            Vertex ver = MainWindow.MyGraph[ID];
            foreach(Vertex v in ver.Candidates)
            {
                MainWindow.myNetWork[v.ID].lbl_Sensing_ID.Background = Brushes.Black;
            }*/
         
        }

        

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           
        }

       

        public int ComputeMaxHopsUplink
        {
            get
            {
                double  DIS= Operations.DistanceBetweenTwoSensors(PublicParameters.SinkNode, this);
                return Convert.ToInt16(Math.Ceiling((Math.Sqrt(PublicParameters.Density) * (DIS / ComunicationRangeRadius))));
            }
        }

        public int ComputeMaxHopsDownlink(Sensor endNode)
        {
            double DIS = Operations.DistanceBetweenTwoSensors(PublicParameters.SinkNode, endNode);
            return Convert.ToInt16(Math.Ceiling((Math.Sqrt(PublicParameters.Density) * (DIS / ComunicationRangeRadius))));
        }

        #region Old Sending Data ///
      
        /// <summary>
        ///  data or control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reciver"></param>
        /// <param name="packt"></param>
        
       
        
        public void IdentifySourceNode(Sensor source)
        {
            if (Settings.Default.ShowAnimation && source.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => source.Ellipse_indicator.Visibility = Visibility.Visible;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => source.Ellipse_indicator.Fill = Brushes.Yellow;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void UnIdentifySourceNode(Sensor source)
        {
            if (Settings.Default.ShowAnimation && source.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => source.Ellipse_indicator.Visibility = Visibility.Hidden;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => source.Ellipse_indicator.Fill = Brushes.Transparent;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void GenerateDataPacket()
        {
            if (Settings.Default.IsIntialized && this.ResidualEnergy > 0)
            {
                this.DissemenateData();

            }
        }

        public void GenerateMultipleDataPackets(int numOfPackets)
        {
            for (int i = 0; i < numOfPackets; i++)
            {
                GenerateDataPacket();
                //  Thread.Sleep(50);
            }
        }

        public void GenerateControlPacket(Sensor endNode)
        {
            if (Settings.Default.IsIntialized && this.ResidualEnergy > 0)
            {

                

            }
        }
        /// <summary>
        /// to the same endnode.
        /// </summary>
        /// <param name="numOfPackets"></param>
        /// <param name="endone"></param>
        public void GenerateMultipleControlPackets(int numOfPackets, Sensor endone)
        {
            for (int i = 0; i < numOfPackets; i++)
            {
                GenerateControlPacket(endone);
            }
        }

        public void IdentifyEndNode(Sensor endNode)
        {
            if (Settings.Default.ShowAnimation && endNode.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => endNode.Ellipse_indicator.Visibility = Visibility.Visible;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => endNode.Ellipse_indicator.Fill = Brushes.DarkOrange;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void UnIdentifyEndNode(Sensor endNode)
        {
            if (Settings.Default.ShowAnimation && endNode.ID != PublicParameters.SinkNode.ID)
            {
                Action actionx = () => endNode.Ellipse_indicator.Visibility = Visibility.Hidden;
                Dispatcher.Invoke(actionx);

                Action actionxx = () => endNode.Ellipse_indicator.Fill = Brushes.Transparent;
                Dispatcher.Invoke(actionxx);
            }
        }

        public void btn_send_packet_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lbl_title = sender as Label;
            switch (lbl_title.Name)
            {
                case "btn_send_1_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(1);
                        }
                        else
                        {
                            RandomSelectEndNodes(1);
                        }

                        break;
                    }
                case "btn_send_10_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(10);
                        }
                        else
                        {
                            RandomSelectEndNodes(10);
                        }
                        break;
                    }

                case "btn_send_100_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(100);
                        }
                        else
                        {
                            RandomSelectEndNodes(100);
                        }
                        break;
                    }

                case "btn_send_300_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(300);
                        }
                        else
                        {
                            RandomSelectEndNodes(300);
                        }
                        break;
                    }

                case "btn_send_1000_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(1000);
                        }
                        else
                        {
                            RandomSelectEndNodes(1000);
                        }
                        break;
                    }

                case "btn_send_5000_packet":
                    {
                        if (this.ID != PublicParameters.SinkNode.ID)
                        {
                            // uplink:
                            GenerateMultipleDataPackets(5000);
                        }
                        else
                        {
                            // DOWN
                            RandomSelectEndNodes(5000);
                        }
                        break;
                    }
            }
        }

        private void OpenChanel(int reciverID, long PID)
        {
            Thread thread = new Thread(() =>
            {
                lock (MyArrows)
                {
                    Arrow ar = GetArrow(reciverID);
                    if (ar != null)
                    {
                        lock (ar)
                        {
                            if (ar.Visibility == Visibility.Hidden)
                            {
                                if (Settings.Default.ShowAnimation)
                                {
                                    Action actionx = () => ar.BeginAnimation(PID);
                                    Dispatcher.Invoke(actionx);
                                    Action action1 = () => ar.Visibility = Visibility.Visible;
                                    Dispatcher.Invoke(action1);
                                }
                                else
                                {
                                    Action action1 = () => ar.Visibility = Visibility.Visible;
                                    Dispatcher.Invoke(action1);
                                    Dispatcher.Invoke(() => ar.Stroke = new SolidColorBrush(Colors.Black));
                                    Dispatcher.Invoke(() => ar.StrokeThickness = 1);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                            }
                            else
                            {
                                if (Settings.Default.ShowAnimation)
                                {
                                    int cid = Convert.ToInt16(PID % PublicParameters.RandomColors.Count);
                                    Action actionx = () => ar.BeginAnimation(PID);
                                    Dispatcher.Invoke(actionx);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                                else
                                {
                                    Dispatcher.Invoke(() => ar.Stroke = new SolidColorBrush(Colors.Black));
                                    Dispatcher.Invoke(() => ar.StrokeThickness = 1);
                                    Dispatcher.Invoke(() => ar.HeadHeight = 1);
                                    Dispatcher.Invoke(() => ar.HeadWidth = 1);
                                }
                            }
                        }
                    }
                }
            }
           );
            thread.Start();
            thread.Priority = ThreadPriority.Highest;
        }

        #endregion


        #region send data: /////////////////////////////////////////////////////////////////////////////

        public Point getDestinationForRingAccess()
        {
            Point destination = new Point();
            if (this.isInsideRing)
            {
                //send to the opposite side direction of the network center
                destination = Operations.GetDirectionToRingNodes(this);
            }
            else
            {
                destination = PublicParameters.networkCenter;
            }
            return destination;
        }
        public Sensor getQueryDestination()
        {
            return null;
        }

        public int maxHopsForDestination(Point destination)
        {
            
            try
            {
                double DIS = Operations.DistanceBetweenTwoPoints(destination, this.CenterLocation);
                return PublicParameters.HopsErrorRange + Convert.ToInt16(Math.Ceiling(((PublicParameters.Density / 2) * (DIS / ComunicationRangeRadius))));
            }
            catch(NullReferenceException e)
            {
                Console.WriteLine(e.Message + " destination node in max hops is null");
                return 0;
            }
           
        }

        public int maxHopsForQuery(Sensor sourceNode)
        {
            double DIS;
            if (sourceNode.isInsideRing)
            {
                DIS = PublicParameters.clusterRadius + (PublicParameters.clusterRadius * 0.5);
                return Convert.ToInt16(Math.Ceiling(((PublicParameters.Density / 2) * (DIS / ComunicationRangeRadius)))) + PublicParameters.HopsErrorRange;
            }
            else
            {
                DIS = Operations.DistanceBetweenTwoPoints(sourceNode.CenterLocation, PublicParameters.networkCenter);
                return Convert.ToInt16(Math.Ceiling(((PublicParameters.Density / 2) * (DIS / ComunicationRangeRadius))));
            }
        
        }

        //**************Generating Packets and Data Dissemenation

        public void DissemenateData()
        {
            //MessageBox.Show("I am here");
            PublicParameters.NumberOfNodesDissemenating += 1;
           
            if (this.isSinkAgent)
            {
                //Directly send to the sink
                this.GenerateDataToSink(PublicParameters.SinkNode);
            }
            else if (this.RingNodesRule.isRingNode)
            {
                //Directly send to the agent
                if (this.RingNodesRule.AnchorNode != null)
                {
                    this.GenerateDataToSink(this.RingNodesRule.AnchorNode);   
                }
            }

            else
            {
                //Need the Sink Agent's location to send
                this.GenerateQueryRequest();
            }
             
 
        }

        public void GenerateDataToSink(Sensor SinkAgent)
        {
           
            Packet packet = new Packet();
            PublicParameters.NumberofGeneratedDataPackets += 1;
            
            packet.Source = this;
            packet.PacketLength = PublicParameters.RoutingDataLength;
            packet.PacketType = PacketType.Data;
            packet.PID = PublicParameters.OverallGeneratedPackets;
            packet.Path = "" + this.ID;
            packet.Destination = SinkAgent;
            packet.TimeToLive = this.maxHopsForDestination(SinkAgent.CenterLocation);
            IdentifySourceNode(this);
            MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_packets.Content = PublicParameters.NumberofGeneratedDataPackets, DispatcherPriority.Normal);
            this.sendDataPack(packet);
        }
        
        public void GenerateQueryRequest()
        {
            if (Settings.Default.IsIntialized && this.ResidualEnergy > 0)
            {
                PublicParameters.NumberofGeneratedQueryPackets += 1;
                Packet QReq = new Packet();
                QReq.Path = "" + this.ID;
                QReq.TimeToLive = maxHopsForQuery(this);
                QReq.Source = this;
                QReq.PacketLength = PublicParameters.ControlDataLength;
                QReq.PacketType = PacketType.QReq;
                QReq.PID = PublicParameters.OverallGeneratedPackets;
                QReq.PointDestination = getDestinationForRingAccess();
                IdentifySourceNode(this);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_num_of_gen_query.Content = PublicParameters.NumberofGeneratedQueryPackets, DispatcherPriority.Normal);
                QueryDelayStartStop(false);
                SendQRequest(QReq);
            }
          
        }

        public void GenerateQueryResponse(Sensor destination)
        {
            Packet QResp = new Packet();
            try
            {
                PublicParameters.NumberofGeneratedQueryPackets += 1;
               
                QResp.Path = "" + this.ID;
                QResp.Destination = destination;
                QResp.TimeToLive = this.maxHopsForDestination(QResp.Destination.CenterLocation);
                QResp.Source = this;
                QResp.PacketLength = PublicParameters.ControlDataLength;
                QResp.PacketType = PacketType.QResp;
                QResp.PID = PublicParameters.OverallGeneratedPackets;
                QResp.SinkAgent = this.RingNodesRule.AnchorNode;
                IdentifySourceNode(this);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_num_of_gen_query.Content = PublicParameters.NumberofGeneratedQueryPackets, DispatcherPriority.Normal);
                //:
                
            }
            catch(NullReferenceException e)
            {
                MessageBox.Show("Couldnt generate Response, Sink Agent is null " + e.Message);
                return;
            }
            this.SendQResponse(QResp);
          
        }

        public void GenerateANS(Sensor oldAgent,Sensor newAgent)
        {
            Packet ASNewAgent = new Packet();

            PublicParameters.NumberofGeneratedFollowUpPackets += 1;
            ASNewAgent.Source = this;

            ASNewAgent.OldAgent = oldAgent;
            ASNewAgent.PacketLength = PublicParameters.ControlDataLength;
            ASNewAgent.PacketType = PacketType.ANS;
            ASNewAgent.PID = PublicParameters.OverallGeneratedPackets;
            ASNewAgent.Path = "" + this.ID;
            ASNewAgent.Destination = newAgent;
            ASNewAgent.TimeToLive = this.maxHopsForDestination(ASNewAgent.Destination.CenterLocation);
         /*   if (oldAgent.ID != newAgent.ID)
            {
                ASOldAgent = ASNewAgent;
                PublicParameters.NumberofGeneratedFollowUpPackets += 1;
                ASOldAgent.Destination = oldAgent;
                ASOldAgent.PID = PublicParameters.NumberofGeneratedDataPackets;
                this.SendAS(ASOldAgent);
            }*/
           
            IdentifySourceNode(this);
            MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
            this.SendANS(ASNewAgent);         

        }

        public void GenerateOldANS(Sensor OldAgent,Sensor newAgent)
        {
            //..WriteLine("Sending From {0} to old agent {1}", this.ID, OldAgent.ID);
            PublicParameters.NumberofGeneratedFollowUpPackets++;
            Packet FM = new Packet();
            FM.Source = this;
            try
            {
                FM.Destination = OldAgent;
                FM.PacketLength = PublicParameters.ControlDataLength;
                FM.PID = PublicParameters.OverallGeneratedPackets;
                FM.PacketType = PacketType.OldANS;
                FM.Path = "" + this.ID;
                FM.TimeToLive = this.maxHopsForDestination(OldAgent.CenterLocation);
                FM.SinkAgent = newAgent;
                IdentifySourceNode(this);
                MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
                this.sendOldANS(FM);
            }
            catch(NullReferenceException e)
            {
                Console.WriteLine(e.Message + " from generate FM agent null");
            }
            
           
        }

        public void GenerateANPI()
        {
            Packet FSA = new Packet();
            PublicParameters.NumberofGeneratedFollowUpPackets += 1;
            FSA.PointDestination = getDestinationForRingAccess();
            FSA.PacketLength = PublicParameters.ControlDataLength;
            FSA.PID = PublicParameters.OverallGeneratedPackets;
            FSA.PacketType = PacketType.ANPI;
            FSA.Path = "" + this.ID;
            FSA.TimeToLive = this.maxHopsForDestination(FSA.PointDestination);
            IdentifySourceNode(this);
            MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal); 
            FSA.Source = this;
            this.sendANPI(FSA);                       
        }

        public void GenerateANPISClock(int hops)
        {
            Packet ANPIS = new Packet();
            PublicParameters.NumberofGeneratedFollowUpPackets += 1;
            ANPIS.Source = this;
            ANPIS.Destination = this.RingNodesRule.ClockWiseNeighbor;
            ANPIS.SinkAgent = this.RingNodesRule.AnchorNode;
            ANPIS.PacketLength = PublicParameters.ControlDataLength;
            ANPIS.PID = PublicParameters.OverallGeneratedPackets;
            ANPIS.PacketType = PacketType.ANPISClock;
            ANPIS.Path = "" + this.ID;
            ANPIS.TimeToLive = hops +4;
            IdentifySourceNode(this);
            MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
            this.sendANPISClock(ANPIS);
        }
        public void GenerateANPISAntiClock(int hops)
        {
            Packet ANPIS = new Packet();

            PublicParameters.NumberofGeneratedFollowUpPackets += 1;
            ANPIS.Source = this;
            ANPIS.Destination = this.RingNodesRule.AntiClockWiseNeighbor;
            ANPIS.SinkAgent = this.RingNodesRule.AnchorNode;
            ANPIS.PacketLength = PublicParameters.ControlDataLength;
            ANPIS.PID = PublicParameters.OverallGeneratedPackets;
            ANPIS.PacketType = PacketType.ANPISAntiClock;
            ANPIS.Path = "" + this.ID;
            ANPIS.TimeToLive = hops + 4;
            IdentifySourceNode(this);
            MainWindow.Dispatcher.Invoke(() => PublicParameters.SinkNode.MainWindow.lbl_num_of_gen_followup.Content = PublicParameters.NumberofGeneratedFollowUpPackets, DispatcherPriority.Normal);
            this.sendANPISAntiClock(ANPIS);
        }
        
        //********************Sending

        public void sendDataPack(Packet packet)
        {
           
            lock (MiniFlowTable)
            {
                Sensor Reciver;

                if (isSinkAgent && packet.Destination.ID == PublicParameters.SinkNode.ID)
                {
                    Reciver = PublicParameters.SinkNode;
                    ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                    //Console.WriteLine("bn:" + ID + "->" + Reciver.ID + ". PID: " + packet.PID);
                    Reciver.RecieveDataPack(packet);
                    return;

                }
                else if (Operations.isInMyComunicationRange(this, packet.Destination))
                {
                    Reciver = packet.Destination;
                    if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                    {
                        ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                        Reciver.RecieveDataPack(packet);
                        return;
                    }
                    else
                    {
                        WaitingPacketsQueue.Enqueue(packet);
                        if (!QueuTimer.IsEnabled)
                        {
                            QueuTimer.Start();
                        }
                        RedundantTransmisionCost(packet, Reciver);
                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                        return;
                    }

                }
                else {
                    DownLinkRouting.GetD_Distribution(this, packet);
                    MiniFlowTableEntry FlowEntry = MatchFlow(packet);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                       
                        ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                        //Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + packet.PID);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecieveDataPack(packet);
                    }
                    else
                    {
                        // no available node right now.
                        // add the packt to the wait list.
                        //  Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + packet.PID);
                        WaitingPacketsQueue.Enqueue(packet);
                        if (!QueuTimer.IsEnabled)
                        {
                            QueuTimer.Start();
                        }
                        // Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                        return;
                    }
                }
                    
            }
        }

        public void SendQRequest(Packet QReq)
        {
             lock (MiniFlowTable)
              {
                  Sensor Reciver;
                if (this.RingNeighborRule.isNeighbor)
                {
                    if(RingNeighborRule.NearestRingNode != null)
                    {
                        if (Operations.isInMyComunicationRange(this,RingNeighborRule.NearestRingNode))
                        {
                            Reciver = RingNeighborRule.NearestRingNode;
                            if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                            {
                                ComputeOverhead(QReq, EnergyConsumption.Transmit, Reciver);
                                Reciver.RecvQueryRequest(QReq);
                                return;
                            }
                            else
                            {
                                WaitingPacketsQueue.Enqueue(QReq);
                                if (!QueuTimer.IsEnabled)
                                {
                                    QueuTimer.Start();
                                }
                                RedundantTransmisionCost(QReq, Reciver);
                                if (Settings.Default.ShowRadar) Myradar.StartRadio();
                                PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                                PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                                return;
                            }

                        }
                    }
                }
                  
                      DownLinkRouting.GetD_Distribution(this, QReq);
                      MiniFlowTableEntry FlowEntry = MatchFlow(QReq);

                      if (FlowEntry != null)
                      {
                          Reciver = FlowEntry.NeighborEntry.NeiNode;
                          ComputeOverhead(QReq, EnergyConsumption.Transmit, Reciver);
                          Reciver.RecvQueryRequest(QReq);
                          return;
                      }
                      else
                      {
                          // no available node right now.
                          // add the packt to the wait list.
                          WaitingPacketsQueue.Enqueue(QReq);
                          if (!QueuTimer.IsEnabled)
                          {
                              QueuTimer.Start();
                          }

                          if (Settings.Default.ShowRadar) Myradar.StartRadio();
                          PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                          PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                          return;
                      }
                  }
                    
            
        }

         public void SendQResponse(Packet QResp)
        {
            lock (MiniFlowTable)
            {
                Sensor Reciver;
                if (Operations.isInMyComunicationRange(this, QResp.Destination))
                {
                    Reciver = QResp.Destination;
                    if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                    {
                        ComputeOverhead(QResp, EnergyConsumption.Transmit, Reciver);
                        Reciver.RecvQueryResponse(QResp);
                    }
                    else
                    {
                        WaitingPacketsQueue.Enqueue(QResp);
                        QueuTimer.Start();
                        RedundantTransmisionCost(QResp, Reciver);
                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }

                }
                else
                {
                    DownLinkRouting.GetD_Distribution(this, QResp);
                    MiniFlowTableEntry FlowEntry = MatchFlow(QResp);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                       // SwichToActive(); // this.
                        ComputeOverhead(QResp, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecvQueryResponse(QResp);
                    }
                    else
                    {
                        // no available node right now.
                        // add the packt to the wait list.
                        WaitingPacketsQueue.Enqueue(QResp);
                        if (!QueuTimer.IsEnabled)
                        {
                            QueuTimer.Start();
                        }

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }
               
            }
        }

   
            
            
      
            
        /*void SendQuery(Packet packet)
        {
            if (packet.PacketType == PacketType.Query)
            {
                lock (MiniFlowTable)
                {                  
                    DownLinkRouting.GetD_Distribution(this, packet);
                    MiniFlowTableEntry FlowEntry = MatchFlow(packet);
                    if (FlowEntry != null)
                    {                     
                        Sensor Reciver = FlowEntry.NeighborEntry.NeiNode;
                        SwichToActive(); // this.
                        ComputeOverhead(packet, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecieveQuery(packet);
                    }
                    else
                    {
                        // no available node right now.
                        // add the packt to the wait list.
                        WaitingPacketsQueue.Enqueue(packet);
                        if (!QueuTimer.IsEnabled)
                        {
                            QueuTimer.Start();
                        }

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParamerters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParamerters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }
            }
        }
        */

     
        public void SendANS(Packet ANS)
        {
            lock (MiniFlowTable) {

                Sensor Reciver = ANS.Destination;
                if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                {
                    ComputeOverhead(ANS, EnergyConsumption.Transmit, Reciver);
                   // Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + AS.PID);
                    Reciver.RecieveANS(ANS);
                }
                else
                {
                   // Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + AS.PID);
                    WaitingPacketsQueue.Enqueue(ANS);
                    QueuTimer.Start();
                   // Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");
                    RedundantTransmisionCost(ANS, Reciver);
                    if (Settings.Default.ShowRadar) Myradar.StartRadio();
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                }
            }
           
        }

        public void sendOldANS(Packet FM)
        {
            Sensor Reciver;
            if (Operations.isInMyComunicationRange(this, FM.Destination))
            {
                Reciver = FM.Destination;
                if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                {
                    ComputeOverhead(FM, EnergyConsumption.Transmit, Reciver);
                    Reciver.RecieveFM(FM);
                }
                else
                {
                    WaitingPacketsQueue.Enqueue(FM);
                    QueuTimer.Start();
                    RedundantTransmisionCost(FM, Reciver);
                    if (Settings.Default.ShowRadar) Myradar.StartRadio();
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                }

            }
            else
            {
                DownLinkRouting.GetD_Distribution(this, FM);
                MiniFlowTableEntry FlowEntry = MatchFlow(FM);
                if (FlowEntry != null)
                {
                    Reciver = FlowEntry.NeighborEntry.NeiNode;
                    ComputeOverhead(FM, EnergyConsumption.Transmit, Reciver);
                    FlowEntry.DownLinkStatistics += 1;
                    Reciver.RecieveFM(FM);

                }
                else
                {
                    // Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + FM.PID);
                    WaitingPacketsQueue.Enqueue(FM);
                    QueuTimer.Start();
                    //  Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                    if (Settings.Default.ShowRadar) Myradar.StartRadio();
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                }
            }
           
        }

        public void sendANPI(Packet ANPI)
        {
                Sensor Reciver;
            if (this.RingNeighborRule.isNeighbor)
            {
                if (RingNeighborRule.NearestRingNode != null)
                {
                    if (Operations.isInMyComunicationRange(this, RingNeighborRule.NearestRingNode))
                    {
                        Reciver = RingNeighborRule.NearestRingNode;
                        if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                        {
                            ComputeOverhead(ANPI, EnergyConsumption.Transmit, Reciver);
                            Reciver.RecieveANPI(ANPI);
                            return;
                        }
                        else
                        {
                            WaitingPacketsQueue.Enqueue(ANPI);
                            if (!QueuTimer.IsEnabled)
                            {
                                QueuTimer.Start();
                            }
                            RedundantTransmisionCost(ANPI, Reciver);
                            if (Settings.Default.ShowRadar) Myradar.StartRadio();
                            PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                            PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                            return;
                        }

                    }
                }
            }
            DownLinkRouting.GetD_Distribution(this, ANPI);
                    MiniFlowTableEntry FlowEntry = MatchFlow(ANPI);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                        ComputeOverhead(ANPI, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecieveANPI(ANPI);
                    }
                    else
                    {
                        //  Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + FSA.PID);
                        WaitingPacketsQueue.Enqueue(ANPI);
                        QueuTimer.Start();
                        ///Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    
                }
           
        }


        public void sendANPISClock(Packet ANPIS)
        {
                Sensor Reciver = ANPIS.Destination;
                if (Operations.isInMyComunicationRange(this, Reciver))
                {
                    if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                    {
                        ComputeOverhead(ANPIS, EnergyConsumption.Transmit, Reciver);
                        // Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + AS.PID);
                        Reciver.RecieveANPISClock(ANPIS);
                    }
                    else
                    {
                        // Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + AS.PID);
                        WaitingPacketsQueue.Enqueue(ANPIS);
                        QueuTimer.Start();
                        // Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");
                        RedundantTransmisionCost(ANPIS, Reciver);
                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }
                else
                {
                    DownLinkRouting.GetD_Distribution(this, ANPIS);
                    MiniFlowTableEntry FlowEntry = MatchFlow(ANPIS);
                    if (FlowEntry != null)
                    {
                        Reciver = FlowEntry.NeighborEntry.NeiNode;
                        ComputeOverhead(ANPIS, EnergyConsumption.Transmit, Reciver);
                        FlowEntry.DownLinkStatistics += 1;
                        Reciver.RecieveANPISClock(ANPIS);
                    }
                    else
                    {
                        //  Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + FSA.PID);
                        WaitingPacketsQueue.Enqueue(ANPIS);
                        QueuTimer.Start();
                        ///Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                        if (Settings.Default.ShowRadar) Myradar.StartRadio();
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                        PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                    }
                }

               
            
        }

        public void sendANPISAntiClock(Packet ANPIS)
        {
            Sensor Reciver = ANPIS.Destination;
            if (Operations.isInMyComunicationRange(this, Reciver))
            {
                if (Reciver.CanRecievePacket && Reciver.CurrentSensorState == SensorState.Active)
                {
                    ComputeOverhead(ANPIS, EnergyConsumption.Transmit, Reciver);
                    // Console.WriteLine("sucess:" + ID + "->" + Reciver.ID + ". PID: " + AS.PID);
                    Reciver.RecieveANPISAntiClock(ANPIS);
                }
                else
                {
                    // Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + AS.PID);
                    WaitingPacketsQueue.Enqueue(ANPIS);
                    QueuTimer.Start();
                    // Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");
                    RedundantTransmisionCost(ANPIS, Reciver);
                    if (Settings.Default.ShowRadar) Myradar.StartRadio();
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                }
            }
            else
            {
                DownLinkRouting.GetD_Distribution(this, ANPIS);
                MiniFlowTableEntry FlowEntry = MatchFlow(ANPIS);
                if (FlowEntry != null)
                {
                    Reciver = FlowEntry.NeighborEntry.NeiNode;
                    ComputeOverhead(ANPIS, EnergyConsumption.Transmit, Reciver);
                    FlowEntry.DownLinkStatistics += 1;
                    Reciver.RecieveANPISAntiClock(ANPIS);
                }
                else
                {
                    //  Console.WriteLine("NID:" + this.ID + " Faild to sent PID:" + FSA.PID);
                    WaitingPacketsQueue.Enqueue(ANPIS);
                    QueuTimer.Start();
                    ///Console.WriteLine("NID:" + this.ID + ". Queu Timer is started.");

                    if (Settings.Default.ShowRadar) Myradar.StartRadio();
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
                    PublicParameters.MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Visible);
                }
            }

           

        }

        //*******************Recieving 

        #region //Recieving a follow Up
        //Inform Old Agent
        public void RecieveFM(Packet FM)
        {
            FM.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                if (this.ID == FM.Destination.ID )
                {
                    //if (this.isSinkAgent)
                    //{
                        this.isSinkAgent = false;
                        MainWindow.Dispatcher.Invoke(() => Ellipse_HeaderAgent_Mark.Visibility = Visibility.Hidden, DispatcherPriority.Send);
                        this.NeighborsTable.RemoveAll(sinkItem => sinkItem.NeiNode.ID == PublicParameters.SinkNode.ID);
                        this.AgentNode.NewAgent = FM.Source;
                        //this.AgentNode.initiateNewAgentTimer();
                        if (this.AgentNode.hasStoredPackets)
                        {
                            this.AgentDelieverStoredPackets();
                        }
                        FM.isDelivered = true;
                        updateStates(FM);
                    //}
                }
                else
                {
                    if (FM.Hops > FM.TimeToLive)
                    {
                        FM.isDelivered = false;
                        FM.DroppedReason = " Hops > Time To Live ";
                        updateStates(FM);
                    }
                    else
                    {
                        sendOldANS(FM);
                    }
                }
            }
            else
            {
                FM.isDelivered = false;
                FM.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(FM);
            }
        }

        //Inform Root
        public void RecieveANPI(Packet ANPI)
        {
            ANPI.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                ANPI.Path += ">" + this.ID;
                
                if (this.RingNodesRule.isRingNode)
                {
                    try
                    {
                        this.RingNodesRule.AnchorNode = ANPI.Source;
                        ANPI.isDelivered = true;
                        this.updateStates(ANPI);
                        
                    }
                    catch (NullReferenceException exp)
                    {
                        MessageBox.Show(exp.Message + "ANPI Content Null");
                    }
                    int half = PublicParameters.RingNodes.Count*2;
                    this.GenerateANPISClock(half);
                    this.GenerateANPISAntiClock(half);
                    
                }
                else if (ANPI.Hops > ANPI.TimeToLive)
                {
                    ANPI.isDelivered = false;
                    ANPI.DroppedReason = "Hops > Time to Live ";
                    updateStates(ANPI);
                    return;
                }
                else
                {
                    if (RingNeighborRule.isNeighbor)
                    {
                        ANPI.Destination = RingNeighborRule.NearestRingNode;
                    }
                    this.sendANPI(ANPI);
                }
            }
            else
            {
                ANPI.isDelivered = false;
                ANPI.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(ANPI);
            }
           
        }

        //Agent Selection
        public void RecieveANS(Packet ANS)
        {
            ANS.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                ANS.Path += ">" + this.ID;
                if (this.ID == ANS.Destination.ID) {
                        //recieve by new agent 
                    try
                    {
                        this.AgentNode = new Agent(ANS.Source, ANS.OldAgent, this);
                        this.isSinkAgent = true;
                        ANS.isDelivered = true;
                        updateStates(ANS);
                        NeighborsTableEntry sinkEntry = new NeighborsTableEntry();
                        sinkEntry.NeiNode = PublicParameters.SinkNode;
                        this.NeighborsTable.Add(sinkEntry);
                        Ellipse_HeaderAgent_Mark.Stroke = new SolidColorBrush(Colors.Black);
                        MainWindow.Dispatcher.Invoke(() => Ellipse_HeaderAgent_Mark.Visibility = Visibility.Visible);

                        this.GenerateANPI();
                        
                        if (this.AgentNode.hasStoredPackets)
                        {
                            this.AgentDelieverStoredPackets();
                        }
                    }
                    catch (NullReferenceException e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("ANS contains null");
                    }
                       
                       
                }
                else
                {
                    if (ANS.Hops > ANS.TimeToLive)
                    {
                        // drop the paket.
                        ANS.isDelivered = false;
                        ANS.DroppedReason = "Hops > Time to live ";
                        updateStates(ANS);
                    }
                    else
                    {
                        SendANS(ANS);
                    }
                }
                
            }
            else
            {
                ANS.isDelivered = false;
                ANS.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(ANS);
            }
           
        }

        //Agent Location sharing Clockwise and AntiClockwise
        public void RecieveANPISClock(Packet ANPIS)
        {
            if(ANPIS.Destination == null)
            {
                Console.WriteLine("Clock null");
            }
            ANPIS.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                if (ID == ANPIS.Destination.ID && RingNodesRule.isRingNode)
                {
                    ANPIS.Path += ">" + this.ID;
                    if (this.RingNodesRule.AnchorNode == null)
                    {
                        this.RingNodesRule.AnchorNode = ANPIS.SinkAgent;
                        if (ANPIS.Hops > ANPIS.TimeToLive)
                        {
                            // drop the paket.
                            ANPIS.isDelivered = false;
                            ANPIS.DroppedReason = "Hops > Time to live";
                            updateStates(ANPIS);
                            return;
                        }
                        ANPIS.Destination = this.RingNodesRule.ClockWiseNeighbor;
                        if (ANPIS.Destination == null)
                        {
                            Console.WriteLine("Clock null");
                        }
                        sendANPISClock(ANPIS);
                        return;
                    }
                    else
                    {
                        if (ANPIS.SinkAgent.ID == this.RingNodesRule.AnchorNode.ID)
                        {
                            ANPIS.isDelivered = true;
                            updateStates(ANPIS);
                            return;
                        }
                        else
                        {
                            this.RingNodesRule.AnchorNode = ANPIS.SinkAgent;
                            if (ANPIS.Hops > ANPIS.TimeToLive)
                            {
                                // drop the paket.
                                ANPIS.isDelivered = false;
                                ANPIS.DroppedReason = "Hops > Time to live";
                                updateStates(ANPIS);
                                return;
                            }
                           
                            ANPIS.Destination = this.RingNodesRule.ClockWiseNeighbor;
                            if (ANPIS.Destination == null)
                            {
                                Console.WriteLine("Clock null");
                            }
                            sendANPISClock(ANPIS);
                        }
                    }

                }
                else
                {
                    if (ANPIS.Hops > ANPIS.TimeToLive)
                    {
                        // drop the paket.
                        ANPIS.isDelivered = false;
                        ANPIS.DroppedReason = "Hops > Time to live";
                        updateStates(ANPIS);
                        return;
                    }
                    else
                    {
                        sendANPISClock(ANPIS);
                    }
                    
                   // MessageBox.Show("Wrong Destination in the Clock ANPIS");
                    //ANPIS.isDelivered = false;
                    //updateStates(ANPIS);
                }
               
            }
        }
        public void RecieveANPISAntiClock(Packet ANPIS)
        {
            ANPIS.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                if (ID == ANPIS.Destination.ID && RingNodesRule.isRingNode)
                {
                    if (RingNodesRule.AntiClockWiseNeighbor == null)
                    {
                        Console.WriteLine("Anti Neighbor is null");
                    }
                    ANPIS.Path += ">" + this.ID;
                    if (this.RingNodesRule.AnchorNode == null)
                    {
                        this.RingNodesRule.AnchorNode = ANPIS.SinkAgent;
                        if (ANPIS.Hops > ANPIS.TimeToLive)
                        {
                            // drop the paket.
                            ANPIS.isDelivered = false;
                            ANPIS.DroppedReason = "Hops > Time to live";
                            updateStates(ANPIS);
                            return;
                        }
                        ANPIS.Destination = this.RingNodesRule.AntiClockWiseNeighbor;
                        if (ANPIS.Destination == null)
                        {
                            Console.WriteLine("Anti Neighbor destination is null");
                        }
                        sendANPISAntiClock(ANPIS);
                        return;
                    }
                    else
                    {
                        if (ANPIS.SinkAgent.ID == this.RingNodesRule.AnchorNode.ID)
                        {
                            ANPIS.isDelivered = true;
                            updateStates(ANPIS);
                            return;
                        }
                        else
                        {
                            this.RingNodesRule.AnchorNode = ANPIS.SinkAgent;
                            if (ANPIS.Hops > ANPIS.TimeToLive)
                            {
                                // drop the paket.
                                ANPIS.isDelivered = false;
                                ANPIS.DroppedReason = "Hops > Time to live";
                                updateStates(ANPIS);
                                return;
                            }
                            ANPIS.Destination = this.RingNodesRule.AntiClockWiseNeighbor;
                            try
                            {
                                if (!(ANPIS.Destination.RingNodesRule.isRingNode))
                                {
                                    Console.WriteLine();
                                }
                            }
                            catch
                            {
                                ANPIS.Destination = null;
                                ANPIS.isDelivered = false;
                                ANPIS.DroppedReason = "ANPIS Empty Destination";
                                updateStates(ANPIS);
                            }
                            sendANPISAntiClock(ANPIS);
                        }
                    }

                }
                else
                {
                    if (ANPIS.Hops > ANPIS.TimeToLive)
                    {
                        // drop the paket.
                        ANPIS.isDelivered = false;
                        ANPIS.DroppedReason = "Hops > Time to live";
                        updateStates(ANPIS);
                        return;
                    }
                    else
                    {
                        sendANPISClock(ANPIS);
                    }
                }

            }
        }
        #endregion
    

        #region //Recieving Data Packet
        public void RecieveDataPack(Packet packet)
        {
            packet.ReTransmissionTry = 0;
            if (!this.CanRecievePacket)
            {
                packet.isDelivered = false;
                packet.DroppedReason = "Node "+this.ID+" can't recieve packet";
                updateStates(packet);
                return;
            }
            packet.Path += ">" + this.ID;
            if (this.ID == PublicParameters.SinkNode.ID)
            {
                packet.isDelivered = true;
                updateStates(packet);
                return;
            }
            else if (packet.Destination.ID == this.ID)
            {
            
                if (this.isSinkAgent)
                {
                    if (this.AgentNode.isSinkInRange())
                    {
                        packet.Destination = PublicParameters.SinkNode;
                        this.sendDataPack(packet);
                        return;
                    }
                    else
                    {
                        this.AgentNode.AgentStorePacket(packet);
                    }
                   
                }
                else
                {
                    //Old Agent Follow Up Mechanisim
                   
                    try
                    {
                        packet.Destination = this.AgentNode.NewAgent;
                        packet.TimeToLive += maxHopsForDestination(packet.Destination.CenterLocation);
                        this.sendDataPack(packet);
                    }
                    catch(NullReferenceException e)
                    {
                        //Agent already removed now do something else Drop packet'
                        packet.isDelivered = false;
                        packet.DroppedReason = "Old Agent is already restored";
                        Console.WriteLine(e.Message);
                        updateStates(packet);
                    }
                   
                }
            }
            else
            {
                if (packet.Hops > packet.TimeToLive)
                {
                    // drop the paket.
                    packet.isDelivered = false;
                    packet.DroppedReason = "Hops > Time to live";
                    updateStates(packet);
                }
                else
                {
                    // forward the packet.
                    this.sendDataPack(packet);
                }
            }
        }
        #endregion


        #region//Recieving Query
        public void  RecvQueryResponse(Packet QResp)
        {
            QResp.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                QResp.Path += ">" + this.ID;
                List<int> paths = Operations.PacketPathToIDS(QResp.Path);
                
                if (QResp.Destination.ID == this.ID)
                {
                    try
                    {
                        QueryDelayStartStop(true);
                        QResp.isDelivered = true;
                        updateStates(QResp);
                        this.GenerateDataToSink(QResp.SinkAgent);
                        return;
                    }
                    catch(NullReferenceException e)
                    {
                        QResp.isDelivered = false;
                        QResp.DroppedReason = "Sink Agent is null";
                        updateStates(QResp);
                        Console.WriteLine("Query Recieved But Sink Agent is null" + e.Message);
                        return;
                    }
                }
                else
                {
                    if (QResp.Hops > QResp.TimeToLive)
                    {
                        // drop the paket.
                        QResp.DroppedReason = "Hops > Time to live";
                        QResp.isDelivered = false;
                        updateStates(QResp);
                    }
                    else
                    {
                        // forward the packet.
                        this.SendQResponse(QResp);
                    }
                }
            }
            else
            {
                QResp.isDelivered = false;
                QResp.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(QResp);
            }
           
        }

        public void RecvQueryRequest(Packet QReq)
        {
            QReq.ReTransmissionTry = 0;
            if (this.CanRecievePacket)
            {
                bool shouldChange = BT.threshReached(this.ResidualEnergyPercentage);
                QReq.Path += ">" + this.ID;
                if (this.RingNodesRule.isRingNode)
                {
                        try
                        {
                            if (this.RingNodesRule.AnchorNode != null)
                            {
                                this.GenerateQueryResponse(QReq.Source);
                                if (shouldChange)
                                {
                                // RingNodesFunctions.ChangeRingNode(this.RingNodesRule, true);
                                    RingNodeChange chn = new RingNodeChange();
                                    chn.ChangeRingNode(this.RingNodesRule);
                                }
                            }
                            else
                            {
                                QReq.isDelivered = false;
                                updateStates(QReq);
                                return;
                            }
                            QReq.isDelivered = true;
                            updateStates(QReq);
                            return;
                        }
                        catch(NullReferenceException e)
                        {
                         
                            QReq.isDelivered = false;
                            QReq.DroppedReason = "Query Request Source Null";
                            updateStates(QReq); 
                            Console.WriteLine("Query Recieved But Source  is null" + e.Message);
                        }

                }
                
                else
                {

                    if (QReq.Hops > QReq.TimeToLive)
                    {
                        // drop the paket.
                        QReq.isDelivered = false;
                        QReq.DroppedReason = "Hops > Time to live";
                        updateStates(QReq);
                        return;
                    }
                    else
                    {
                        if (RingNeighborRule.isNeighbor)
                        {
                            QReq.Destination = RingNeighborRule.NearestRingNode;
                        }
                        // forward the packet.
                        this.SendQRequest(QReq);
                        return;
                    }
                }
            }
            else
            {
                QReq.isDelivered = false;
                QReq.DroppedReason = "Node " + this.ID + " can't recieve packet";
                updateStates(QReq);
            }
        }
      
        #endregion

        public void AgentDelieverStoredPackets()
        {
            do
            {
                Packet packet = this.AgentNode.AgentBuffer.Dequeue();
                if (this.isSinkAgent)
                {
                    if (this.AgentNode.isSinkInRange())
                    {
                      //  Console.WriteLine("Sending to the sink directly");
                        packet.Destination = PublicParameters.SinkNode;
                        packet.TimeToLive += maxHopsForDestination(packet.Destination.CenterLocation);
                        sendDataPack(packet);
                    }
                }
                else if (this.AgentNode.NewAgent != null)
                {
                   // Console.WriteLine("Sending to the new agent, packet {0}", packet.PID);
                    packet.Destination = this.AgentNode.NewAgent;
                    packet.TimeToLive += maxHopsForDestination(packet.Destination.CenterLocation);
                    PIDE = packet.PID;
                    sendDataPack(packet);
                }
                else
                {
                    packet.isDelivered = false;
                    packet.DroppedReason = "Old Agent unkown destination";
                    updateStates(packet);
                }
            } while (this.AgentNode.AgentBuffer.Count > 0);
            
        }


        public static long PIDE = -1;


        public void updateStates(Packet packet)
        {
            if (packet.isDelivered)
            {
                if (packet.PacketType == PacketType.OldANS || packet.PacketType == PacketType.ANPI || packet.PacketType == PacketType.ANS || packet.PacketType == PacketType.ANPISClock || packet.PacketType == PacketType.ANPISAntiClock)
                {
                    PublicParameters.NumberofDelieveredFollowUpPackets += 1;
                }
                else if (packet.PacketType == PacketType.QReq || packet.PacketType == PacketType.QResp)
                {
                    PublicParameters.NumberOfDelieveredQueryPackets += 1;
                    packet.ComputeDelay();
                    PublicParameters.QueryDelay += packet.Delay;
                }
                else
                {
                    PublicParameters.NumberOfDelieveredDataPackets += 1;
                    packet.ComputeDelay();
                    PublicParameters.DataDelay += packet.Delay;
                }

                PublicParameters.NumberofDeliveredPackets += 1;
               // Console.WriteLine("{2} Packet: {0} with Path: {1} delievered",packet.PID,packet.Path,packet.PacketType);
                PublicParameters.FinishedRoutedPackets.Add(packet);
                ComputeOverhead(packet, EnergyConsumption.Recive, null);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_total_consumed_energy.Content = PublicParameters.TotalEnergyConsumptionJoule + " (JOULS)", DispatcherPriority.Send);

                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Delivered_QPacket.Content = PublicParameters.NumberOfDelieveredQueryPackets, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Delivered_CPacket.Content = PublicParameters.NumberofDelieveredFollowUpPackets, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Delivered_Packet.Content = PublicParameters.NumberOfDelieveredDataPackets, DispatcherPriority.Send);

                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_sucess_ratio.Content = PublicParameters.DeliveredRatio, DispatcherPriority.Send);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_nymber_inQueu.Content = PublicParameters.InQueuePackets.ToString());
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_num_of_disseminatingNodes.Content = PublicParameters.NumberOfNodesDissemenating.ToString());
                 MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Average_QDelay.Content = PublicParameters.AverageQueryDelay.ToString());
                 MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Total_Delay.Content = PublicParameters.AverageTotalDelay.ToString());

                UnIdentifySourceNode(packet.Source);
                // Console.WriteLine("PID:" + packet.PID + " has been delivered.");
            }
            else
            {
                //Drop the packet
                if (packet.PacketType == PacketType.ANPI || packet.PacketType == PacketType.ANS)
                {
                    if (packet.Source.isSinkAgent)
                    {
                       // Agent.checkDroppedPacket(packet);
                    }
                    else
                    {
                       // MobileModel.checkDroppedPacket(packet);
                    }
                }
               // Console.WriteLine("Failed {2} PID: {0} Reason: {1}", packet.PID, packet.DroppedReason,packet.PacketType);
                PublicParameters.NumberofDropedPackets += 1;
                PublicParameters.FinishedRoutedPackets.Add(packet);
                //  Console.WriteLine("PID:" + packet.PID + " has been droped.");
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);
            }
        }

        private void DeliveerPacketsInQueuTimer_Tick(object sender, EventArgs e)
        {

            if (WaitingPacketsQueue.Count > 0) { 
            Packet toppacket = WaitingPacketsQueue.Dequeue();
            toppacket.WaitingTimes += 1;
            toppacket.ReTransmissionTry += 1;
            PublicParameters.TotalWaitingTime += 1; // total;
            if (toppacket.ReTransmissionTry < 7)
            {
                //Dispatcher.Invoke(() => SwichToActive(), DispatcherPriority.Send);

                if (toppacket.PacketType == PacketType.QResp)
                {
                    SendQResponse(toppacket);
                }
                else if (toppacket.PacketType == PacketType.QReq)
                {
                    SendQRequest(toppacket);
                }
                else if (toppacket.PacketType == PacketType.ANPI)
                {
                    sendANPI(toppacket);
                }
                else if (toppacket.PacketType == PacketType.ANS)
                {
                    SendANS(toppacket);
                }
                else if (toppacket.PacketType == PacketType.Data)
                {
                    sendDataPack(toppacket);
                }
                else if (toppacket.PacketType == PacketType.OldANS)
                {
                    sendOldANS(toppacket);
                }
                else if (toppacket.PacketType == PacketType.ANPISClock)
                {
                    sendANPISClock(toppacket);
                }
                else if (toppacket.PacketType == PacketType.ANPISAntiClock)
                {
                    sendANPISAntiClock(toppacket);
                }

                else
                {
                    MessageBox.Show("Unknown");
                }
            }  
            else
            {
               // PublicParameters.NumberofDropedPackets += 1;
                toppacket.isDelivered = false;
                toppacket.DroppedReason = "Waiting times > 7";
                updateStates(toppacket);
              //  Console.WriteLine("Waiting times more for packet {0}", toppacket.PID);
               // PublicParameters.FinishedRoutedPackets.Add(toppacket);
            //    MessageBox.Show("PID:" + toppacket.PID + " has been droped. Packet Type = "+toppacket.PacketType);
                MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParameters.NumberofDropedPackets, DispatcherPriority.Send);
               

            }
            if (WaitingPacketsQueue.Count == 0)
            {
                if(Settings.Default.ShowRadar) Myradar.StopRadio();

                QueuTimer.Stop();
               // Console.WriteLine("NID:" + this.ID + ". Queu Timer is stoped.");
                MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Fill = Brushes.Transparent);
                MainWindow.Dispatcher.Invoke(() => Ellipse_indicator.Visibility = Visibility.Hidden);
            }
            MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_nymber_inQueu.Content = PublicParameters.InQueuePackets.ToString());
            }
        }


        private void RemoveOldAgentTimer_Tick(object sender, EventArgs e) {
            OldAgentTimer.Stop();
            this.AgentNode = new Agent();
        }

               

        public void RedundantTransmisionCost(Packet pacekt, Sensor reciverNode)
        {
            // logs.
            PublicParameters.TotalReduntantTransmission += 1;
            double UsedEnergy_Nanojoule = EnergyModel.Receive(PublicParameters.PreamblePacketLength); // preamble packet length.
            double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
            reciverNode.ResidualEnergy = reciverNode.ResidualEnergy - UsedEnergy_joule;
            pacekt.UsedEnergy_Joule += UsedEnergy_joule;
            PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
            PublicParameters.TotalWastedEnergyJoule += UsedEnergy_joule;
            MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Redundant_packets.Content = PublicParameters.TotalReduntantTransmission);
            MainWindow.Dispatcher.Invoke(() => MainWindow.lbl_Wasted_Energy_percentage.Content = PublicParameters.WastedEnergyPercentage);
        }

        /// <summary>
        /// the node which is active will send preample packet and will be selected.
        /// match the packet.
        /// </summary>
        public MiniFlowTableEntry MatchFlow(Packet packet)
        {

            MiniFlowTableEntry ret = null;
            try
            {
               
                if (MiniFlowTable.Count > 0)
                {
                  
                    foreach (MiniFlowTableEntry selectedflow in MiniFlowTable)
                    {
                        if (selectedflow.NID != PublicParameters.SinkNode.ID)
                        {
                            if (selectedflow.SensorState == SensorState.Active && selectedflow.DownLinkAction == FlowAction.Forward) // && selectedflow.DownLinkAction == FlowAction.Forward && selectedflow.SensorBufferHasSpace)
                            {
                                if (ret == null)
                                {
                                    ret = selectedflow;
                                }
                                else
                                {
                                    RedundantTransmisionCost(packet, selectedflow.NeighborEntry.NeiNode);
                                }
                            }
                        }
                        
                    }
                }
                else
                {
                    MessageBox.Show("No Flow!!!. muach flow!");
                    return null;
                }
            }
            catch
            {
                ret = null;
                MessageBox.Show(" Null Match.!");
            }

            return ret;
        }

        // When the sensor open the channel to transmit the data.
  
        
        




        public void ComputeOverhead(Packet packt, EnergyConsumption enCon, Sensor Reciver)
        {
            if (enCon == EnergyConsumption.Transmit)
            {
                if (ID != PublicParameters.SinkNode.ID)
                {
                    // calculate the energy 
                    double Distance_M = Operations.DistanceBetweenTwoSensors(this, Reciver);
                    double UsedEnergy_Nanojoule = EnergyModel.Transmit(packt.PacketLength, Distance_M);
                    double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
                    ResidualEnergy = this.ResidualEnergy - UsedEnergy_joule;
                    PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
                    packt.UsedEnergy_Joule += UsedEnergy_joule;
                    packt.RoutingDistance += Distance_M;
                    packt.Hops += 1;
                    double delay = DelayModel.DelayModel.Delay(this, Reciver);
                    packt.Delay += delay;
                    PublicParameters.TotalDelayMs += delay;
                    if (Settings.Default.SaveRoutingLog)
                    {
                        RoutingLog log = new RoutingLog();
                        log.PacketType = PacketType.Data;
                        log.IsSend = true;
                        log.NodeID = this.ID;
                        log.Operation = "To:" + Reciver.ID;
                        log.Time = DateTime.Now;
                        log.Distance_M = Distance_M;
                        log.UsedEnergy_Nanojoule = UsedEnergy_Nanojoule;
                        log.RemaimBatteryEnergy_Joule = ResidualEnergy;
                        log.PID = packt.PID;
                        this.Logs.Add(log);
                    }

                    // for control packet.
                    if (packt.isAdvirtismentPacket())
                    {
                        // just to remember how much energy is consumed here.
                        PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule;
                    }
                }

                if (Settings.Default.ShowRoutingPaths)
                {
                    OpenChanel(Reciver.ID, packt.PID);
                }

            }
            else if (enCon == EnergyConsumption.Recive)
            {

                double UsedEnergy_Nanojoule = EnergyModel.Receive(packt.PacketLength);
                double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
                ResidualEnergy = ResidualEnergy - UsedEnergy_joule;
                packt.UsedEnergy_Joule += UsedEnergy_joule;
                PublicParameters.TotalEnergyConsumptionJoule += UsedEnergy_joule;


                if (packt.isAdvirtismentPacket())
                {
                    // just to remember how much energy is consumed here.
                    PublicParameters.EnergyComsumedForControlPackets += UsedEnergy_joule;
                }


            }

        }

     
        #endregion







        private void lbl_MouseEnter(object sender, MouseEventArgs e)
        {
            ToolTip = new Label() { Content = "("+ID + ") [ " + ResidualEnergyPercentage + "% ] [ " + ResidualEnergy + " J ]" };
        }

        private void btn_show_routing_log_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(Logs.Count>0)
            {
                UiShowRelativityForAnode re = new ui.UiShowRelativityForAnode();
                re.dg_relative_shortlist.ItemsSource = Logs;
                re.Show();
            }
        }

        private void btn_draw_random_numbers_MouseDown(object sender, MouseButtonEventArgs e)
        {
            List<KeyValuePair<int, double>> rands = new List<KeyValuePair<int, double>>();
            int index = 0;
            foreach (RoutingLog log in Logs )
            {
                if(log.IsSend)
                {
                    index++;
                    rands.Add(new KeyValuePair<int, double>(index, log.ForwardingRandomNumber));
                }
            }
            UiRandomNumberGeneration wndsow = new ui.UiRandomNumberGeneration();
            wndsow.chart_x.DataContext = rands;
            wndsow.Show();
        }

        private void Ellipse_center_MouseEnter(object sender, MouseEventArgs e)
        {
            
        }

        private void btn_show_my_duytcycling_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
        }

        private void btn_draw_paths_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NetworkVisualization.UpLinksDrawPaths(this);
        }

       
         
        private void btn_show_my_flows_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
            ListControl ConMini = new ui.conts.ListControl();
            ConMini.lbl_title.Content = "Mini-Flow-Table";
            ConMini.dg_date.ItemsSource = MiniFlowTable;


            ListControl ConNei = new ui.conts.ListControl();
            ConNei.lbl_title.Content = "Neighbors-Table";
            ConNei.dg_date.ItemsSource = NeighborsTable;

            UiShowLists win = new UiShowLists();
            win.stack_items.Children.Add(ConMini);
            win.stack_items.Children.Add(ConNei);
            win.Title = "Tables of Node " + ID;
            win.Show();
            win.WindowState = WindowState.Maximized;
        }

        private void btn_send_1_p_each1sec_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SendPacketTimer.Start();
            SendPacketTimer.Tick += SendPacketTimer_Random; // redfine th trigger.
        }



        public void RandomSelectEndNodes(int numOFpACKETS)
        {
            if (PublicParameters.SimulationTime > PublicParameters.MacStartUp)
            {
                int index = 1 + Convert.ToInt16(UnformRandomNumberGenerator.GetUniform(PublicParameters.NumberofNodes - 2));
                if (index != PublicParameters.SinkNode.ID)
                {
                    Sensor endNode = MainWindow.myNetWork[index];
                    GenerateMultipleControlPackets(numOFpACKETS, endNode);
                }
            }
        }

        private void SendPacketTimer_Random(object sender, EventArgs e)
        {
            if (ID != PublicParameters.SinkNode.ID)
            {
                // uplink:
                GenerateMultipleDataPackets(1);
            }
            else
            { //
                RandomSelectEndNodes(1);
            }
        }

        /// <summary>
        /// i am slected as end node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_select_me_as_end_node_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label lbl_title = sender as Label;
            switch (lbl_title.Name)
            {
                case "Btn_select_me_as_end_node_1":
                    {
                       PublicParameters.SinkNode.GenerateMultipleControlPackets(1, this);

                        break;
                    }
                case "Btn_select_me_as_end_node_10":
                    {
                        PublicParameters.SinkNode.GenerateMultipleControlPackets(10, this);
                        break;
                    }
                //Btn_select_me_as_end_node_1_5sec

                case "Btn_select_me_as_end_node_1_5sec":
                    {
                        PublicParameters.SinkNode.SendPacketTimer.Start();
                        PublicParameters.SinkNode.SendPacketTimer.Tick += SelectMeAsEndNodeAndSendonepacketPer5s_Tick;
                        break;
                    }
            }
        }

        
        
        public void SelectMeAsEndNodeAndSendonepacketPer5s_Tick(object sender, EventArgs e)
        {
            PublicParameters.SinkNode.GenerateMultipleControlPackets(1, this);
        }





        /*** Vistualize****/

        public void ShowID(bool isVis )
        {
            if (isVis) { lbl_Sensing_ID.Visibility = Visibility.Visible; lbl_hops_to_sink.Visibility = Visibility.Visible; }
            else { lbl_Sensing_ID.Visibility = Visibility.Hidden; lbl_hops_to_sink.Visibility = Visibility.Hidden; }
        }

        public void ShowSensingRange(bool isVis)
        {
            if (isVis) Ellipse_Sensing_range.Visibility = Visibility.Visible;
            else Ellipse_Sensing_range.Visibility = Visibility.Hidden;
        }

        public void ShowComunicationRange(bool isVis)
        {
            if (isVis) Ellipse_Communication_range.Visibility = Visibility.Visible;
            else Ellipse_Communication_range.Visibility = Visibility.Hidden;
        }

        public void ShowBattery(bool isVis) 
        {
            if (isVis) Prog_batteryCapacityNotation.Visibility = Visibility.Visible;
            else Prog_batteryCapacityNotation.Visibility = Visibility.Hidden;
        }

        private void btn_update_mini_flow_MouseDown(object sender, MouseButtonEventArgs e)
        {
          
        }
    }
}
