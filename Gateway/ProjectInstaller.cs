using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;


namespace TraderService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            this.serviceInstaller1.FailRunCommand = "SystemController.exe";

            // The fail count reset time resets the failure count after N seconds of no failures
            // on the service.  This value is set in seconds, though note that the SCM GUI only
            // displays it in increments of days.
            this.serviceInstaller1.FailCountResetTime = 0;

            // The fail reboot message is used when a reboot action is specified and works in 
            // conjunction with the RecoverAction.Reboot type.

            this.serviceInstaller1.FailRebootMsg = "Trade Service Has a Problem";

            // Set some failure actions : Isn't this easy??
            // Do note that if you specify less than three actions, the remaining actions will take on
            // the value of the last action.  For example, if you only set one action to RunCommand,
            // failure 2 and failure 3 will also take on the default action of RunCommand. This is 
            // a "feature" of the ChangeServiceConfig2() method; Use RecoverAction.None to disable
            // unwanted actions.

            this.serviceInstaller1.FailureActions.Add(new FailureAction(RecoverAction.Restart, 0));
            this.serviceInstaller1.FailureActions.Add(new FailureAction(RecoverAction.Restart, 0));
            this.serviceInstaller1.FailureActions.Add(new FailureAction(RecoverAction.Restart, 0));

            // Configure the service to start right after it is installed.  We do not want the user to
            // have to reboot their machine or to have some other process start it.  Do be careful because
            // if this service is dependent upon other services, they must be installed PRIOR to this one
            // for the service to be started properly

            this.serviceInstaller1.StartOnInstall = true;

        }
    }
}
