using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Management.Instrumentation;
using System.Runtime.Serialization;
using System.Threading;
using System.Text;

using MayhemCore;
using MayhemWpf.ModuleTypes;
using MayhemWpf.UserControls;

namespace SystemMayhem {

    [DataContract]
    [MayhemModule("AC Power Monitor", "Triggers when the computer is plugged in or unplugged")]
    public class ACPowerMonitor : EventBase, IWpfConfigurable {

        public enum AlertType {
            PluggedIn = 0x01,
            Unplugged = 0x02,
            Both = PluggedIn & Unplugged
        }

        [DataMember]
        private AlertType AlertWhen;

        private static int PluggedInPollingDelay = 1000;
        private static int UnpluggedPollingDelay = 5000;

        private bool Enabled;

        private bool Done;

        private Thread PollingThread;

        private object Lock;

        protected override void OnLoadDefaults() {
            base.OnLoadDefaults();
            AlertWhen = AlertType.Both;
        }

        protected override void OnLoadFromSaved() {
            base.OnLoadFromSaved();
        }

        protected override void OnAfterLoad() {
            base.OnAfterLoad();
            Lock = new object();
            Enabled = false;
            Done = false;
        }

        protected override void OnDeleted() {
            base.OnDeleted();

            lock (Lock) {
                Done = true;
            }
            if (PollingThread != null) {
                PollingThread.Join();
                PollingThread = null;
            }
        }

        protected override void OnEnabling(EnablingEventArgs e) {
            base.OnEnabling(e);
            lock (Lock) {
                Enabled = true;
                Done = false;
            }

            // Start up the polling thread
            PollingThread = new Thread(ACPowerMonitorThread);
            PollingThread.Start();
        }

        protected override void OnDisabled(DisabledEventArgs e) {
            base.OnDisabled(e);
            lock (Lock) {
                Enabled = false;
                Done = true;
            }
            if (PollingThread != null) {
                PollingThread.Join();
                PollingThread = null;
            }
        }

        public string GetConfigString() {
            lock (Lock) {
                switch (AlertWhen) {
                    case AlertType.PluggedIn:
                        return "Plugged in";
                    case AlertType.Unplugged:
                        return "Unplugged";
                    case AlertType.Both:
                    default:
                        return "Plugged in or unplugged";
                }
            }
        }

        public WpfConfiguration ConfigurationControl {
            get {
                ACPowerMonitorConfig config;
                lock (Lock) {
                    config = new ACPowerMonitorConfig(AlertWhen);
                }
                return config;
            }
        }

        public void OnSaved(WpfConfiguration configurationControl) {
            ACPowerMonitorConfig config = configurationControl as ACPowerMonitorConfig;
            lock (Lock) {
                AlertWhen = config.AlertWhen;
            }
        }

        private bool IsPluggedIn() {
            lock (Lock) {
                // Find all batteries through WMI
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Battery");
                foreach (ManagementObject manager in searcher.Get()) {
                    // Get the status of each battery
                    UInt16 status = (UInt16)manager["BatteryStatus"];

                    // Documentation for these return values found here:
                    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa394074(v=vs.85).aspx
                    // Basically, if the status is any of these values, then we should be plugged in.
                    switch (status) {
                        case 2:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                            return true;
                    }
                }
            }
            // If there weren't any batteries that said they were plugged in, then we must not be plugged in.
            return false;
        }

        private static bool AlertWhenPluggedIn(AlertType when) {
            return when == AlertType.PluggedIn || when == AlertType.Both;
        }

        private static bool AlertWhenUnplugged(AlertType when) {
            return when == AlertType.Unplugged || when == AlertType.Both;
        }

        private void ACPowerMonitorThread() {

            bool wasPluggedIn = IsPluggedIn();
            int pollingDelay = UnpluggedPollingDelay;

            bool done = false;
            while (!done) {
                // Check triggers
                lock (Lock) {
                    done = Done;

                    if (Enabled) {
                        bool isPluggedInNow = IsPluggedIn();
                        if (isPluggedInNow && !wasPluggedIn && AlertWhenPluggedIn(AlertWhen)) {
                            Trigger();
                        } else if (!isPluggedInNow && wasPluggedIn && AlertWhenUnplugged(AlertWhen)) {
                            Trigger();
                        }
                        wasPluggedIn = isPluggedInNow;

                        // If we are plugged in, then we can poll more frequently
                        if (isPluggedInNow) {
                            pollingDelay = PluggedInPollingDelay;
                        } else {
                            pollingDelay = UnpluggedPollingDelay;
                        }
                    }
                }

                if (!done) {
                    // Sleep till the next time we should check things
                    Thread.Sleep(pollingDelay);
                }
            }
        }
    }
}
