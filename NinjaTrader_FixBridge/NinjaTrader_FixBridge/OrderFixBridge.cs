using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;

namespace NinjaTrader_FixBridge
{
    /* Represents a single execution in terms of FIX */
    public class ExecStatus
    {
        public int CumQty { get; set; }
        public double AvgPx { get; set; }
        public int LeavesQty{ get; set; }

    }
    /* Order mapper between NT7 and FIX */
    public class OrderFixBridge
    {
        List<IExecution> m_executions = new List<IExecution>();
        public NinjaTrader.Cbi.IOrder Order { get; set; }
        public string QuickFixId { get; set; }
        public QuickFix.SessionID OrderSessionID { get; set; }
        public OrderFixBridge(NinjaTrader.Cbi.IOrder p_order, string quickfix_id, QuickFix.SessionID sessionid)
        {
            Order = p_order;
            QuickFixId = quickfix_id;
            OrderSessionID = sessionid;
        }
        public void AddExec(IExecution iexec)
        {
            if(iexec!=null)
                m_executions.Add(iexec);
        }

        public ExecStatus GetExecStatus()
        {
            double avgpx=0, r = 0;
            int total_qty=0;
            m_executions.ForEach(ie => { r += ie.Quantity * ie.Price; total_qty += ie.Quantity; });
            if (total_qty == 0)
                avgpx = 0;
            else
                avgpx = r / total_qty;
            return new ExecStatus() { AvgPx = avgpx, LeavesQty = Order.Quantity - total_qty, CumQty = total_qty };
        }
        public IExecution GetLast()
        {
            return m_executions.Count == 0 ? null : m_executions[m_executions.Count - 1];
        }
    }

    public class OrderFixBridgeCollection
    {
        protected List<OrderFixBridge> m_orders = new List<OrderFixBridge>();
      
        public void Clear()
        {
            m_orders.Clear();
        }


        public OrderFixBridge AddOrGet(IOrder order, string clordid, QuickFix.SessionID session)
        {
            if (order == null && string.IsNullOrEmpty(clordid))
                throw new Exception("internal error: order is null and clordid empty");

            OrderFixBridge r = null;

            if (order == null) // lookup by clordid
            {
                r = m_orders.Find(s => s.QuickFixId == clordid);
                if (r == null)
                    m_orders.Add(r = new OrderFixBridge(null, clordid, session));
            }
            else  // lookup by order
            {
                r = m_orders.Find(s => s.Order == order);
                if (r == null)
                    m_orders.Add(r = new OrderFixBridge(order, clordid, session));
                
            }

            // update session
            if (r.OrderSessionID == null && session != null)
                r.OrderSessionID = session;
            
            //update fix id
            if (string.IsNullOrEmpty(r.QuickFixId) && !string.IsNullOrEmpty(clordid))
                r.QuickFixId = clordid;

            return r;              
        }
    }
	
}
