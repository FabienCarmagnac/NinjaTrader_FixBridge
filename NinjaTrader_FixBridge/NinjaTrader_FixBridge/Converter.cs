using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;

/** Convert QuickFix world from/to NinjaTrader world */

namespace NinjaTrader_FixBridge
{
    using SideMapping = Pair<QuickFix.Fields.Side, NinjaTrader.Cbi.OrderAction>;
    using OrderTypeMapping = Pair<QuickFix.Fields.OrdType, NinjaTrader.Cbi.OrderType>;
    using OrderStatusMapping = Pair<QuickFix.Fields.OrdStatus, NinjaTrader.Cbi.OrderState>;
	
    public static class Converter
    {
        readonly static List<SideMapping> s_sides = new List<SideMapping>()
		{
			new SideMapping(new QuickFix.Fields.Side(QuickFix.Fields.Side.BUY),NinjaTrader.Cbi.OrderAction.Buy)
			,new SideMapping(new QuickFix.Fields.Side(QuickFix.Fields.Side.SELL), NinjaTrader.Cbi.OrderAction.Sell)
		};

        readonly static List<OrderTypeMapping> s_ordtypes = new List<OrderTypeMapping>()
		{
			new OrderTypeMapping(new QuickFix.Fields.OrdType(QuickFix.Fields.OrdType.MARKET), NinjaTrader.Cbi.OrderType.Market)
			,new OrderTypeMapping(new QuickFix.Fields.OrdType(QuickFix.Fields.OrdType.LIMIT), NinjaTrader.Cbi.OrderType.Limit )
			,new OrderTypeMapping(new QuickFix.Fields.OrdType(QuickFix.Fields.OrdType.STOP), NinjaTrader.Cbi.OrderType.Stop )
			,new OrderTypeMapping(new QuickFix.Fields.OrdType(QuickFix.Fields.OrdType.STOP_LIMIT), NinjaTrader.Cbi.OrderType.StopLimit)
		};

        readonly static List<OrderStatusMapping> s_ordState = new List<OrderStatusMapping>()
		{
			new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.ACCEPTED_FOR_BIDDING),OrderState.Accepted)
			, new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.CANCELED),  OrderState.Cancelled)
			, new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.FILLED),  OrderState.Filled)
			, new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.PARTIALLY_FILLED),  OrderState.PartFilled)
			, new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.PENDING_CANCEL),  OrderState.PendingCancel)
			, new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.PENDING_CANCELREPLACE),  OrderState.PendingChange)
			, new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.PENDING_NEW),  OrderState.PendingSubmit)
			, new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.REJECTED),  OrderState.Rejected)
			, new OrderStatusMapping(new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.NEW),  OrderState.Working)
		};

        public static OrderAction c(QuickFix.Fields.Side s)
        {
            var val = s_sides.Find(v => v.first.getValue() == s.getValue());
            if (val == null)
                throw new Exception("Unmapped QuickFix.Fields.Side value : " + s.ToString());
            return val.second;
        }
        public static QuickFix.Fields.Side c(OrderAction s)
        {
            var val = s_sides.Find(v => v.second == s);
            if (val == null)
                throw new Exception("Unmapped OrderAction value : " + s.ToString());
            return val.first;
        }
        public static OrderType c(QuickFix.Fields.OrdType s)
        {
            var val = s_ordtypes.Find(v => v.first.getValue() == s.getValue());
            if (val == null)
                throw new Exception("Unmapped QuickFix.Fields.OrdType value : " + s.ToString());
            return val.second;
        }
        public static OrderState c(QuickFix.Fields.OrdStatus s)
        {
            var val = s_ordState.Find(v => v.first.getValue() == s.getValue());
            if (val == null)
                throw new Exception("Unmapped QuickFix.Fields.OrdStatus value : " + s.ToString());
            return val.second;
        }
        public static QuickFix.Fields.OrdStatus c(OrderState s)
        {
            var val = s_ordState.Find(v => v.second == s);
            if (val == null)
                throw new Exception("Unmapped OrderState value : " + s.ToString());
            return val.first;
        }

    }
}
