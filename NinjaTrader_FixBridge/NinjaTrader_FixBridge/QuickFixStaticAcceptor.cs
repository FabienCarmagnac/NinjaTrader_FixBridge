using NinjaTrader;
using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NinjaTrader_FixBridge
{
  
    static public class QuickFixStaticAcceptor
    {
        public const string Name = "QuickFixStaticAcceptor";
        
        static object locker_ = new object();
        static NinjaTrader_FixBridge.QuickFixApp m_app = null;
        static QuickFix.ThreadedSocketAcceptor m_acceptor = null;
        static InstrumentMarketProvider s_fixConnectors = new InstrumentMarketProvider();

        public static string ConfigFileName
        {
            get;
            private set;
        }

        public static void FromFIX_CancelOrder(IOrder p_order)
        {
            lock (locker_)
            {
                MarketDataDef mdd = s_fixConnectors.GetMarketDataDefForSymbol(p_order.Instrument.FullName);
                if (mdd == null)
                {
                    throw new Exception("No FixConnector available for symbol '" + p_order.Instrument.FullName + "', cant send cancel request for order " + p_order.OrderId);
                }
                mdd.fixConnector.FromFIX_CancelOrder(p_order);
            }
        }
        public static void FromFIX_ChangeOrder(IOrder p_order, int quantity, double limitPrice, double stopPrice)
        {
            lock (locker_)
            {
                MarketDataDef mdd = s_fixConnectors.GetMarketDataDefForSymbol(p_order.Instrument.FullName);
                if (mdd == null)
                {
                    throw new Exception("No FixConnector available for symbol '" + p_order.Instrument.FullName + "', cant send order modification for order " + p_order.OrderId + ". Gonna try with any FixConnector ");
                }
                mdd.fixConnector.FromFIX_ChangeOrder(p_order, quantity, limitPrice, stopPrice);
            }
        }

        public static void FromNT_OnOrderUpdate(IOrder order, IExecution exec)
        {
            m_app.FromNT_OnOrderUpdate(order, exec);
        }
        public static void FromFIX_SendOrder(OrderFixBridge orderFixBridge, string instrumentId, OrderAction orderAction, OrderType orderType, int quantity, double limitPrice, double stopPrice, string signalName)
        {
            lock (locker_)
            {
                MarketDataDef mdd = s_fixConnectors.GetMarketDataDefForSymbol(instrumentId);
                if (mdd == null)
                {
                    throw new Exception("No FixConnector available for symbol to create order on '" + instrumentId + "'.");
                }

                mdd.fixConnector.FromFIX_SendOrder(orderFixBridge, mdd.internal_barinprogress, orderAction, orderType, quantity, limitPrice, stopPrice, signalName);                
            }
        }
        /*
         * 
         * instrument quickfix_id => list of fixconnectors
         */
        static public void OnStartUp(NinjaTrader.Strategy.FixConnector fixConnector)
        {
            lock (locker_)
            {
            // adding anyway
            s_fixConnectors.Add(fixConnector);

            try
            {
                // start ?
                if (string.IsNullOrEmpty(ConfigFileName))
                {

                    if (string.IsNullOrEmpty(fixConnector.ConfigFileName))
                    {
                        // cannot start
                        s_fixConnectors.Warn(Name, "No config file name set to start QuickFix server. FixConnector enqueued but inactive for now ...");
                        return;
                    }

                    // a configfile is set, let's spin that shit
                    ConfigFileName = fixConnector.ConfigFileName;
                    s_fixConnectors.Info(Name, "[OnStartUp] Creating FIX session with file " + ConfigFileName);
                    QuickFix.SessionSettings sessionSettings = new QuickFix.SessionSettings(ConfigFileName);

                    s_fixConnectors.Info(Name, "[OnStartUp] Creating socket acceptor");
                    m_acceptor = new QuickFix.ThreadedSocketAcceptor(
                        m_app=new NinjaTrader_FixBridge.QuickFixApp(s_fixConnectors),
                        new QuickFix.FileStoreFactory(sessionSettings),
                        sessionSettings,
                        new QuickFix.FileLogFactory(sessionSettings)
                        );

                    sessionSettings = null;

                    s_fixConnectors.Info(Name, "[OnStartUp] Starting FixBridge ...");
                    m_acceptor.Start();
                    s_fixConnectors.Info(Name, "[OnStartUp] FixBridge started !");
                }
            }
            catch (Exception e)
            {
                s_fixConnectors.Error(Name, "[OnStartUp] Exception OnStartUp : " + e.Message + ", source:" + e.Source);
                s_fixConnectors.Error(Name, "[OnStartUp] Exception OnStartUp : " + e.ToString());
                s_fixConnectors.Error(Name, "[OnStartUp] Exception OnStartUp : " + e.StackTrace);
                s_fixConnectors.Error(Name, "[OnStartUp]  => destroying FIX layer");

                KillFix();
                throw e;
            }
        }
        }
        static public void OnTermination(NinjaTrader.Strategy.FixConnector fixConnector)
        {
            lock (locker_)
            {
                s_fixConnectors.Remove(fixConnector);
                if (s_fixConnectors.Empty)
                {
                    s_fixConnectors.Info(Name, "No more FixConnectors : stopping.");
                    KillFix();
                }
            }
        }
        static public void KillFix()
        {
            lock (locker_)
            {
                if (m_app != null)
                {
                    m_app.Dispose();
                    m_app = null;
                }

                if (m_acceptor != null)
                {
                    m_acceptor.Stop(true);
                    m_acceptor.Dispose();
                    m_acceptor = null;
                }

            }
        }
    }
}
