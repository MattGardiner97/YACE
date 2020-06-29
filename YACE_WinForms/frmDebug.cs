using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using YACE;

namespace YACE_WinForms
{
    public partial class frmDebug : Form
    {
        private Emulator _emulator;

        private RegisterTextbox txtPC;
        private RegisterTextbox[] txtRegisters;
        private RegisterTextbox txtRegisterI;
        private Button btnStep;
        private Button btnPauseResume;

        public frmDebug(Emulator Emulator)
        {
            _emulator = Emulator;
            _emulator.Ticked += EmulatorTicked;
            _emulator.Paused += () => { UpdateDebugInfo(); btnStep.Enabled = true; };
            _emulator.CPU.ValueChanged += () => { UpdateDebugInfo(); };

            InitializeComponent();

            CreateComponents();

            UpdateDebugInfo();
        }

        private void CreateComponents()
        {
            //Main layout panel
            FlowLayoutPanel mainPanel = new FlowLayoutPanel()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown
            };
            this.Controls.Add(mainPanel);

            //PC register
            txtPC = new RegisterTextbox("PC", 2);
            txtPC.ValidValueEntered += (value) => { _emulator.CPU.PC = (ushort)(value); };
            txtPC.InvalidValueEntered += () => { txtPC.Textbox.Text = _emulator.CPU.PC.ToString("X4"); };

            //Main registers
            FlowLayoutPanel registerFlowPanel = new FlowLayoutPanel() { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            FlowLayoutPanel registerLeftSubPanel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            FlowLayoutPanel registerRightSubPanel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            registerFlowPanel.Controls.Add(registerLeftSubPanel);
            registerFlowPanel.Controls.Add(registerRightSubPanel);

            txtRegisters = new RegisterTextbox[16];
            for (int i = 0; i < 16; i++)
            {
                txtRegisters[i] = new RegisterTextbox($"V{i.ToString("X1")}", 1);
                if (i % 2 == 0)
                    registerLeftSubPanel.Controls.Add(txtRegisters[i]);
                else
                    registerRightSubPanel.Controls.Add(txtRegisters[i]);
                txtRegisters[i].ValidValueEntered += (value) => { _emulator.CPU.Registers[i] = (byte)value; };
                txtRegisters[i].InvalidValueEntered += () => { txtRegisters[i].Textbox.Text = _emulator.CPU.Registers[i].ToString("X2"); };
            }

            //Register I
            txtRegisterI = new RegisterTextbox("  I", 2);
            txtRegisterI.ValidValueEntered += (value) => { _emulator.CPU.RegisterI = (ushort)(value); };
            txtRegisterI.InvalidValueEntered += () => { txtRegisterI.Textbox.Text = _emulator.CPU.RegisterI.ToString("X4"); };

            //Step button
            btnStep = new Button()
            {
                Text = "Step",
                Enabled = false
            };
            btnStep.Click += (_, __) => { _emulator.Tick(); };

            btnPauseResume = new Button()
            {
                Text = "Resume"
            };
            btnPauseResume.Click += (_, ___) =>
            {
                btnPauseResume.Text = !_emulator.IsPaused ? "Resume" : "Pause";
                btnStep.Enabled = !_emulator.IsPaused;
                _emulator.IsPaused = !_emulator.IsPaused;
            };

            mainPanel.Controls.Add(txtPC);
            mainPanel.Controls.Add(registerFlowPanel);
            mainPanel.Controls.Add(txtRegisterI);
            mainPanel.Controls.Add(btnStep);
            mainPanel.Controls.Add(btnPauseResume);


            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MinimumSize = new Size(150, 0);
            this.ShowIcon = false;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Text = "Debug";
        }

        public void UpdateDebugInfo()
        {
            txtPC.Textbox.Text = _emulator.CPU.PC.ToString("X4");
            for (int i = 0; i < 16; i++)
                txtRegisters[i].Textbox.Text = _emulator.CPU.Registers[i].ToString("X2");
        }

        private void EmulatorTicked()
        {
            UpdateDebugInfo();
        }
    }
}
