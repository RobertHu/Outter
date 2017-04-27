using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace SystemController
{
    partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        static void Main()
        {
            ServiceBase.Run(new Service());
        }

        protected override void OnStart(string[] args)
        {
            Server.Default.Start();
        }

        protected override void OnStop()
        {
            Server.Default.Stop();
        }
    }
}
