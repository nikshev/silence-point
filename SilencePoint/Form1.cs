using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TradePlatform.MT4.Core;
using TradePlatform.MT4.SDK.API;

namespace SilencePoint
{
    public partial class Form1 : Form
    {

        private int time_series_count = 11;
        private int atr_coefficient = 30000;
        private int SeriesNo;
        private int oldSeriesNo;
        private string message = "";
        private string old_message = "";
        private int GlobalID;
        private double LastNumHigh;
        private BaseConnect baseConnect;
        private MetaTrader4 Terminal;
        private double Point = 0;
        private double PriceHigh = 0;
        private double PriceLow = 0;
        private int Digits = 5;
        private List<QuoteStruct> tempListHigh;
        private List<double> closeHigh;
        FractalClass fractalClassHigh;
        SilencePointArea silencePointArea;
        private int LiveTime = 60;
        private double Profit = 35;
        private double BreakEven = 2.5;
        private double spread_factor = 20;
        private int process_points = 0;
        public Form1()
        {
            InitializeComponent();
            try
            {
                baseConnect = new BaseConnect();
                this.baseConnect.CloseAllStartPoint();
                Bridge.InitializeHosts(true);
                Terminal = Bridge.GetTerminal(6932541, "EURUSD");
                Terminal.QuoteRecieved += EURUSD_QuoteRecieved;
                richTextBox1.Clear();
                richTextBox1.Text += DateTime.Now.ToString() + ": Initialization complete!\r\n";
                tempListHigh = new List<QuoteStruct>();
                closeHigh = new List<double>();
                fractalClassHigh = new FractalClass();
                silencePointArea = new SilencePointArea(baseConnect);
                timer1.Enabled = true;
                timer2.Enabled = true;
            }
            catch (Exception ex)
            {
                richTextBox1.Text += DateTime.Now.ToString() + ": Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
            }


            //SilencePointArea silencePointArea = new SilencePointArea();
            // message += silencePointArea.GetFirstSeriesSilencePoint(17) + "\r\n";

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void backgroundWorker1_DoWork_1(object sender, DoWorkEventArgs e)
        {
            if (this.LastNumHigh != -1.0 && this.GlobalID != 1)
            {
                message += "Start point:" + this.LastNumHigh.ToString() + "; GlobalID=" + this.GlobalID + "\r\n";
                try
                {
                    // FractalClass fractalClassHigh = new FractalClass();
                    //SilencePointArea silencePointArea = new SilencePointArea(baseConnect);
                    SeriesNo = 1;
                    oldSeriesNo = 1;
                    for (int n = 0; n < 4; n++)
                    // for (int n = 0; n < 1;1 n++)
                    {
                        for (int i = 1; i < time_series_count; i++)
                        {
                            fractalClassHigh.CreateFractal(FractalClass.TimeFrame.H1, 24, ((n + 4) * 10) / 100, 0, 24, 0, 6, this.LastNumHigh, (double)i / atr_coefficient);
                            tempListHigh = fractalClassHigh.H1ListQS;

                            this.baseConnect.AddFirstSeries("EURUSD", SeriesNo, tempListHigh, this.GlobalID);

                            oldSeriesNo = SeriesNo;
                            SeriesNo++;
                        }
                    }
                    tempListHigh.Clear();
                    closeHigh.Clear();
                    message += silencePointArea.GetFirstSeriesSilencePoint(this.GlobalID) + "\r\n";
                    this.baseConnect.CloseStartPoint(this.GlobalID);
                }
                catch (Exception ex)
                {
                    message += "Method:  backgroundWorker1; Source:" + ex.Source + "; Exception:" + ex.Message + "\r\n";
                    this.Close();
                }
                finally
                {

                }
            }

        }

        private void MoveBreakEven(MqlHandler mql)
        {

            for (int i = 0; i < mql.OrdersTotal(); i++)
            {
                mql.OrderSelect(i, SELECT_BY.SELECT_BY_POS, POOL_MODES.MODE_TRADES);
                this.WaitForTradeContext(mql);
                if (mql.OrderSymbol() == mql.Symbol() && mql.OrderType() == ORDER_TYPE.OP_SELLLIMIT)
                {
                    //if (OrderMagicNumber() == magic)
                    if (Math.Round(mql.High(0) - mql.OrderOpenPrice(), this.Digits) >= Math.Round(this.BreakEven * 10 * Point, this.Digits) &&
                        Math.Round(mql.OrderStopLoss(), Digits) != Math.Round(mql.OrderOpenPrice(), Digits)
                       && Math.Round(mql.Bid(), Digits) > Math.Round(mql.OrderOpenPrice(), Digits))
                    {
                        if (mql.OrderOpenPrice() < mql.Bid())
                        {
                            mql.OrderModify(mql.OrderTicket(), mql.OrderOpenPrice(), mql.OrderOpenPrice(), mql.OrderTakeProfit(), mql.OrderExpiration(), 0);
                            //Print ("MoveBreakEven()"," High[0]=",High[0]);
                            message += "MoveBreakEven() High[0]=" + mql.High(0).ToString() + " Order ticket=" + mql.OrderTicket();
                        }
                    }
                }
                else
                {
                    if (mql.OrderSymbol() == mql.Symbol() && mql.OrderType() == ORDER_TYPE.OP_BUY)
                    {
                        //if (OrderMagicNumber() == magic)
                        if (Math.Round(mql.OrderOpenPrice() - mql.Low(0), Digits) >= Math.Round(this.BreakEven * 10 * this.Point, Digits)
                           && Math.Round(mql.OrderStopLoss(), Digits) != Math.Round(mql.OrderOpenPrice(), Digits)
                           && Math.Round(mql.Ask(), Digits) < Math.Round(mql.OrderOpenPrice(), Digits))
                        {
                            if (mql.OrderOpenPrice() > mql.Ask())
                            {
                                mql.OrderModify(mql.OrderTicket(), mql.OrderOpenPrice(), mql.OrderOpenPrice(), mql.OrderTakeProfit(), mql.OrderExpiration(), 0);
                                //Print ("MoveBreakEven()"," Low[0]=",Low[0]);
                                message += "MoveBreakEven() Low[0]=" + mql.Low(0).ToString() + " Order ticket=" + mql.OrderTicket();
                            }
                        }
                    }
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            richTextBox1.Text += DateTime.Now.ToString() + ": Generate complete!\r\n";
            richTextBox1.Text += DateTime.Now.ToString() + ": Silence points first series\r\n" + message;
            // timer1.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string log="";
            if (message != "")
            {
                richTextBox1.Text += DateTime.Now.ToString() + ": " + message + "\r\n";
                log +=  DateTime.Now.ToString() + ": " + message + "\r\n";
                message = "";
            }

            string tmpStr = this.baseConnect.GetMessage();
            if (tmpStr != "")
            {
                richTextBox1.Text += DateTime.Now.ToString() + ": Message from baseConnect" + tmpStr + "\r\n";
                log += DateTime.Now.ToString() + ": Message from baseConnect" + tmpStr + "\r\n";
            }
            
            if (log!="")
            {
             using (System.IO.StreamWriter file = new System.IO.StreamWriter(System.Environment.CurrentDirectory + "\\log.txt", true))
              {
                  file.WriteLine(log);
              }
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!this.backgroundWorker1.IsBusy)
            {
                this.LastNumHigh = this.baseConnect.GetNextStartPoint();
                this.GlobalID = this.baseConnect.GetGlobalID();

                if (this.LastNumHigh != -1)
                {
                    if (this.backgroundWorker1 != null)
                    {
                        this.backgroundWorker1.Dispose();
                        this.backgroundWorker1 = null;
                        FlushMemory();
                        this.backgroundWorker1 = new BackgroundWorker();
                    }

                    this.backgroundWorker1.WorkerReportsProgress = true;
                    this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork_1);
                    this.backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
                    this.backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
                    this.backgroundWorker1.RunWorkerAsync();
                }
                else
                {
                    this.GlobalID = -1;
                    if (this.Point != 0)
                    {
                        double need_add_point = this.Point;
                        this.Point = 0;
                        this.baseConnect.AddPoint(need_add_point);
                        message += " Point=" + need_add_point.ToString() + " added... \r\n";
                        this.process_points++;
                        if (this.process_points > 2)
                        {
                            Console.Write(DateTime.Now.ToString() + ": Closing.....\r\n"); 
                            this.Close();
                        }
                        FlushMemory();
                    }
                }
            }
        }
        //Method  EURUSD_QuoteRecieved - listener of metatrader EURUSD,M1
        private void EURUSD_QuoteRecieved(MqlHandler mql)
        {
            //this.MoveBreakEven(mql);
            string tmpMessage = "";
            this.Point = mql.Bid();
            this.DeletAllPending(mql);
            if (!this.backgroundWorker1.IsBusy)
            {
                this.PriceHigh = this.baseConnect.GetHighValue();
                tmpMessage = this.baseConnect.GetMessage();
                if (tmpMessage != "")
                {
                    message += tmpMessage;
                    System.Threading.Thread.Sleep(1000);
                    this.PriceHigh = this.baseConnect.GetHighValue();
                }
                
                this.PriceLow = this.baseConnect.GetLowValue();
                tmpMessage = this.baseConnect.GetMessage();
                if (tmpMessage != "")
                {
                    message += tmpMessage;
                    System.Threading.Thread.Sleep(1000);
                    this.PriceLow = this.baseConnect.GetLowValue();
                }
                if (this.PriceLow == 10)
                    this.PriceLow = 0;
                if (this.PriceHigh != 0)
                {
                    message += "PriceHigh=" + this.PriceHigh.ToString() + ";\r\n";
                    message += "PriceLow=" + this.PriceLow.ToString() + ";\r\n";
                }

                double diff = Math.Round((this.PriceHigh - this.PriceLow) * 1 / mql.Point(), 0);
                if (diff > 300)
                {
                    double local_max = 0;
                    double local_min = mql.Low(0);
                    for (int i = 0; i < 12; i++)
                    {
                        if (mql.High(i) > local_max)
                            local_max = mql.High(i);

                        if (mql.Low(i) < local_min)
                            local_min = mql.Low(i);
                    }
                    message += "High=" + local_max.ToString() + ";\r\n";
                    message += "Low=" + local_min.ToString() + "; PriceLow=" + this.PriceLow.ToString() + "\r\n";

                    this.DeletBuyPending(mql);
                    this.PriceHigh -= (this.spread_factor * mql.Point());
                    double max_cond = this.PriceHigh - (this.spread_factor * mql.Point());
                    if (local_max < max_cond)
                    {
                        if (!(DateTime.Now.DayOfWeek == DayOfWeek.Friday && DateTime.Now.Hour > 20))
                            this.OpenBuyPending(mql);
                        else
                            message += " Week end :) \r\n";
                    }
                    message += "local_max=" + local_max.ToString() + "; max_cond=" + max_cond.ToString()+";\r\n";


                    this.DeletSellPending(mql);
                    this.PriceLow += (this.spread_factor * mql.Point());
                    double min_cond = this.PriceLow + (this.spread_factor * mql.Point());
                    if (local_min > min_cond)
                    {
                        if (!(DateTime.Now.DayOfWeek == DayOfWeek.Friday && DateTime.Now.Hour > 20))
                         this.OpenSellPending(mql);
                        else
                            message += " Week end :) \r\n"; 
                    }
                    message += "local_min=" + local_min.ToString() + "; min_cond=" + min_cond.ToString() + ";\r\n";
                 }
                else if (diff > 0)
                {
                    this.DeletAllPending(mql);
                    message += "PriceHigh=" + this.PriceHigh.ToString() + " PriceLow=" + this.PriceLow.ToString() + " Difference=" + diff.ToString()+"\r\n";
                }
            }
        }


        private int GetPendingOrderCount(MqlHandler mql)
        {
            int ret_val = 0;
            for (int i = mql.OrdersTotal() - 1; i >= 0; i--)
            {
                if (mql.OrderSelect(i, SELECT_BY.SELECT_BY_POS))
                {
                    if (mql.OrderType() == ORDER_TYPE.OP_SELLLIMIT || mql.OrderType() == ORDER_TYPE.OP_SELLSTOP)
                        ret_val++;
                }
            }

            return ret_val;
        }


        private void OpenBuyPending(MqlHandler mql)
        {

            DateTime dt = DateTime.Now;
            DateTime Exp;
            if (dt.Day < 12)
                Exp = dt.AddDays(11);
            else
                Exp = dt;


            int ticket = 0;

            this.WaitForTradeContext(mql);

            if (PriceHigh > 1)
            {
                double price = this.PriceHigh - (this.Profit * mql.Point());
                double tp = this.PriceHigh;
               // double tp = 0;
                ticket = mql.OrderSend(mql.Symbol(), ORDER_TYPE.OP_SELLLIMIT, 0.1, price, 5, 0, tp, "", 0, Exp, 0);
                message += "Send Buy pending order (ticket="+ticket.ToString()+"; Price=" + price.ToString() + "; tp=" + tp.ToString() + ") error:" + mql.GetLastError().ToString() + "\r\n";
                if (ticket == -1)
                {
                    this.WaitForTradeContext(mql);
                    ticket = mql.OrderSend(mql.Symbol(), ORDER_TYPE.OP_SELLLIMIT, 0.1, price, 5, 0, tp, "", 0, Exp, 0);
                    message += "Send Buy pending order (ticket=" + ticket.ToString() + "; Price=" + price.ToString() + "; tp=" + tp.ToString() + ") error:" + mql.GetLastError().ToString() + "\r\n";
                    if (ticket == -1)
                    {
                        this.WaitForTradeContext(mql);
                        ticket = mql.OrderSend(mql.Symbol(), ORDER_TYPE.OP_SELLLIMIT, 0.1, price, 5, 0, tp, "", 0, Exp, 0);
                        message += "Send Buy pending order (ticket=" + ticket.ToString() + "; Price=" + price.ToString() + "; tp=" + tp.ToString() + ") error:" + mql.GetLastError().ToString() + "\r\n";
                    }
                }
            }
        }

        private void DeletSellPending(MqlHandler mql)
        {
            for (int i = mql.OrdersTotal() - 1; i >= 0; i--)
            {
                if (mql.OrderSelect(i, SELECT_BY.SELECT_BY_POS))
                {
                    if (mql.OrderType() == ORDER_TYPE.OP_SELLSTOP)
                        if (GetMinutesDiff(mql.OrderOpenTime()) > LiveTime)
                            if (!mql.OrderDelete(mql.OrderTicket()))
                                message += "Delete sell order " + mql.OrderTicket().ToString() + "error:" + mql.GetLastError().ToString() + "\r\n";
                }
            }
        }

        private void DeletBuyPending(MqlHandler mql)
        {
            for (int i = mql.OrdersTotal() - 1; i >= 0; i--)
            {
                if (mql.OrderSelect(i, SELECT_BY.SELECT_BY_POS))
                {
                    if (mql.OrderType() == ORDER_TYPE.OP_SELLLIMIT)
                        if (this.GetMinutesDiff(mql.OrderOpenTime()) > LiveTime)
                            if (!mql.OrderDelete(mql.OrderTicket()))
                                message += "Delete Buy order " + mql.OrderTicket().ToString() + "error:" + mql.GetLastError().ToString() + "\r\n";
                }
            }
        }

        private void DeletAllPending(MqlHandler mql)
        {
            for (int i = mql.OrdersTotal() - 1; i >= 0; i--)
            {
                if (mql.OrderSelect(i, SELECT_BY.SELECT_BY_POS))
                {
                    if (mql.OrderType() == ORDER_TYPE.OP_SELLLIMIT || mql.OrderType() == ORDER_TYPE.OP_SELLSTOP)
                        if (this.GetMinutesDiff(mql.OrderOpenTime()) > LiveTime)
                            if (!mql.OrderDelete(mql.OrderTicket()))
                                message += "Delete order " + mql.OrderTicket().ToString() + "error:" + mql.GetLastError().ToString() + "\r\n";
                }
            }
        }

        private int GetMinutesDiff(DateTime dt2)
        {
            DateTime dt1 = DateTime.Now;
            int diff = (dt1.Year - dt2.Year) * 365 * 24 * 60;
            diff += (dt1.Month - dt2.Month) * 31 * 24 * 60;
            diff += (dt1.Day - dt2.Day) * 24 * 60;
            diff += (dt1.Hour - dt2.Hour) * 60;
            diff += dt1.Minute - dt2.Minute;
            return diff;
        }


        private void OpenSellPending(MqlHandler mql)
        {
            int ticket = 0;
            DateTime dt = DateTime.Now;
            DateTime Exp;
            if (dt.Day < 12)
                Exp = dt.AddDays(11);
            else
                Exp = dt;

            this.WaitForTradeContext(mql);
            if (PriceLow > 1 && PriceLow != 10)
            {
                double price = this.PriceLow + (this.Profit * mql.Point());
                double tp = this.PriceLow;
               // double tp = 0;
                ticket = mql.OrderSend(mql.Symbol(), ORDER_TYPE.OP_SELLSTOP, 0.1, price, 5, 0, tp, "", 0, Exp, 0);
                message += "Send sell pending  order (ticket=" + ticket.ToString() + "; Price=" + price.ToString() + "; tp=" + tp.ToString() + ")  error:" + mql.GetLastError().ToString() + "\r\n";
                if (ticket == -1)
                {
                    this.WaitForTradeContext(mql);
                    ticket = mql.OrderSend(mql.Symbol(), ORDER_TYPE.OP_SELLSTOP, 0.1, price, 5, 0, tp, "", 0, Exp, 0);
                    message += "Send sell pending  order (ticket=" + ticket.ToString() + "; Price=" + price.ToString() + "; tp=" + tp.ToString() + ")  error:" + mql.GetLastError().ToString() + "\r\n";
                    if (ticket == -1)
                    {
                        this.WaitForTradeContext(mql);
                        ticket = mql.OrderSend(mql.Symbol(), ORDER_TYPE.OP_SELLSTOP, 0.1, price, 5, 0, tp, "", 0, Exp, 0);
                        message += "Send sell pending  order (ticket=" + ticket.ToString() + "; Price=" + price.ToString() + "; tp=" + tp.ToString() + ")  error:" + mql.GetLastError().ToString() + "\r\n";
                    }
                }
            }
        }



        private void WaitForTradeContext(MqlHandler mql)
        {
            while (!mql.IsTradeAllowed())
                System.Threading.Thread.Sleep(1000);
        }

        public static void FlushMemory()
        {
            System.Diagnostics.Process prs = System.Diagnostics.Process.GetCurrentProcess();
            try { prs.MaxWorkingSet = (IntPtr)((int)(prs.MaxWorkingSet + 1)); }
            catch { }
        }

    }



}
