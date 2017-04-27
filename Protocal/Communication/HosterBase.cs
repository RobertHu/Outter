using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocal.Communication
{
    public abstract class HosterBase
    {
        private enum CommandType
        {
            None,
            Start,
            Stop
        }

        private List<ServiceAgentBase> _serviceAgents;

        protected HosterBase()
        {
            _serviceAgents = this.InitializeServiceAgents();
        }

        protected abstract List<ServiceAgentBase> InitializeServiceAgents();

        public void Start()
        {
            this.Command(CommandType.Start);
        }

        public void Stop()
        {
            this.Command(CommandType.Stop);
        }

        private void Command(CommandType commandType)
        {
            if (_serviceAgents == null || _serviceAgents.Count == 0) throw new ArgumentException(string.Format("_serviceAgents is {0}", _serviceAgents == null ? "null" : "empty"));
            foreach (var eachAgent in _serviceAgents)
            {
                if (commandType == CommandType.Start)
                {
                    eachAgent.Start();
                }
                else if (commandType == CommandType.Stop)
                {
                    eachAgent.Stop();
                }
            }
        }

    }
}
