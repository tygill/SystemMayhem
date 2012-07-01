using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MayhemWpf.UserControls;

namespace SystemMayhem {
    /// <summary>
    /// Interaction logic for InactivityEventControl.xaml
    /// </summary>
    public partial class InactivityEventConfig : WpfConfiguration {
        public InactivityEventConfig(uint threshold, InactivityEvent.Unit unit, bool triggerOnLeave) {
            InitializeComponent();
            
            Threshold = threshold;
            ThresholdUnit = unit;
            TriggerOnLeave = triggerOnLeave;
        }

        public uint Threshold { get; private set; }

        public InactivityEvent.Unit ThresholdUnit { get; private set; }

        public bool TriggerOnLeave { get; private set; }

        public override void OnLoad() {
            inactivityThreshold.Text = GetThresholdDisplay();

            // Initialize the units combobox
            units.Items.Add("milliseconds");
            units.Items.Add("seconds");
            units.Items.Add("minutes");
            units.Items.Add("hours");
            // Not 100% safe, but here I trust the conversion as I control the enums definition
            units.SelectedIndex = (int)ThresholdUnit;
            triggerWhen.SelectedIndex = TriggerOnLeave ? 0 : 1;
        }

        public override string Title {
            get {
                return "Inactivity";
            }
        }

        private void inactivityThreshold_TextChanged(object sender, TextChangedEventArgs e) {
            inactivityThreshold.Text = Regex.Replace(inactivityThreshold.Text, "[^0-9]", "", RegexOptions.Compiled);
            CanSave = inactivityThreshold.Text.Length > 0;

            update();
        }

        private void units_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            update();
        }

        private void triggerWhen_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            TriggerOnLeave = triggerWhen.SelectedIndex == 0;
        }

        private void update() {
            if (CanSave) {
                // Yes, I know that hard coding all these is probably bad form,
                // but at this point I don't want to figure out how .NET handles
                // strings as the content.
                // It probably handles them fine, as thats the most common use case anyway,
                // but oh well.
                switch (units.SelectedIndex) {
                    case 0:
                        Threshold = uint.Parse(inactivityThreshold.Text);
                        ThresholdUnit = InactivityEvent.Unit.Millisecond;
                        break;
                    case 1:
                        Threshold = uint.Parse(inactivityThreshold.Text) * 1000;
                        ThresholdUnit = InactivityEvent.Unit.Second;
                        break;
                    case 2:
                        Threshold = uint.Parse(inactivityThreshold.Text) * 1000 * 60;
                        ThresholdUnit = InactivityEvent.Unit.Minute;
                        break;
                    case 3:
                        Threshold = uint.Parse(inactivityThreshold.Text) * 1000 * 60 * 60;
                        ThresholdUnit = InactivityEvent.Unit.Hour;
                        break;
                }
            }
        }

        private string GetThresholdDisplay() {
            switch (ThresholdUnit) {
                default:
                case InactivityEvent.Unit.Millisecond:
                    return Threshold.ToString();
                case InactivityEvent.Unit.Second:
                    return ((uint)(Threshold / 1000)).ToString();
                case InactivityEvent.Unit.Minute:
                    return ((uint)(Threshold / (1000 * 60))).ToString();
                case InactivityEvent.Unit.Hour:
                    return ((uint)(Threshold / (1000 * 60 * 60))).ToString();
            }
        }
    }
}
