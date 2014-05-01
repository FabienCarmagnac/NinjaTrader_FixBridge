using NinjaTrader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NinjaTrader
{

    public class MarketDataDef
    {
        public NinjaTrader.Strategy.FixConnector fixConnector = null;
        public int internal_barinprogress = -1;
        public string symbol = string.Empty;
        public double open, high, low, close, volume, bid, ask, qty_bid, qty_ask, last, qty_last;
        public DateTime last_update;

        public MarketDataDef(NinjaTrader.Strategy.FixConnector pfixConnector, string psymbol)
        {
            fixConnector = pfixConnector;
            symbol = psymbol;
        }
        public static List<MarketDataDef> Read(NinjaTrader.Strategy.FixConnector f, string input)
        {
            List<MarketDataDef> limdd = new List<MarketDataDef>(f.Instruments.Select(i => new MarketDataDef(f, i.FullName)));
            limdd.AddRange(
                input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .ConvertAll(s => new MarketDataDef(f, s.Trim()))
                );
            
            for (int i = 0; i < limdd.Count;i++)
                limdd[i].internal_barinprogress=i;

            return limdd;
        }

        public void notify()
        {
            
        }
    }
}

