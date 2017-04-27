using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;

namespace iExchange.StateServer
{
    public class MatchInfoCommandBuffer
    {
        private Dictionary<Guid, MatchInfoCommand> _Commands;

        public MatchInfoCommandBuffer()
        {
            this._Commands = new Dictionary<Guid, MatchInfoCommand>();
        }

        public List<MatchInfoCommand> GetCommands(IEnumerable<Guid> instrumentIds)
        {
            List<MatchInfoCommand> commands = new List<MatchInfoCommand>();
            foreach (Guid instrumentId in instrumentIds)
            {
                lock (this)
                {
                    if (this._Commands.ContainsKey(instrumentId))
                    {
                        commands.Add(this._Commands[instrumentId]);
                    }
                }
            }
            return commands;
        }

        public void UpdateCache(MatchInfoCommand command)
        {
            lock (this)
            {
                this._Commands[command.InstrumentId] = command;
            }
        }
    }
}