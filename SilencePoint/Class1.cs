using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data.OleDb;

namespace SilencePoint
{
    public struct CrossPoint
    {
        public double point;
        public double count;
        public CrossPoint(double point, double count)
        {
            this.point = point;
            this.count = count;
        }
    }

    public struct DPoint
    {
        public double X;
        public double Y;
        public DPoint(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

    class SilencePointArea:IDisposable
    {
        private string message = "";
        private BaseConnect baseConnect;
        private List<CrossPoint> CrossPointList;
        private List<double[]> Series;
        //  private List<CrossPoint> crossPointListFirstSeries;
        // private List<CrossPoint> crossPointListSecondSeries; 


        public SilencePointArea(BaseConnect bConnect)
        {
            this.baseConnect = bConnect;
            //  crossPointListFirstSeries = new List<CrossPoint>();
            //   crossPointListSecondSeries = new List<CrossPoint>();
        }

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            this.baseConnect = null;
          
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
          //  GC.SuppressFinalize(this);
        }

        

        private void FindFirstSeriesCrosses(int GlobalID)
        {
           
            CrossPointList = new List<CrossPoint>();
            Series = this.baseConnect.GetSeries(GlobalID);
            for (int i = 0; i < Series.Count; i++)
            {
                double[] tempValForSearch = Series.ElementAt(i);
                for (int j = 1; j < 24; j++)
                {

                    for (int k = 0; k < Series.Count; k++)
                    {
                        if (k != i)
                        {
                            try
                            {
                                double[] tempValSearch = Series.ElementAt(k);
                                DPoint p1 = new DPoint(j - 1, tempValForSearch[j - 1]);
                                DPoint p2 = new DPoint(j, tempValForSearch[j]);
                                DPoint p3 = new DPoint(j - 1, tempValSearch[j - 1]);
                                DPoint p4 = new DPoint(j, tempValSearch[j]);
                                AddCrossPointFS(Crossing(p1, p2, p3, p4), GlobalID);
                            }
                            catch (Exception ex)
                            {
                                this.message += "Method: FindFirstSeriesCrosses(); Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
                            }
                            finally
                            {
                              
                            }
                        }
                    }
                }
            }
            Series.Clear();
            Series = null;
            this.baseConnect.SetCrossPointList(CrossPointList, GlobalID);
            CrossPointList.Clear();
            CrossPointList = null;
        
        }

        private void AddCrossPointFS(DPoint point, int GlobalID)
        {
            try
            {
                CrossPoint crossPoint;
                if (!(point.X == 0 && point.Y == 0))
                {
                    //List<CrossPoint> CrossPointList = this.baseConnect.GetCrossPointList(GlobalID);
                    int CrossPointCount = CrossPointList.Count;

                    if (CrossPointCount > 0)
                    {
                        bool Added = false;
                        for (int i = 0; i < CrossPointCount; i++)
                        {
                            if (CompareDouble(Math.Round(CrossPointList.ElementAt(i).point, 5), Math.Round(point.Y, 5)))
                            {
                                crossPoint = CrossPointList[i];
                                crossPoint.count++;
                                CrossPointList[i] = crossPoint;
                                Added = true;
                            }
                        }
                        if (!Added)
                        {
                            crossPoint = new CrossPoint(Math.Round(point.Y, 5), 1.0);
                            CrossPointList.Add(crossPoint);
                        }
                    }
                    else
                    {
                        crossPoint = new CrossPoint(Math.Round(point.Y, 5), 1.0);
                        CrossPointList.Add(crossPoint);
                    }

                  //  this.baseConnect.SetCrossPointList(CrossPointList, GlobalID);
                    //CrossPointList.Clear();
                   // CrossPointList = null;
                }
            }
            catch (Exception ex)
            {
                this.message += "Method:  AddCrossPointFS(); Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
            }
            finally
            {

            }
        }

                
        public string GetFirstSeriesSilencePoint(int GlobalID)
        {
            string retVal = "";
            try
            {
                FindFirstSeriesCrosses(GlobalID);
                //List<CrossPoint> CrossPointList = this.GetCrossPointList(GlobalID);
                //int CrossPointCount = CrossPointList.Count;
                if (this.message != "")
                    retVal += "Message:" + this.message + ";\r\n";
                retVal += "Global ID=" + GlobalID.ToString() + " cross points founded;\r\n";
                /*for (int i = 0; i < CrossPointCount; i++)
                {
                    retVal += "SP=" + CrossPointList.ElementAt(i).point.ToString();
                    retVal += "; Count=" + CrossPointList.ElementAt(i).count.ToString();
                    retVal += ";\r\n";
                }*/
            }
            catch (Exception ex)
            {
                retVal += "Method: AddFirstSeries(); Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
            }
            finally
            {
               
            }

            return retVal;
        }

      
        private bool CompareDouble(double arg1, double arg2)
        {
            bool retVal = false;
            double tempVal1 = Math.Round((arg1 - 0.00005), 5);
            double tempVal2 = Math.Round(arg2, 5);
            for (int i = 0; i < 10; i++)
            {
                if (Math.Round(tempVal1, 5) == Math.Round(tempVal2, 5))
                    retVal = true;
                tempVal1 += 0.00001;
                tempVal1 = Math.Round(tempVal1, 5);
            }
            return retVal;
        }

        public DPoint Crossing(DPoint p1, DPoint p2, DPoint p3, DPoint p4)
        {
            /*if (p3.X == p4.X)   // вертикаль
            {*/
            double y = p1.Y + ((p2.Y - p1.Y) * (p3.X - p1.X)) / (p2.X - p1.X);
            if (y > Math.Max(p3.Y, p4.Y) || y < Math.Min(p3.Y, p4.Y) || y > Math.Max(p1.Y, p2.Y) || y < Math.Min(p1.Y, p2.Y))   // если за пределами отрезков
                return new DPoint(0, 0);
            else
                return new DPoint(p3.X, y);
            /* }
             else            // горизонталь
             {
                 double x = p1.X + ((p2.X - p1.X) * (p3.Y - p1.Y)) / (p2.Y - p1.Y);
                 if (x > Math.Max(p3.X, p4.X) || x < Math.Min(p3.X, p4.X) || x > Math.Max(p1.X, p2.X) || x < Math.Min(p1.X, p2.X))   // если за пределами отрезков
                     return new DPoint(0, 0);
                 else
                     return new DPoint(x, p3.Y);
             }*/
        }
    }

}
