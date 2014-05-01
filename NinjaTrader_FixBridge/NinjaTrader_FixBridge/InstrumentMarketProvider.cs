using NinjaTrader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace NinjaTrader_FixBridge
{

    public class InstrumentMarketProvider : ILogger
    {
        public const string Name="InstrumentMarketProvider";

        object locker_ = new object();
        List<MarketDataDef> m_providers = new List<MarketDataDef>();

        protected void RemoveBySwapToEnd(IList list, int index)
        {
            list[index] = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
        }
        public void Add(NinjaTrader.Strategy.FixConnector fixc)
        {
            if (fixc == null)
                return;

            lock (locker_)
            {
                m_providers.AddRange(fixc.GetSymbols());
            }
        }
        public void Remove(NinjaTrader.Strategy.FixConnector fixc)
        {
            if (fixc == null)
                return;

            lock (locker_)
            {
                m_providers.RemoveAll(mdd => mdd.fixConnector == fixc);
            }
        }

        public bool Empty
        {
            get { lock (locker_) return m_providers.Count == 0; }
        }


        public NinjaTrader.Strategy.FixConnector GetAnyFixConnector()
        {
            if (m_providers.Count > 0)
                return m_providers[0].fixConnector;
            return null;
        }
        public MarketDataDef GetMarketDataDefForSymbol(string symb)
        {
            lock (locker_)
            {
                return m_providers.Find(m => m.symbol == symb);
            }
        }

        #region loggers
        public void Info(string source, string message)
        {
            lock (locker_)
            {
                NinjaTrader.Strategy.FixConnector f = Logger();
                if (f != null) f.Info(source, message);
            }
        }
        public void Warn(string source, string message)
        {
            lock (locker_)
            {
                NinjaTrader.Strategy.FixConnector f = Logger();
                if (f != null) f.Warn(source, message);
            }
        }
        public void Error(string source, string message)
        {
            lock (locker_)
            {
                NinjaTrader.Strategy.FixConnector f = Logger();
                if (f != null) f.Error(source, message);
            }
        }
        public void Alert(string source, string message)
        {
            lock (locker_)
            {
                NinjaTrader.Strategy.FixConnector f = Logger();
                if (f != null) f.Alert(source, message);
            }
        }

        #endregion

        #region protected members
        protected NinjaTrader.Strategy.FixConnector Logger()
        {
            return GetAnyFixConnector();
        }
        
        #endregion

    }
}
