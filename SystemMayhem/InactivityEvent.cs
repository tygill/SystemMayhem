using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

using MayhemCore;
using MayhemWpf.ModuleTypes;
using MayhemWpf.UserControls;

namespace SystemMayhem {

    [DataContract]
    [MayhemModule("Inactivity", "Triggers when the user is inactive for a specified amount of time")]
    public class InactivityEvent : EventBase, IWpfConfigurable {

        public enum Unit {
            Millisecond,
            Second,
            Minute,
            Hour
        }

        [DataMember]
        // In ticks (milliseconds)
        private uint InactivityThreshold;

        [DataMember]
        private Unit InactivityUnit;

        [DataMember]
        private bool TriggerOnLeave;

        private bool Enabled;

        private bool Done;

        private Thread PollingThread;

        private object Lock;

        protected override void OnLoadDefaults() {
            base.OnLoadDefaults();

            InactivityThreshold = 5000;
            InactivityUnit = Unit.Second;
            TriggerOnLeave = true;
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
            PollingThread = new Thread(InactivityThread);
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
                switch (InactivityUnit) {
                    default:
                    case Unit.Millisecond:
                        return String.Format("{0:d} milliseconds of inactivity", InactivityThreshold);
                    case Unit.Second:
                        return String.Format("{0:d} seconds of inactivity", (uint)(InactivityThreshold / 1000));
                    case Unit.Minute:
                        return String.Format("{0:d} minutes of inactivity", (uint)(InactivityThreshold / (1000 * 60)));
                    case Unit.Hour:
                        return String.Format("{0:d} hours of inactivity", (uint)(InactivityThreshold / (1000 * 60 * 60)));
                }
            }
        }

        public WpfConfiguration ConfigurationControl {
            get {
                InactivityEventConfig config;
                lock (Lock) {
                    config = new InactivityEventConfig(InactivityThreshold, InactivityUnit, TriggerOnLeave);
                }
                return config;
            }
        }

        public void OnSaved(WpfConfiguration configurationControl) {
            InactivityEventConfig config = configurationControl as InactivityEventConfig;
            lock (Lock) {
                InactivityThreshold = config.Threshold;
                InactivityUnit = config.ThresholdUnit;
                TriggerOnLeave = config.TriggerOnLeave;
            }
        }

        // http://stackoverflow.com/questions/3911367/getting-system-idle-time-with-qt
        public struct LastInputInfo {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LastInputInfo plii);

        /// <summary> 
        /// Returns the number of milliseconds since the last user input (or mouse movement) 
        /// </summary> 
        private static uint GetIdleTime() {
            LastInputInfo lastInput = new LastInputInfo();
            lastInput.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInput);
            GetLastInputInfo(ref lastInput);

            return (uint)Environment.TickCount - lastInput.dwTime;
        } 


        private void InactivityThread() {

            int pollingDelay = 1000;
            uint lastIdleTime = 0;
            bool triggerOnReturn = false;

            bool done = false;
            while (!done) {
                // Check triggers
                lock (Lock) {
                    done = Done;

                    if (Enabled) {
                        uint inactiveTime = GetIdleTime();

                        // If they reset during this last interval, reset the last idle time as well
                        if (inactiveTime < lastIdleTime) {
                            lastIdleTime = 0;
                            // If we trigger on return instead of on leave,
                            // then here's where someone is back.
                            if (triggerOnReturn) {
                                Trigger();
                                triggerOnReturn = false;
                            }
                        }

                        // Only the first time that the inactive time passes the threshold should it be triggered
                        if (lastIdleTime < InactivityThreshold && inactiveTime >= InactivityThreshold) {
                            if (TriggerOnLeave) {
                                // Trigger only if we're triggering on leave
                                Trigger();
                            } else {
                                // Otherwise, prep it for a trigger when we come back
                                triggerOnReturn = true;
                            }
                        }

                        lastIdleTime = inactiveTime;

                        // Set the polling time to match whatever the current unit is
                        switch (InactivityUnit) {
                            case Unit.Millisecond:
                                pollingDelay = 1;
                                break;
                            // Make this be the default so that if they change units in the middle, this thread isn't stuck
                            // sleeping forever before it notices that it needed to change.
                            case Unit.Second:
                            default:
                                pollingDelay = 1000;
                                break;
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
