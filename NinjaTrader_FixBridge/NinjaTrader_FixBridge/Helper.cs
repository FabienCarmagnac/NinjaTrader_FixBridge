using NinjaTrader.Cbi;
using System;

namespace NinjaTrader_FixBridge
{
    public class FakeOrderForRejectReport : NinjaTrader.Cbi.IOrder
    {
        int m_qty;
        OrderAction m_side;
        public FakeOrderForRejectReport(int qty, OrderAction side)
        {
            m_qty = qty;
            m_side = side;

        }
        public int Quantity
        {
            get;
            set;
        }
        public DateTime Time
        {
            get { return DateTime.Now; }
        }
        public NinjaTrader.Cbi.OrderAction OrderAction
        {
            get;
            set;
        }


        public double AvgFillPrice
        {
            get { return 0; }
        }

        public NinjaTrader.Cbi.OrderState OrderState
        {
            get { return NinjaTrader.Cbi.OrderState.Rejected; }
        }

        public int Filled
        {
            get { return 0; }
        }
        #region non implemented property
        public NinjaTrader.Cbi.ErrorCode Error
        {
            get { throw new NotImplementedException(); }
        }

        public string FromEntrySignal
        {
            get { throw new NotImplementedException(); }
        }

        public NinjaTrader.Cbi.Instrument Instrument
        {
            get { throw new NotImplementedException(); }
        }

        public double LimitPrice
        {
            get { throw new NotImplementedException(); }
        }

        public bool LiveUntilCancelled
        {
            get { throw new NotImplementedException(); }
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public string NativeError
        {
            get { throw new NotImplementedException(); }
        }

        public string Oco
        {
            get { throw new NotImplementedException(); }
        }

        public string OrderId
        {
            get { throw new NotImplementedException(); }
        }

        public NinjaTrader.Cbi.OrderType OrderType
        {
            get { throw new NotImplementedException(); }
        }

        public bool OverFill
        {
            get { throw new NotImplementedException(); }
        }

        public double StopPrice
        {
            get { throw new NotImplementedException(); }
        }

       
        public NinjaTrader.Cbi.TimeInForce TimeInForce
        {
            get { throw new NotImplementedException(); }
        }

        public string Token
        {
            get { throw new NotImplementedException(); }
        }
        #endregion

    }
    public static class Helper
    {
        static object locker_ = new object();
        static Random rand = new Random();
        public static string GetUniqueID(string prefix)
        {
            lock (locker_)
            {
                return prefix+"_"+rand.Next().ToString("000000000");
            }

        }
    }
}
