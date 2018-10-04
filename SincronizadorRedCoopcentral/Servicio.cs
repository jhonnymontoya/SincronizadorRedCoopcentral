using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Security;

namespace Com.StartLineSoft.SincronizadorRedCoopcentral
{
    public partial class Servicio : ServiceBase
    {
        public Servicio()
        {
            InitializeComponent();

            this.eventLog = new EventLog();
            try
            {
                if (!EventLog.SourceExists("FonAdmin"))
                {
                    EventLog.CreateEventSource("FonAdmin", "FonAdmin Log");
                }
           
                this.eventLog.Source = "FonAdmin";
                this.eventLog.Log = "FonAdmin Log";

                this.timer = new Timer();
                this.timer.Interval = 60000;
                this.timer.Elapsed += new ElapsedEventHandler(this.onTimer);
                this.timer.Enabled = true;
                this.timer.Stop();
            }
            catch (SecurityException e)
            {
            }
            catch(Exception e)
            {
            }

        }

        protected void onTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            this.eventLog.WriteEntry("Monitoreando", EventLogEntryType.Information, 5);
            Monitor monitor = new Monitor();
            monitor.Ejecutar();
        }

        protected override void OnStart(string[] args)
        {
            this.timer.Start();
            this.eventLog.WriteEntry(
                "Sincronizador Red Coopcentral Fonadmin iniciado",
                EventLogEntryType.Information,
                1
            );
        }

        protected override void OnStop()
        {
            this.eventLog.WriteEntry(
                "Sincronizador Red Coopcentral Fonadmin detenido",
                EventLogEntryType.Information,
                2
            );
            this.timer.Stop();
        }

        protected override void OnPause()
        {
            base.OnPause();
            this.eventLog.WriteEntry(
                "Sincronizador Red Coopcentral Fonadmin en pausa",
                EventLogEntryType.Information,
                3
            );
            this.timer.Stop();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            this.eventLog.WriteEntry(
                "Sincronizador Red Coopcentral Fonadmin continua su operación",
                EventLogEntryType.Information,
                4
            );
            this.timer.Start();
        }
    }
}
