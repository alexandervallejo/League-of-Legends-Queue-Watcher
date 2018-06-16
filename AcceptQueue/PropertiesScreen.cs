using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LOL.AcceptQueue
{
    public partial class PropertiesScreen : Form
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        MonitorService _MonitorService;

        public PropertiesScreen()
        {
            InitializeComponent();
            this.FormClosed += PropertiesScreen_FormClosed;


            comboBoxAllowMinimize.SelectedIndexChanged += ComboBoxAllowMinimize_SelectedIndexChanged;
            comboBoxShowVision.SelectedIndexChanged += ComboBoxShowVision_SelectedIndexChanged;

            textBoxLaunchPageInterval.KeyPress += ForceInt;
            textBoxLaunchPageInterval.TextChanged += Format0To100;
            textBoxLaunchPageInterval.TextChanged += TextBoxLaunchPageInterval_TextChanged;

            textBoxQueuePopInterval.KeyPress += ForceInt;
            textBoxQueuePopInterval.TextChanged += Format0To100;
            textBoxQueuePopInterval.TextChanged += TextBoxQueuePopInterval_TextChanged;

            textBoxAcceptionLocationX.KeyPress += ForceInt;
            textBoxAcceptionLocationX.TextChanged += Format0To100;
            textBoxAcceptionLocationX.TextChanged += TextBoxAcceptionLocationX_TextChanged;

            textBoxAcceptionLocationY.KeyPress += ForceInt;
            textBoxAcceptionLocationY.TextChanged += Format0To100;
            textBoxAcceptionLocationY.TextChanged += TextBoxAcceptionLocationY_TextChanged;
        }

        private void ComboBoxAllowMinimize_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItem = (sender as ComboBox).SelectedItem?.ToString() ?? string.Empty;
            bool applicationVision;
            if (bool.TryParse(selectedItem, out applicationVision))
            {
                _MonitorService.AllowLaunchPageMinimizing = applicationVision;
            }
        }

        private void ComboBoxShowVision_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItem = (sender as ComboBox).SelectedItem?.ToString() ?? string.Empty;
            bool applicationVision;
            if (bool.TryParse(selectedItem, out applicationVision))
            {
                _MonitorService.ShowApplicationVision = applicationVision;
            }
        }

        private void TextBoxAcceptionLocationX_TextChanged(object sender, EventArgs e)
        {
            _MonitorService.AcceptLocationX = ((double)IntFromTextBox(sender as TextBox) / 100);
        }

        private void TextBoxAcceptionLocationY_TextChanged(object sender, EventArgs e)
        {
            _MonitorService.AcceptLocationY = ((double)IntFromTextBox(sender as TextBox) / 100);
        }

        private void TextBoxQueuePopInterval_TextChanged(object sender, EventArgs e)
        {
            _MonitorService.CheckForQueueIntervalSec = IntFromTextBox(sender as TextBox);
        }

        private void TextBoxLaunchPageInterval_TextChanged(object sender, EventArgs e)
        {
            _MonitorService.CheckForLaunchPageIntervalSec = IntFromTextBox(sender as TextBox);
        }

        public void InitializeUpdateObject(MonitorService monitorService)
        {
            try
            {
                _MonitorService = monitorService;
                textBoxLaunchPageInterval.Text = _MonitorService.CheckForLaunchPageIntervalSec.ToString();
                textBoxQueuePopInterval.Text = _MonitorService.CheckForQueueIntervalSec.ToString();
                textBoxAcceptionLocationX.Text = (_MonitorService.AcceptLocationX * 100).ToString("#.##");
                textBoxAcceptionLocationY.Text = (_MonitorService.AcceptLocationY * 100).ToString("#.##");
                comboBoxAllowMinimize.SelectedItem = _MonitorService.AllowLaunchPageMinimizing ? "True" : "False";
                comboBoxShowVision.SelectedItem = _MonitorService.ShowApplicationVision ? "True" : "False";

            }
            catch (Exception ex)
            {
                log.Error("InitializeUpdateObject", ex);
            }
        }

        private void PropertiesScreen_FormClosed(object sender, FormClosedEventArgs e)
        {
            _MonitorService = null;
        }


        private int IntFromTextBox(TextBox textBox)
        {
            int parsedInt = -1;
            int.TryParse(textBox?.Text ?? "", out parsedInt);
            return parsedInt;
        }


        private void Format0To100(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                int parsedText = 0;
                if (int.TryParse(textBox.Text, out parsedText))
                {
                    if (parsedText > 100)
                    {
                        textBox.Text = "100";
                    }
                }
                else if (textBox.Text.Length > 0)
                {
                    textBox.Text = new string(textBox.Text.Take(textBox.Text.Length - 1).ToArray());
                }
                else
                {
                    textBox.Text = "1";
                }
            }
        }

        private void FormatGreaterThan0(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            int parsedText = 0;
            if (textBox != null &&
                int.TryParse(textBox.Text, out parsedText) == false)
            {
                if (textBox.Text.Length > 0)
                {
                    textBox.Text = new string(textBox.Text.Take(textBox.Text.Length - 1).ToArray());
                }
                else
                {
                    textBox.Text = "1";
                }
            }
        }





        private void ForceInt(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }
    }
}
