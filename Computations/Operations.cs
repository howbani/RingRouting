using RingRouting.Constructor;
using RingRouting.Dataplane;
using System;
using System.Collections.Generic;
using System.Windows;

namespace RingRouting.Intilization
{
    public class Operations
    {
        public static int Orientation(Point p1, Point p2, Point p3)
        {
            // See 10th slides from following link  
            // for derivation of the formula 
            double v = (p2.Y - p1.Y) * (p3.X - p2.X) - (p2.X - p1.X) * (p3.Y - p2.Y);
            int val = Convert.ToInt32(v);
            if (val == 0) return 0; // colinear 

            // clock or counterclock wise 
            return (val > 0) ? 1 : 2;
        }
        public static Point GetDirectionToRingNodes(Sensor source)
        {
            Point src = source.CenterLocation;
            Point center = PublicParameters.networkCenter;
            double xDif = center.X - src.X;
            double yDif = center.Y - src.Y;
            double destX = 0;
            double destY = 0;
            // first up or down 
            if (yDif < 0)
            {
                //it means it down
                destY--;
            }
            else if (yDif > 0)
            {
                //its up
                destY++;
            }
            if (xDif < 0)
            {
                destX++;
            }
            else if (xDif > 0)
            {
                destX--;
            }
            destX = Math.Round(destX * PublicParameters.clusterRadius);
            destY = Math.Round(destY * PublicParameters.clusterRadius);
            Point destination = new Point(source.CenterLocation.X + destX, source.CenterLocation.Y + destY);
            return destination;
        }

