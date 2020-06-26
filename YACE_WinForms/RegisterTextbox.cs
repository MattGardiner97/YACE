using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace YACE_WinForms
{
    public partial class RegisterTextbox : UserControl
    {
        public Label Label { get; private set; }
        public TextBox Textbox { get; private set; }

        public event Action<int> ValidValueEntered;
        public event Action InvalidValueEntered;

        private int _registerSize;

        public RegisterTextbox(string LabelText, int RegisterSize)
        {
            _registerSize = RegisterSize;
            InitializeComponent();
            CreateComponents(LabelText);
        }

        private void CreateComponents(string LabelText)
        {
            FlowLayoutPanel flp = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            Textbox = new TextBox()
            {
                MaxLength = 4,
                Width = 40,
                Margin = new Padding(0)
            };
            Textbox.KeyPress += Textbox_KeyPress;
            Label = new Label()
            {
                Text = LabelText,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.None,
                AutoSize = true,
                Margin = new Padding(0,0,2,0)
            };


            this.Controls.Add(flp);
            flp.Controls.Add(this.Label);
            flp.Controls.Add(this.Textbox);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Label.Height = Textbox.Height;
            Label.Top = Textbox.Top;
        }

        private void Textbox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char)Keys.Enter)
                return;

            if(_registerSize == 1)
            {
                byte result = 0;
                if (byte.TryParse(Textbox.Text, System.Globalization.NumberStyles.HexNumber, null, out result))
                {
                    Textbox.Text = result.ToString("X2");
                    ValidValueEntered?.Invoke(result);
                }
                else
                    InvalidValueEntered?.Invoke();
            }
            else if(_registerSize == 2)
            {
                ushort result = 0;
                if (ushort.TryParse(Textbox.Text, System.Globalization.NumberStyles.HexNumber, null, out result))
                {
                    Textbox.Text = result.ToString("X4");
                    ValidValueEntered?.Invoke(result);
                }
                else
                    InvalidValueEntered?.Invoke();
            }
        }

        public void SetText(string Text)
        {
            this.Textbox.Text = Text;
        }
    }
}
