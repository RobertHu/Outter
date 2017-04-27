using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.TransactionServer.Engine.iExchange.Common
{
    internal struct CommandSequence
    {
        private readonly long _value;
        internal CommandSequence(long value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(string.Format("{0} is out of range", value));
            _value = value;
        }
        internal long Value { get { return _value; } }
    }

    internal sealed class CommandSequenceManager
    {
        internal static readonly CommandSequenceManager Default = new CommandSequenceManager();
        private int _sequence;
        private CommandSequenceManager()
        {
            _sequence = 0;
        }

        internal CommandSequence Get()
        {
            long seq = Interlocked.Increment(ref _sequence);
            return new CommandSequence(seq);
        }
    }
}