        /// <summary>
        /// Is a certain point inside a polygon
        /// Polygon is a list of point (each point is a vertisy) 
        /// </summary>
        /// <param name="polygon">list of points that form the polygon</param>
        /// <param name="MyPoint">The point of intrest</param>
        /// <returns></returns>
        public static bool PointInPolygon(List<Sensor> poly, Sensor myPoint)
        {
            Sensor anchor = Ring.PointZero;
            bool isInside = false;
            int j = poly.Count - 1;
            Point start = myPoint.CenterLocation;
            Point end = new Point(start.X, 0);

            if (start.Y > anchor.CenterLocation.Y)
            {
                return false;
            }

            for (int i = 0; i < poly.Count; i++)
            {

                Point verticyI = new Point(poly[i].CenterLocation.X, poly[i].CenterLocation.Y);
                Point verticyJ = new Point(poly[j].CenterLocation.X, poly[j].CenterLocation.Y);

                bool intersect = (((isClockwise(start, end, verticyJ) != isClockwise(start, end, verticyI)) && (isClockwise(verticyI, verticyJ, start) != isClockwise(verticyI, verticyJ, end))));

                // bool intersect = ((verticyI.Y > myPoint.Y) != (verticyJ.Y > myPoint.Y))
                // && (myPoint.X < (verticyJ.X - verticyI.X) * (myPoint.Y - verticyI.Y) / (verticyJ.Y - verticyI.Y) + verticyI.X);

                if (intersect)
                {
                    isInside = !isInside;
                }
                j = i;

            }
            return isInside;
        }
        public static int isClockwise(Point a, Point b, Point c)
        {
            double value = ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));
            if (value > 0)
            {
                return 1;
            }
            else if (value == 0)
            {
                return 0;
            }
            else { return 2; }

        }


        public static int ConvertAngleToDirection(int angle){
            int part = getAnglePart(angle);
            int direction = convertToDirection(getNearestAngle(angle, part));
            return direction;
        }
        private static int getAnglePart(int angle)
        {
            int part = 0;
            if (angle <= 90 && angle >= 0)
            {
                //1
                part = 1;
            }
            else if (angle > 90 && angle <= 180)
            {
                //2
                part = 2;
            }
            else if (angle > 180 && angle <= 270)
            {
                //3
                part = 3;
            }
            else if (angle > 270 && angle <= 360)
            {
                //4
                part = 4;
            }

            return part;
        
        }
        private static int getNearestAngle(double angle, int part)
        {
            int direction = 0;
            int to = 90 * part;
            int from = to - 90;
            int middle = (to + from) / 2;
            int middleUp = (middle + to) / 2;
            int middleDown = (middle + from) / 2;

            if (angle > middle)
            {
                //between to and middle 
                if (angle > middleUp)
                {
                    //Return to
                    direction = to;
                }
                else
                {
                    //return middle 
                    direction = middle;
                }
            }
            else
            {
                if (angle > middleDown)
                {
                    //return middle 
                    direction = middle;
                }
                else
                {
                    direction = from;
                }
            }
            return direction;
        }
        private static int convertToDirection(int dir)
        {
            switch (dir)
            {
                case 0:
                    return 1;
                case 180:
                    return 2;
                case 90:
                    return 3;
                case 270:
                    return 4;
                case 360:
                    return 1;
                case 45:
                    return 5;
                case 135:
                    return 7;
                case 315:
                    return 6;
                case 225:
                    return 8;
            }
            return 0;

        }

        public static List<int> PacketPathToIDS(String path)
        {
            String[] strIDS = path.Split('>');
            List<int> ids = new List<int>();

            foreach (String id in strIDS)
            {
                int x = Int16.Parse(id);
                ids.Add(x);
            }
            return ids;

        }
        public static double kmphToTimerInterval(double speed)
        {
            if (speed <= 0)
            {
                return 0;
            }
            double disInMeter = speed * 1000;
            double timeInSec = 3600;
            double interval = timeInSec / disInMeter;
            return interval;

        }
        public static double factorial(double n)
        {
            double results = 1;
            while (n != 1)
            {
                results *= n;
                n -= 1;
            }
            return results;
        }
        public static double factorial(double n, double r, double dif)
        {
            double untill = 0;
            double divide = 0;
            if (r > dif)
            {
                untill = (n - r);
                divide = dif;
            }
            else
            {

                untill = dif;
                divide = r;
            }
            double result = 1;
            divide = factorial(divide);
            while (n != untill)
            {
                result = result * n;
                n = n - 1;
            }
            return result / divide;
        }

        public static double nChooseR(double n, double r)
        {
            if (n == r)
            {
                return 1;
            }
            double dif = n - r;
            double solution = factorial(n, r, dif);

            return solution;


        }


        public static double DistanceBetweenTwoSensors(Sensor sensor1, Sensor sensor2)
        {
            try
            {
                double dx = (sensor1.CenterLocation.X - sensor2.CenterLocation.X);
                dx *= dx;
                double dy = (sensor1.CenterLocation.Y - sensor2.CenterLocation.Y);
                dy *= dy;
                return Math.Sqrt(dx + dy);
            }
            catch (Exception e)
            {
                Console.WriteLine("Distance between sensors returned an exception: "+e.Message);
               // Console.WriteLine(e.Message);
                return 0;
            }
           
        }

        public static double DistanceBetweenTwoPoints(Point p1, Point p2)
        {
            double dx = (p1.X - p2.X);
            dx *= dx;
            double dy = (p1.Y - p2.Y);
            dy *= dy;
            return Math.Sqrt(dx + dy);
        }

        /// <summary>
        /// the communication range is overlapped.
        /// 
        /// </summary>
        /// <param name="sensor1"></param>
        /// <param name="sensor2"></param>
        /// <returns></returns>
        public static bool isOverlapped(Sensor sensor1, Sensor sensor2)
        {
            bool re = false;
            double disttance = DistanceBetweenTwoSensors(sensor1, sensor2);
            if (disttance < (sensor1.ComunicationRangeRadius + sensor2.ComunicationRangeRadius))
            {
                re = true;
            }
            return re;
        }

        /// <summary>
        /// check if j is within the range of i.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static bool isInMySensingRange(Sensor i, Sensor j)
        {
            bool re = false;
            double disttance = DistanceBetweenTwoSensors(i, j);
            if (disttance <= (i.VisualizedRadius))
            {
                re = true;
            }
            return re;
        }

        /// <summary>
        /// Returns the perpendicular distance between a line (between two points) and a point
        /// 
        /// </summary>
        /// <param name="src">Source Node Center Location</param>
        /// <param name="dest">Destination Node Center Location</param>
        /// <param name="candi">Candidate Node (Outside Point)</param>
        /// <returns>The value of the distance</returns>

        public static double GetPerpindicularDistance(Point src, Point dest, Point candi)
        {
            
            double srcAndDesDis = DistanceBetweenTwoPoints(src, dest);
            double dist = (candi.X*(dest.Y - src.Y) - (candi.Y * (dest.X - src.X)) + ((dest.X * src.Y) - (dest.Y * src.X)));
            dist = Math.Abs(dist) / srcAndDesDis;
            return dist;
        }

        public static double GetDirectionAngle(Point source, Point destination, Point forwarder)
        {
            double angle = 0;
            double srcForwarder = DistanceBetweenTwoPoints(source, forwarder);
            double srcDest = DistanceBetweenTwoPoints(source, destination);
            double forwarderDest = DistanceBetweenTwoPoints(destination, forwarder);
            double sum = (srcDest * srcDest) + (srcForwarder * srcForwarder)-(forwarderDest * forwarderDest);
            sum /= (2 * srcDest * srcForwarder);
            angle = Math.Acos(sum);
           
            return angle;
        }
        public static double GetAngleDotProduction(Point i, Point j, Point d)
        {
            double axb = (j.X - i.X) * (d.X - i.X) + (j.Y - i.Y) * (d.Y - i.Y);
            double disMul = DistanceBetweenTwoPoints(i, d) * DistanceBetweenTwoPoints(i, j);
            double angale = Math.Acos(axb / disMul);
            double norAngle = angale / Math.PI;
            if (norAngle <= 0.5)
                return (Math.Pow(((1 - (norAngle * Math.Exp(norAngle))) / (1 + (norAngle * Math.Exp(norAngle)))), 1)); // heigher pri
            else
                return (Math.Pow(((1 - (norAngle * Math.Exp(norAngle))) / (1 + (norAngle * Math.Exp(norAngle)))), 3)); // smaller pri
        }
        public static double GetPerpendicularProbability(Point psource, Point pj, Point pdestination)
        {
            double past = Math.Abs(((pdestination.Y - psource.Y) * pj.X) - ((pdestination.X - psource.X) * pj.Y) + (pdestination.X * psource.Y) - (pdestination.Y * psource.X));
            double sbDis = DistanceBetweenTwoPoints(psource, pdestination);
            double perDis = past / sbDis;

            // dist: if there is a mistake, then we should consider the normalization.
            double pr = Math.Exp(-perDis);
            return pr;
        }


        /// <summary>
        /// commnication=sensing rang*2
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static bool isInMyComunicationRange(Sensor i, Sensor j)
        {
            bool re = false;
            double disttance = DistanceBetweenTwoSensors(i, j);
            if (disttance <= (i.ComunicationRangeRadius))
            {
                re = true;
            }
            return re;
        }

        public static double FindNodeArea(double com_raduos)
        {
            return Math.PI * Math.Pow(com_raduos, 2);
        }

        /// <summary>
        /// n!
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static double Factorial(int n)
        {
            long i, fact;
            fact = n;
            for (i = n - 1; i >= 1; i--)
            {
                fact = fact * i;
            }
            return fact;
        }

        /// <summary>
        /// combination 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static double Combination(int n, int k)
        {
            if (k == 0 || n == k) return 1;
            if (k == 1) return n;
            int dif = n - k;
            int max = Max(dif, k);
            int min = Min(dif, k);

            long i, bast;
            bast = n;
            for (i = n - 1; i > max; i--)
            {
                bast = bast * i;
            }
            double mack = Factorial(min);
            double x = bast / mack;
            return x;
        }


        private static int Max(int n1,int n2) { if (n1 > n2) return n1; else return n2; }
        private static int Min(int n1, int n2) { if (n1 < n2) return n1; else return n2; } 
    }
}
