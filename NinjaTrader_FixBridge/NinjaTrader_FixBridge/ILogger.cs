using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NinjaTrader_FixBridge
{
    public interface ILogger
    {
        void Info(string source, string message);
        void Warn(string source, string message);
        void Error(string source, string message);
        void Alert(string source, string message);
    }
}
