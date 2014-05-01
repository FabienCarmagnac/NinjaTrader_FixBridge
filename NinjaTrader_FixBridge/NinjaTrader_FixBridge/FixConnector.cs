using NinjaTrader.Cbi;
using NinjaTrader.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    

    /// <summary>
    /// A FixConnector provides order management system and market data for a symbol
    /// </summary>
    [Description("FixConnector")]
    public class FixConnector : Strategy
    {

        object locker_ = new object();


        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {

            

            CalculateOnBarClose = false;
            Unmanaged = true;
            RealtimeErrorHandling = RealtimeErrorHandling.TakeNoAction;

            List<MarketDataDef> ls = GetSymbols();
            for(int i=Instruments.Length;i<ls.Count;i++)
                Add(ls[i].symbol, PeriodType.Tick, 1);


        }


        protected override void OnStartUp()
        {
            if (string.IsNullOrEmpty(BridgeName))
                throw new Exception("BridgeName cannot be empty.");
            try
            {

                Info(BridgeName, "[OnStartUp] Starting FixConnector for symbols [ " + m_symbols + " ]");
                NinjaTrader_FixBridge.QuickFixStaticAcceptor.OnStartUp(this);
                Info(BridgeName, "[OnStartUp] FixConnector started.");

            }
            catch (Exception e)
            {
                Error(BridgeName, "[OnStartUp] Exception OnStartUp : " + e.Message + ", source:" + e.Source);
                Error(BridgeName, "[OnStartUp] Exception OnStartUp : " + e.ToString());
                Error(BridgeName, "[OnStartUp] Exception OnStartUp : " + e.StackTrace);
                NinjaTrader_FixBridge.QuickFixStaticAcceptor.OnTermination(this);
                throw e;

            }
        }

        #region Tepp call coming from FIX engine
        public void FromFIX_SendOrder(NinjaTrader_FixBridge.OrderFixBridge orderFixBridge, int index, OrderAction orderAction, OrderType orderType, int quantity, double limitPrice, double stopPrice, string signalName)
        {
            lock (locker_)
            {
                try
                {
                    orderFixBridge.Order = this.SubmitOrder(index, orderAction, orderType, quantity, limitPrice, stopPrice, string.Empty, signalName);
                }catch(Exception e)
                {
                    orderFixBridge.Order = null;
                    this.Error("[FromFIX_SendOrder]", e.ToString());
                }
            }
        }

        public void FromFIX_CancelOrder(IOrder p_order)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(obj =>
            {
                lock (locker_)
                    CancelOrder(p_order);
            }));
        }
        public void FromFIX_ChangeOrder(IOrder p_order, int quantity, double limitPrice, double stopPrice)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(obj =>
            {
                lock (locker_)
                    ChangeOrder(p_order, quantity, limitPrice, stopPrice);
            }));
        }
        #endregion
        protected override void OnExecution(IExecution execution)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(obj =>
            {
                lock (locker_)
                    NinjaTrader_FixBridge.QuickFixStaticAcceptor.FromNT_OnOrderUpdate(execution.Order, execution);
            }));
        }

        protected override void OnOrderUpdate(IOrder order)
        {
            // exception : we ignore this notification when order is Filled or PartFilled, we wait the call back via OnExecution
            if (order.OrderState == OrderState.Filled || order.OrderState == OrderState.PartFilled)
                return;
            
            ThreadPool.QueueUserWorkItem(new WaitCallback(obj =>
            {
                lock (locker_)
                {
                    NinjaTrader_FixBridge.QuickFixStaticAcceptor.FromNT_OnOrderUpdate(order, null);
                }
            }));
        }
        protected override void OnTermination()
        {
            Info(BridgeName, "[OnTermination] Stopping FixConnector  ...");
            lock (locker_)
            {
                NinjaTrader_FixBridge.QuickFixStaticAcceptor.OnTermination(this);
            }
            Info(BridgeName, "[OnTermination] FixConnector stopped !");
        }
     

        #region Properties
        [Description("File absolute path to load sessions FIX parameters. This defines the instance of QuickFix bridge, since you can have several.")]
        [GridCategory("Parameters")]
        public string ConfigFileName
        {
            get { return m_configFileName; }
            set { m_configFileName = value; }
        }
        [Description("Bridge name to identify it in logs")]
        [GridCategory("Parameters")]
        public string BridgeName
        {
            get { return m_bridgeName; }
            set { m_bridgeName = value; }
        }
        [Description("List of symbols autorised to trade, separated by ','")]
        [GridCategory("Parameters")]
        public string Symbols
        {
            get { return m_symbols; }
            set { m_symbols = value; }
        }
        [Description("Activate trace (0/1). For debugging only")]
        [GridCategory("Parameters")]
        public int ActiveTrace
        {
            get { return m_activeTrace; }
            set { m_activeTrace = value; }
        }

        #endregion

        #region utils

        public void Info(string source, string message)
        {
            Log("[" + source + "] " + message, NinjaTrader.Cbi.LogLevel.Information);
        }
        public void Warn(string source, string message)
        {
            Log("[" + source + "] " + message, NinjaTrader.Cbi.LogLevel.Warning);
        }
        public void Error(string source, string message)
        {
            Log("[" + source + "] " + message, NinjaTrader.Cbi.LogLevel.Error);
        }
        public void Alert(string source, string message)
        {
            Log("[" + source + "] " + message, NinjaTrader.Cbi.LogLevel.Alert);
        }
        public void Trace(string source, string message)
        {
            if(ActiveTrace>0)
                Log("[ TRACE " + source + "] " + message, NinjaTrader.Cbi.LogLevel.Information);
        }

        #endregion


        #region refered to market data
        protected override void OnBarUpdate()
        {

            try
            {
                MarketDataDef mdd = GetSymbols()[BarsInProgress];
                if (CurrentBars[BarsInProgress] >= 1)
                {
                    mdd.bid = GetCurrentBid(BarsInProgress);
                    mdd.ask = GetCurrentAsk(BarsInProgress);
                    mdd.qty_bid = GetCurrentBidVolume(BarsInProgress);
                    mdd.qty_ask = GetCurrentAskVolume(BarsInProgress);
                    mdd.notify();
                }
            }
            catch (Exception)
            { }
        }
        #endregion

        #region refered to orders
        public List<MarketDataDef> GetSymbols()
        {
            if (m_symbols_list == null)
            {
                m_symbols_list = MarketDataDef.Read(this, Symbols);
                m_symbols_list.InsertRange(0, Instruments.Select(i => new MarketDataDef(this, i.FullName)));
                for (int i = 0; i < m_symbols_list.Count; i++)
                    m_symbols_list[i].internal_barinprogress = i;
            }
           

            return m_symbols_list;
        }

        #endregion
    
        #region variables

        string m_configFileName = @"D:\trading\tepp\nt7fixbridge\acceptor.cfg";
        string m_symbols = "ES ##-##,GC ##-##,CL ##-##";
        string m_bridgeName = (new Random()).Next(1000000000).ToString("000000000");
        
        List<MarketDataDef> m_symbols_list = null;
        int m_activeTrace = 1;
        #endregion


    }
}