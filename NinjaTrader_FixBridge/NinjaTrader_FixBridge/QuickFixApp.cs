using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NinjaTrader_FixBridge
{
    public class QuickFixApp : QuickFix.MessageCracker, QuickFix.IApplication, IDisposable
    {
        object locker_ = new object();
        ILogger m_logger;
        public QuickFixApp(ILogger logger)
        {
            m_logger = logger;
        }
        /* mapping NT7 <=> QuickFixApp */
        protected OrderFixBridgeCollection m_orders = new OrderFixBridgeCollection();

        #region QuickFix.Application Methods

        public void FromApp(QuickFix.Message msg, QuickFix.SessionID sessionID)
        {
            m_logger.Info(sessionID.SenderCompID, " FromApp msg : " + msg.ToString());
            Crack(msg, sessionID);
        }
        public void OnCreate(QuickFix.SessionID sessionID) { }
        public void OnLogout(QuickFix.SessionID sessionID) { m_logger.Info(sessionID.TargetCompID, " has logout"); }
        public void OnLogon(QuickFix.SessionID sessionID) { m_logger.Info(sessionID.TargetCompID, " has logon"); }
        public void FromAdmin(QuickFix.Message msg, QuickFix.SessionID sessionID)
        {
            m_logger.Info(sessionID.SenderCompID," FromAdmin msg : " + msg.ToString());
            //Crack(msg, sessionID);			
        }
        public void ToAdmin(QuickFix.Message msg, QuickFix.SessionID sessionID)
        {
            m_logger.Info(sessionID.SenderCompID," ToAdmin msg : " + msg.ToString());
            //Crack(msg, sessionID);			
        }
        public void ToApp(QuickFix.Message msg, QuickFix.SessionID sessionID)
        {
            m_logger.Info(sessionID.SenderCompID, " ToApp msg : " + msg.ToString());
           // Crack(msg, sessionID);
        }

        #endregion

        public void FromNT_OnOrderUpdate(IOrder order, IExecution exec)
        {
            try
            {
                if (order == null)
                    return;

                OrderFixBridge nt7fb;
                ExecStatus current_exec_status;
                lock (locker_)
                {
                    nt7fb = m_orders.AddOrGet(order, string.Empty, null);
                    if (exec != null)
                        nt7fb.AddExec(exec);
                    current_exec_status = nt7fb.GetExecStatus();
                }
                if (exec == null)
                    SendExecutionReport(nt7fb, order.Instrument.FullName, double.NaN, double.NaN, "execution on order occured");
                else
                    SendExecutionReport(nt7fb, order.Instrument.FullName, exec.Quantity, exec.Price, "execution on order occured");
            }catch(Exception e)
            {
                m_logger.Info("[QuickFixApp.FromNT_OnOrderUpdate]", e.ToString());
            }
        }



        protected void SendExecutionReport(OrderFixBridge order, string symbol, double execQty, double execPrice, string text)
        {
            if (order == null) throw new Exception("[QuickFixApp.SendExecutionReport] OrderFixBridge order is null ");
            IOrder o = order.Order;

            if (o == null) throw new Exception("[QuickFixApp.SendExecutionReport] IOrder is null ");
            if (string.IsNullOrEmpty(symbol)) throw new Exception("[QuickFixApp.SendExecutionReport] symbol is empty");

            QuickFix.SessionID session = order.OrderSessionID;
            if (session == null) throw new Exception("[QuickFixApp.SendExecutionReport] session is null ");

            QuickFix.FIX42.ExecutionReport msg = new QuickFix.FIX42.ExecutionReport();
            msg.OrderID = new QuickFix.Fields.OrderID(Helper.GetUniqueID("EXECID"));
            msg.HandlInst = new QuickFix.Fields.HandlInst(QuickFix.Fields.HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE_NO_BROKER_INTERVENTION);
            msg.Symbol = new QuickFix.Fields.Symbol(symbol);
            msg.Side = Converter.c(o.OrderAction);
            msg.LeavesQty = new QuickFix.Fields.LeavesQty(Convert.ToDecimal(Math.Max(0,o.Quantity-o.Filled)));
            msg.CumQty = new QuickFix.Fields.CumQty(Convert.ToDecimal(o.Filled));
            msg.AvgPx = new QuickFix.Fields.AvgPx(Convert.ToDecimal(o.AvgFillPrice));
            msg.TransactTime = new QuickFix.Fields.TransactTime(o.Time, true);
            msg.Text = new QuickFix.Fields.Text(text);
            msg.ExecTransType = new QuickFix.Fields.ExecTransType(QuickFix.Fields.ExecTransType.NEW);

            switch (o.OrderState)
            {
                     // pending : ignored
                case OrderState.Accepted:
                case OrderState.PendingChange:
                case OrderState.PendingSubmit:
                case OrderState.PendingCancel:
                    return;

                case OrderState.Working:
                    msg.OrdStatus = new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.NEW);
                    
                    msg.ExecID = new QuickFix.Fields.ExecID(Helper.GetUniqueID("NEW"));
                    msg.ExecType = new QuickFix.Fields.ExecType(QuickFix.Fields.ExecType.NEW);
                    
                    break;

                case OrderState.Cancelled:
                    msg.OrdStatus = new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.CANCELED);
                    msg.ExecID = new QuickFix.Fields.ExecID(Helper.GetUniqueID("CANCELED"));
                    msg.ExecType = new QuickFix.Fields.ExecType(QuickFix.Fields.ExecType.CANCELED);

                    break;

                case OrderState.Filled:
                case OrderState.PartFilled:

                    if (double.IsNaN(execQty)) return;
                    if (double.IsNaN(execPrice)) return;

                    bool full = o.OrderState == OrderState.Filled;

                    msg.OrdStatus = new QuickFix.Fields.OrdStatus(full ? QuickFix.Fields.OrdStatus.FILLED : QuickFix.Fields.OrdStatus.PARTIALLY_FILLED);
                    msg.ExecID = new QuickFix.Fields.ExecID(Helper.GetUniqueID("EXEC"));
                    msg.ExecType = new QuickFix.Fields.ExecType(full ? QuickFix.Fields.ExecType.FILL : QuickFix.Fields.ExecType.PARTIAL_FILL);
                    msg.LastShares = new QuickFix.Fields.LastShares(Convert.ToDecimal(execQty));
                    msg.LastPx = new QuickFix.Fields.LastPx(Convert.ToDecimal(execPrice));

                    break;

                case OrderState.Rejected:

                    msg.OrdStatus = new QuickFix.Fields.OrdStatus(QuickFix.Fields.OrdStatus.REJECTED);
                    msg.ExecType = new QuickFix.Fields.ExecType(QuickFix.Fields.ExecType.REJECTED);
                    msg.OrigClOrdID = new QuickFix.Fields.OrigClOrdID(order.QuickFixId);
                    msg.ExecID = new QuickFix.Fields.ExecID(Helper.GetUniqueID("REJ"));
                    break;


                case OrderState.Unknown:
                    throw new Exception("[QuickFixApp.SendExecutionReport] IOrder should not have unknown status at this point !");
            }

            QuickFix.Session.SendToTarget(msg, session);

        }

        #region MessageCracker overloads

        #region Cancel/Replace, Cancel & CancelReject

        protected void ProcessOrderCancelRequest(QuickFix.SessionID session, QuickFix.Fields.ClOrdID clordid, QuickFix.Fields.OrigClOrdID origordid)
        {
            ProcessOrderCancelRequest(session, clordid, origordid, false, 0, 0, 0);
        }
        protected void ProcessOrderCancelRequest(QuickFix.SessionID session, QuickFix.Fields.ClOrdID clordid, QuickFix.Fields.OrigClOrdID origordid
            , bool is_cancelreplace_request, int new_qty, double new_px, double new_stop_px)
        {
            IOrder order = null;
            try
            {
                // order exists ?
                OrderFixBridge order_bridge ;
                lock(locker_)
                    order_bridge = m_orders.AddOrGet(null, origordid.getValue(), session);

                if (order_bridge.Order == null)
                {
                    RejectCancelRequest(session, clordid, origordid, null, "Unknown order !", QuickFix.Fields.CxlRejReason.UNKNOWN_ORDER, true);
                    return;
                }

                order = order_bridge.Order;

                switch (order.OrderState)
                {
                    // order is still alive, go ahead
                    case OrderState.Accepted:
                    case OrderState.PartFilled:
                    case OrderState.Working:
                        if (is_cancelreplace_request)
                            QuickFixStaticAcceptor.FromFIX_ChangeOrder(order, new_qty, new_px, new_stop_px);
                        else
                            QuickFixStaticAcceptor.FromFIX_CancelOrder(order);

                        return;

                    // pending 
                    case OrderState.PendingChange: //VV?
                    case OrderState.PendingSubmit: //VV?
                    case OrderState.PendingCancel: //VV?
                        {
                            string error_message = "Invalid cancel or cancel/replace request since order " + origordid.getValue() + " is in pending state : " + order.OrderState.ToString();
                            RejectCancelRequest(session, clordid, origordid, order, error_message, QuickFix.Fields.CxlRejReason.ALREADY_PENDING, is_cancelreplace_request);
                            return;
                        }

                    // terminal state
                    case OrderState.Cancelled:
                    case OrderState.Filled:
                    case OrderState.Rejected:
                        {
                            string error_message = "Invalid cancel or cancel/replace request since order " + origordid.getValue() + " is in terminal/closed state : " + order.OrderState.ToString();
                            RejectCancelRequest(session, clordid, origordid, order, error_message, QuickFix.Fields.CxlRejReason.TOO_LATE_TO_CANCEL, is_cancelreplace_request);
                            return;
                        }
                    case OrderState.Unknown:
                        {
                            string error_message = "Invalid cancel or cancel/replace request since order " + origordid.getValue() + " is in unknow NT7 state : " + order.OrderState.ToString();
                            RejectCancelRequest(session, clordid, origordid, order, error_message, QuickFix.Fields.CxlRejReason.OTHER, is_cancelreplace_request);
                            return;
                        }
                }
            }
            catch (Exception e)
            {
                RejectCancelRequest(session, clordid, origordid, order, "ProcessOrderCancelRequest : internal Error : " + e.ToString()
                , QuickFix.Fields.CxlRejReason.OTHER, is_cancelreplace_request);
            }
        }

        public void OnMessage(QuickFix.FIX42.OrderCancelReplaceRequest msg, QuickFix.SessionID session)
        {
            try
            {
                // qty set ?
                int qty = -1;
                if (msg.IsSetOrderQty()) qty = Convert.ToInt32(msg.OrderQty.getValue());
                else if (msg.IsSetCashOrderQty()) qty = Convert.ToInt32(msg.CashOrderQty.getValue());
                if (qty < 0)
                {
                    RejectCancelRequest(session, msg.ClOrdID, msg.OrigClOrdID, null, "Qty nor CashOrderQty field is not set", QuickFix.Fields.CxlRejReason.BROKER_OPTION, true);
                    return;
                }

                double price = (msg.IsSetPrice() ? Convert.ToDouble(msg.Price.getValue()) : 0);
                double stopPx = (msg.IsSetStopPx() ? Convert.ToDouble(msg.StopPx.getValue()) : 0);

                ProcessOrderCancelRequest(session, msg.ClOrdID, msg.OrigClOrdID, true, qty, price, stopPx);

            }
            catch (Exception e)
            {
                RejectCancelRequest(session, msg.ClOrdID, msg.OrigClOrdID, null, "OnMessage(QuickFix.FIX42.OrderCancelReplaceRequest msg, QuickFix.SessionID session) : internal error : " + e.ToString()
                , QuickFix.Fields.CxlRejReason.OTHER, true);

            }
        }

        public void OnMessage(QuickFix.FIX42.OrderCancelRequest msg, QuickFix.SessionID session)
        {
            try
            {
                ProcessOrderCancelRequest(session, msg.ClOrdID, msg.OrigClOrdID);

            }
            catch (Exception e)
            {
                RejectCancelRequest(session, msg.ClOrdID, msg.OrigClOrdID, null, "OnMessage(QuickFix.FIX42.OrderCancelRequest msg, QuickFix.SessionID session): internal error : " + e.ToString()
                , QuickFix.Fields.CxlRejReason.OTHER, false);
            }
        }

        protected void RejectCancelRequest(QuickFix.SessionID session, QuickFix.Fields.ClOrdID clordid, QuickFix.Fields.OrigClOrdID origordid, IOrder nt7_order, string rej_reason, int cxl_rej, bool is_cancelreplace_request)
        {
            QuickFix.FIX42.OrderCancelReject rej = new QuickFix.FIX42.OrderCancelReject();
            if (nt7_order == null)
                rej.Set(new QuickFix.Fields.OrderID("NONE"));
            else
                rej.Set(new QuickFix.Fields.OrderID(clordid.getValue()));

            rej.Set(origordid);
            rej.Set(Converter.c(nt7_order.OrderState));
            rej.Set(new QuickFix.Fields.CxlRejResponseTo(is_cancelreplace_request ? QuickFix.Fields.CxlRejResponseTo.ORDER_CANCEL_REPLACE_REQUEST : QuickFix.Fields.CxlRejResponseTo.ORDER_CANCEL_REQUEST));
            rej.Set(new QuickFix.Fields.CxlRejReason(cxl_rej));
            rej.Set(new QuickFix.Fields.Text(rej_reason));
            rej.Set(new QuickFix.Fields.TransactTime(DateTime.Now, true));

            QuickFix.Session.SendToTarget(rej, session);
        }
        #endregion // Cancel and CancelReject

        #region Creation and rejection

        public void OnMessage(QuickFix.FIX42.NewOrderSingle msg, QuickFix.SessionID session)
        {
            try
            {
                OrderFixBridge r ;
                lock(locker_)
                    r = m_orders.AddOrGet(null, msg.ClOrdID.getValue(), session);

                if (r.Order!=null)
                {
                    RejectNew(session, msg.Symbol.getValue(), msg.ClOrdID.getValue(), Convert.ToInt32(msg.OrderQty.getValue()), Converter.c(msg.Side), "ClOrdID '" + msg.ClOrdID + "'already exists !");
                    return;
                }

                QuickFixStaticAcceptor.FromFIX_SendOrder(
                    r,
                    msg.Symbol.getValue(),
                    Converter.c(msg.Side),
                    Converter.c(msg.OrdType),
                    msg.IsSetOrderQty() ? Convert.ToInt32(msg.OrderQty.getValue()) : 0,
                    msg.IsSetPrice() ? Convert.ToDouble(msg.Price.getValue()) : 0,
                    msg.IsSetStopPx() ? Convert.ToDouble(msg.StopPx.getValue()) : 0,
                    msg.IsSetText() ? msg.Text.getValue() : string.Empty
                );

                if (r.Order == null)
                {
                    RejectNew(session, msg.Symbol.getValue(), msg.ClOrdID.getValue(), Convert.ToInt32(msg.OrderQty.getValue()), Converter.c(msg.Side), "NinjaTrader7 returned null on creating order (internal error)");
                    return;
                }
            }
            catch (Exception e)
            {
                RejectNew(session, msg.Symbol.getValue(), msg.ClOrdID.getValue(), Convert.ToInt32(msg.OrderQty.getValue()), Converter.c(msg.Side), e.Message);
            }
        }

        protected void RejectNew(QuickFix.SessionID session, string symbol, string order_id, int qty, OrderAction side, string reply_text)
        {
            IOrder fake_order = new FakeOrderForRejectReport(qty, side);
            OrderFixBridge order = new OrderFixBridge(fake_order, order_id, session);
            SendExecutionReport(order, symbol, 0, 0, reply_text);
        }

        #endregion // Creation and rejection

        #endregion // Message cracker

        public void Dispose()
        {
            m_orders.Clear();
            m_orders = null;
        }

    }
}

