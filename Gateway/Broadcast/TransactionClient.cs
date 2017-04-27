using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace SystemController.Broadcast
{
    internal sealed class Client : ClientBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(Client));
        private ActionBlock<Command> _actionBlock;
        private const int MAX_ERROR_COUNT = 5;
        private int _errorCount = 0;

        internal Client(ICommandCollectService service, string serviceUrl, iExchange.Common.AppType appType)
            : base(service, serviceUrl, appType)
        {
            _actionBlock = new ActionBlock<Command>((Action<Command>)this.SendCommandHandle);
        }

        private void SendCommandHandle(Command command)
        {
            try
            {
                this.InnerSendCommand(command);
                _errorCount = 0;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                this.HandError(command);
            }
        }

        private void HandError(Command command)
        {
            try
            {
                _errorCount++;
                this.Close();
                if (_errorCount < MAX_ERROR_COUNT)
                {
                    this.Reconnect();
                    this.InnerSendCommand(command);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
            }
        }

        private void InnerSendCommand(Command command)
        {
            if (this.ShouldBroadcast(command))
            {
                if (command.IsQuotation && this.IsQuotationExceedTimeDiff((QuotationCommand)command))
                {
                    return;
                }
                Logger.InfoFormat("Broadcast command to client url={0}, appType={1}, CommandType={2}", this.ServiceUrl, this.AppType, command.GetType());
                _service.AddCommand(command);
            }
        }

        internal override void DoSend(Command command)
        {
            _actionBlock.Post(command);
        }

        internal override bool IsCommunicationOK()
        {
            try
            {
                _service.Test();
                return true;
            }
            catch
            {
                return false;
            }
        }


        protected override ILog Logger
        {
            get { return _Logger; }
        }
    }

    internal abstract class ClientBase
    {
        protected ICommandCollectService _service;
        private string _serviceUrl;
        private iExchange.Common.AppType _appType;

        protected ClientBase(ICommandCollectService service, string serviceUrl, iExchange.Common.AppType appType)
        {
            _service = service;
            _serviceUrl = serviceUrl;
            _appType = appType;
        }

        internal string ServiceUrl
        {
            get { return _serviceUrl; }
        }

        internal iExchange.Common.AppType AppType
        {
            get { return _appType; }
        }

        internal void Send(Command command)
        {
            if (this.ShouldBroadcast(command))
            {
                this.DoSend(command);
            }
        }

        internal abstract void DoSend(Command command);

        protected virtual bool ShouldBroadcast(Protocal.Command command)
        {
            return command.SourceType != _appType;
        }

        internal abstract bool IsCommunicationOK();

        protected bool IsQuotationExceedTimeDiff(QuotationCommand quotationCommand)
        {
            DateTime baseTime = DateTime.Now;
            if (quotationCommand.OverridedQs != null)
            {
                foreach (var eachOverrideQs in quotationCommand.OverridedQs)
                {
                    if ((baseTime - eachOverrideQs.Timestamp).TotalMilliseconds >= SettingManager.Default.QuotationTimeDiffInMS)
                    {
                        return true;
                    }
                }
            }

            if (quotationCommand.OriginQs != null)
            {
                foreach (var eachOriginQuotation in quotationCommand.OriginQs)
                {
                    if ((baseTime - eachOriginQuotation.Timestamp).TotalMilliseconds >= SettingManager.Default.QuotationTimeDiffInMS)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        protected void Close()
        {
            ICommunicationObject communicationObject = _service as ICommunicationObject;
            try
            {
                if (communicationObject != null)
                {
                    communicationObject.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                communicationObject.Abort();
            }
        }

        protected void Reconnect()
        {
            _service = ClientManager.CreateChannel(_serviceUrl);
            _service.Test();
        }

        protected abstract ILog Logger { get; }

        public override string ToString()
        {
            return string.Format("register url ={0}, appType = {1}", _serviceUrl, _appType);
        }
    }


    internal sealed class TransactionClient : ClientBase
    {
        private sealed class CommandManager : ThreadQueueBase<Command>
        {
            private TransactionClient _owner;

            internal CommandManager(TransactionClient owner)
                : base(50)
            {
                _owner = owner;
            }

            public override void DoWork(Command item)
            {
                _owner.SendCommand(item);
            }

            public override void RecordLog(Exception ex)
            {
                _owner.Logger.Error(ex);
            }
        }

        private static readonly ILog _Logger = LogManager.GetLogger(typeof(TransactionClient));

        private CommandManager _commandManager;

        internal TransactionClient(ICommandCollectService service, string serviceUrl, iExchange.Common.AppType appType)
            : base(service, serviceUrl, appType)
        {
            _commandManager = new CommandManager(this);
        }

        protected override bool ShouldBroadcast(Command command)
        {
            return base.ShouldBroadcast(command) && !(command is NotifyCommand);
        }

        private void SendCommand(Command command)
        {
            try
            {
                _service.AddCommand(command);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                this.RecoverConnection();
                _service.AddCommand(command);
            }
        }

        private void RecoverConnection()
        {
            while (true)
            {
                try
                {
                    this.Close();
                    this.Reconnect();
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                    Thread.Sleep(10000);
                }
            }
        }



        internal override bool IsCommunicationOK()
        {
            return true;
        }

        internal override void DoSend(Protocal.Command command)
        {
            _commandManager.Add(command);
        }

        protected override log4net.ILog Logger
        {
            get { return _Logger; }
        }
    }
}
