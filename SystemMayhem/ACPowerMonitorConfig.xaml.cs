using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MayhemCore;
using MayhemWpf.UserControls;

namespace SystemMayhem {
    /// <summary>
    /// Interaction logic for ACPowerMonitorConfig.xaml
    /// </summary>
    public partial class ACPowerMonitorConfig : WpfConfiguration {

        public ACPowerMonitorConfig(ACPowerMonitor.AlertType alertWhen) {
            InitializeComponent();
            AlertWhen = alertWhen;

            // As the combobox can only have valid values, we can always save
            CanSave = true;
        }

        public ACPowerMonitor.AlertType AlertWhen { get; private set; }

        public override void OnLoad() {
            base.OnLoad();
            alertType.Items.Add("Plugged in and unplugged");
            alertType.Items.Add("Plugged in");
            alertType.Items.Add("Unplugged");
            switch (AlertWhen) {
                case ACPowerMonitor.AlertType.Both:
                default:
                    alertType.SelectedIndex = 0;
                    break;
                case ACPowerMonitor.AlertType.PluggedIn:
                    alertType.SelectedIndex = 1;
                    break;
                case ACPowerMonitor.AlertType.Unplugged:
                    alertType.SelectedIndex = 2;
                    break;
            }
        }

        public override string Title {
            get {
                return "AC Power Monitor";
            }
        }

        private void alertType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            switch (alertType.SelectedIndex) {
                case 0:
                    AlertWhen = ACPowerMonitor.AlertType.Both;
                    break;
                case 1:
                    AlertWhen = ACPowerMonitor.AlertType.PluggedIn;
                    break;
                case 2:
                    AlertWhen = ACPowerMonitor.AlertType.Unplugged;
                    break;
            }
        }
    }
}
