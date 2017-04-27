using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal sealed class AccountFacade
    {
        static AccountFacade() { }
        internal static readonly AccountFacade Default = new AccountFacade();
        private Lazy<StrategyFactory> _generalStrategyFactory;
        private Lazy<StrategyFactory> _physicalStrategyFactory;

        private Lazy<ExecuteService> _generalExecuteService;
        private Lazy<ExecuteService> _physicalExecuteService;

        private Lazy<ExecuteService> _generalBookService;
        private Lazy<ExecuteService> _physicalBookService;

        private AccountFacade()
        {
            _generalStrategyFactory = new Lazy<StrategyFactory>(() => new GeneralExecuteStrategyFactory());
            _physicalStrategyFactory = new Lazy<StrategyFactory>(() => new PhysicalExecuteStrategyFactory());
            _generalExecuteService = new Lazy<ExecuteService>(() => new ExecuteService(_generalStrategyFactory.Value.CreateExecuteStrategy()));
            _physicalExecuteService = new Lazy<ExecuteService>(() => new ExecuteService(_physicalStrategyFactory.Value.CreateExecuteStrategy()));
            _generalBookService = new Lazy<ExecuteService>(() => new ExecuteService(_generalStrategyFactory.Value.CreateBookStrategy()));
            _physicalBookService = new Lazy<ExecuteService>(() => new ExecuteService(_physicalStrategyFactory.Value.CreateBookStrategy()));
        }

        internal ExecuteService CreateExecuteService(Transaction tran)
        {
            if (tran.IsPhysical)
            {
                return _physicalExecuteService.Value;
            }
            else
            {
                return _generalExecuteService.Value;
            }
        }

        internal ExecuteService CreateBookService(Transaction tran)
        {
            if (tran.IsPhysical)
            {
                return _physicalBookService.Value;
            }
            else
            {
                return _generalBookService.Value;
            }
        }


    }
}
