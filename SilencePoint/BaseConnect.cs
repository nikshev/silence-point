using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Data.SQLite;

namespace SilencePoint
{
    class BaseConnect
    {
        private string message = "";
        private int GlobalID = -1;
        private SQLiteConnection connection;
        private SQLiteCommand command;
        private int ConnectionTimeout = 100;
        

        public BaseConnect()
        {
            string connectionString = "Data Source="+
                                      System.Environment.CurrentDirectory +
                                      "\\mydatabase.sqlite;Version=3;New=False;Compress=True;";
            connection = new SQLiteConnection(connectionString);
            command = new SQLiteCommand();
            command.Connection = connection;
        }

        public int GetGlobalID()
        {
            return this.GlobalID;
        }

        public string GetMessage()
        {
            string retval = this.message;
            this.message = "";
            return retval;
        }

        public List<double[]> GetSeries(int GlobalID)
        {
            List<double[]> ret_val = new List<double[]>();

            connection.Open();

            try
            {
                List<int> SeriesNo = new List<int>();
                string queryString = "SELECT Global_id, Series_id " +
                                     " FROM Rates " +
                                     " GROUP BY Global_id, Series_id " +
                                     " HAVING (((Global_id)=" + GlobalID.ToString() + "));";


                command.CommandText = queryString;
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    // Call Read before accessing data.
                    while (reader.Read())
                    {
                        SeriesNo.Add(Int32.Parse(reader[1].ToString()));
                    }
                }

                for (int i = 0; i < SeriesNo.Count; i++)
                {
                    queryString = " SELECT [Series_id], [Close], Global_id " +
                                 " FROM Rates " +
                                 " WHERE ((([Series_id])=" + SeriesNo.ElementAt(i).ToString() + ") " +
                                 " AND ((Global_id)=" + GlobalID.ToString() + "));";
                    // connection.Close();
                    // connection.Open();

                    command.CommandText = queryString;
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        double[] tempVal = new double[24];
                        // Call Read before accessing data.
                        int j = 0;
                        while (reader.Read())
                        {
                            tempVal[j] = Double.Parse(reader[1].ToString());
                            j++;
                        }
                        ret_val.Add(tempVal);
                    }
                }
                SeriesNo.Clear();
                SeriesNo = null;
                connection.Close();
            }
            catch (Exception ex)
            {
                this.message += "Method:   GetSeries(); Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
                connection.Close();
             
            }
            finally
            {
                
            }
            return ret_val;
        }

        public List<CrossPoint> GetCrossPointList(int GlobalID)
        {
            List<CrossPoint> ret_val = new List<CrossPoint>();


            string queryString = "SELECT Global_id, Point, Count " +
                                 " FROM CrossPoints " +
                                 " WHERE (((Global_id)=" + GlobalID.ToString() + ")); ";
            try
            {
                connection.Open();
                command.CommandText = queryString;
                CrossPoint crossPoint;
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    // Call Read before accessing data.
                    while (reader.Read())
                    {
                        crossPoint = new CrossPoint(Double.Parse(reader[1].ToString()), Double.Parse(reader[2].ToString()));
                        ret_val.Add(crossPoint);
                    }
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                this.message += "Method: GetCrossPointList(); Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
                connection.Close();
            }
            finally
            {
              
            }

            return ret_val;
        }

        public void SetCrossPointList(List<CrossPoint> PointList, int GlobalID)
        {

            try
            {
                string queryString = " DELETE " +
                                     " FROM CrossPoints " +
                                     " WHERE (((Global_id)=" + GlobalID.ToString() + ")); ";
                connection.Open();
                command.CommandText = queryString;
                command.ExecuteNonQuery();

                CrossPoint crossPoint;
                for (int i = 0; i < PointList.Count; i++)
                {
                    crossPoint = PointList.ElementAt(i);
                    queryString = " INSERT INTO CrossPoints (Global_id, Point, [Count] ) " +
                                  " SELECT " + GlobalID.ToString() + " AS Gid, " +
                                  crossPoint.point.ToString() + " AS P, " +
                                  crossPoint.count.ToString() + " AS C;";
                    command.CommandText = queryString;
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                this.message += "Method:  SetCrossPointList(); Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
                connection.Close();
            }
            finally
            {
            
            }
        }

        public void AddFirstSeries(string Currency, int SeriesNo, List<QuoteStruct> Series, int GlobalNo)
        {

            try
            {
                QuoteStruct tempQuote;
                connection.Open();
                for (int i = 0; i < Series.Count; i++)
                {
                    tempQuote = Series.ElementAt(i);
                    string queryString = "INSERT INTO Rates ( Series_id, [Currency], [Open], High, Low, [Close], [Global_id]) " +
                                         "SELECT " + SeriesNo.ToString() + " AS id, '" + Currency + "' AS Cur, " +
                                         tempQuote.Open.ToString() + " AS O, " +
                                         tempQuote.High.ToString() + " AS H, " +
                                         tempQuote.Low.ToString() + " AS L, " +
                                         tempQuote.Close.ToString() + " AS C, " +
                                         GlobalNo.ToString() + " AS G;";


                    command.CommandText = queryString;
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                this.message += "Method: AddFirstSeries(); Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
                connection.Close();
            }
            finally
            {
              
            }
        }

        public double GetNextStartPoint()
        {

            double ret_val = -1.0;
            this.GlobalID = -1;
            try
            {
                string queryString = "SELECT id, StartPoint, Complete " +
                                     " FROM StartPoints " +
                                     " WHERE (((Complete)=0)) " +
                                     " ORDER BY id LIMIT 1 ;";

                connection.Open();
                command.CommandText = queryString;
                using (SQLiteDataReader reader = command.ExecuteReader())
                {

                    // Call Read before accessing data.
                    while (reader.Read())
                    {
                        ret_val = Double.Parse(reader[1].ToString());
                        this.GlobalID = Int32.Parse(reader[0].ToString());
                    }
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                message += "Method:  GetNextStartPoint(); Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
                connection.Close();
            }
            finally
            {
               
            }
            return ret_val;
        }

        public void CloseStartPoint(int Global)
        {
            try
            {
                string queryString = "UPDATE StartPoints SET Complete = 1" +
                                     " WHERE (((id)=" + Global.ToString() + "));";

                connection.Open();
                command.CommandText = queryString;
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                message += "Method CloseStartPoint; Source:" + ex.Source + "; Exception: " + ex.Message + "\r\n";
                connection.Close();
            }
            finally
            {
              
            }
        }

        public void CloseAllStartPoint()
        {

            try
            {
                string queryString = "UPDATE StartPoints SET Complete = 1, " +
                                     "Processed_h = 1, Processed_l = 1;";


                connection.Open();
                command.CommandText = queryString;
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                message += "Method: CloseAllStartPoint(); Source:" + ex.Source + "; Exception: " + ex.Message + "\r\n";
                connection.Close();
            }
            finally
            {
               
            }
        }

        public double GetLowValue()
        {
            double ret_val = 0;
            int Global = -1;

            try
            {
                connection.Open();
                string queryString = "SELECT id, Complete, Processed_l " +
                                     " FROM StartPoints " +
                                     " WHERE (((Complete)=1) AND ((Processed_l)=0)) " +
                                     " ORDER BY id DESC LIMIT 1;";

                command.CommandText = queryString;
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        Global = Int32.Parse(reader[0].ToString());
                }

                if (Global > -1)
                {
                    queryString = " SELECT Global_id, Point, Count " +
                                  " FROM CrossPoints " +
                                  " WHERE (((Global_id)=" + GlobalID.ToString() + ") AND " +
                                  " ((Count)<50 And (Count)>29)) " +
                                  " ORDER BY Count DESC; ";

                    command.CommandText = queryString;
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        double min = 10;
                        while (reader.Read())
                            if (min > Double.Parse(reader[1].ToString()))
                                min = Double.Parse(reader[1].ToString());
                        ret_val = min;
                    }

                    //if (min != 10)
                    //{
                    queryString = "UPDATE StartPoints SET Processed_l = 1 " +
                                  " WHERE (((id)=" + GlobalID.ToString() + ")); ";

                    command.CommandText = queryString;
                    command.ExecuteNonQuery();
                    //}

                }
                connection.Close();
            }
            catch (Exception ex)
            {
                message += "Method GetLowValue() Source:" + ex.Source + "; Exception: " + ex.Message;
                connection.Close();
            }
            finally
            {
                connection.Close();
            }
            return ret_val;
        }

        public double GetHighValue()
        {
            double ret_val = 0;
            int GlobalID = -1;

            try
            {
                connection.Open();
                string queryString = "SELECT id, Complete, Processed_h " +
                                     " FROM StartPoints " +
                                     " WHERE (((Complete)=1) AND ((Processed_h)=0)) " +
                                     " ORDER BY id DESC LIMIT 1;";

                command.CommandText = queryString;
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        GlobalID = Int32.Parse(reader[0].ToString());
                }

                if (GlobalID > -1)
                {
                    queryString = " SELECT Global_id, Point, Count " +
                                  " FROM CrossPoints " +
                                  " WHERE (((Global_id)=" + GlobalID.ToString() + ") AND " +
                                  " ((Count)<50 And (Count)>29)) " +
                                  " ORDER BY Count DESC; ";

                    command.CommandText = queryString;
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        double max = 0;
                        while (reader.Read())
                            if (max < Double.Parse(reader[1].ToString()))
                                max = Double.Parse(reader[1].ToString());
                                ret_val = max;
                    }
                }

                //if (max>0) {
                queryString = "UPDATE StartPoints SET Processed_h = 1 " +
                              " WHERE (((StartPoints.id)=" + GlobalID.ToString() + ")); ";

                command.CommandText = queryString;
                command.ExecuteNonQuery();
                //}
                connection.Close();
            }
            catch (Exception ex)
            {
                message += "Method GetHighValue() Source:" + ex.Source + "; Exception: " + ex.Message;
                connection.Close();
            }
            finally
            {
                connection.Close();
            }
            return ret_val;
        }

        public void AddPoint(double point)
        {
            try
            {
                string queryString = "INSERT INTO StartPoints (StartPoint, Complete, Processed_h, Processed_l ) " +
                                     " SELECT " + point.ToString() + " AS SP, 0 AS C, 0 AS P_h, 0 AS P_l";
                connection.Open();
                command.CommandText = queryString;
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception ex)
            {
                message += "Method AddPoint() Source:" + ex.Source + "; Exception: " + ex.Message + "\r\n";
                connection.Close();
            }
        }

    }
}
