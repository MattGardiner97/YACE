using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using Graphics = System.Drawing.Graphics;

using YACE;

namespace YACE_WinForms
{
    public partial class frmMain : Form
    {
        private Emulator _emulator;
        private Disassembler _disassembler;

        Bitmap internalBitmap;
        Bitmap screenBitmap;

        private frmDebug _frmDebug;
        private frmAssembly _frmAssembly;

        private MenuStrip menuMain;
        private PictureBox picScreen;

        public bool Paused { get; set; } = false;

        public frmMain()
        {
            InitializeComponent();

            _emulator = new Emulator();
            byte[] rom = File.ReadAllBytes("C:/Users/Matt/Desktop/CONNECT4");
            _emulator.Memory.LoadROM(rom);
            _emulator.Graphics.ScreenRefresh += Graphics_ScreenRefresh;

            _emulator.Paused += () =>
            {
                this.Text = $"YACE - Paused";
            };
            _emulator.Resumed += () =>
            {
                this.Text = "YACE";
                RunLoop();
            };

            _disassembler = new Disassembler(_emulator.Memory.ROMBaseAddress);
            _disassembler.LoadROM(rom);
            _disassembler.Disassemble();

            CreateComponents();

            InitialiseBitmap();

            this.Shown += FrmMain_Shown;
            this.Move += FrmMain_Move;
            this.KeyPress += FrmMain_KeyPress;
        }

        private void FrmMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            char c = e.KeyChar;
            byte result = 0;
            if (byte.TryParse(c.ToString(), System.Globalization.NumberStyles.HexNumber, null, out result))
                _emulator.Input.SetKeyState(result, true);
        }

        //Events
        private void FrmMain_Move(object sender, EventArgs e)
        {
            UpdateFormDebug();
            UpdateFormAssembly();
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            _frmDebug.Show();
            UpdateFormDebug();

            _frmAssembly.Show();
            UpdateFormAssembly();

            RunLoop();
        }

        private void Graphics_ScreenRefresh()
        {
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                    internalBitmap.SetPixel(x, y, _emulator.Graphics.FrameBuffer[x, y] == 0 ? Color.Black : Color.Blue);
            UpdateScreenBitmap();
        }

        //User functions
        private void RunLoop()
        {
            while (_emulator.IsPaused == false)
            {
                _emulator.Tick();

                this.Invalidate();
                this.Update();
                this.Refresh();
                Application.DoEvents();
            }
        }

        private void UpdateFormDebug()
        {
            _frmDebug.Left = this.Right;
            _frmDebug.Top = this.Top;
        }

        private void UpdateFormAssembly()
        {
            _frmAssembly.Left = this.Left - _frmAssembly.Width;
            _frmAssembly.Top = this.Top;
        }

        private void InitialiseBitmap()
        {
            internalBitmap = new Bitmap(64, 32);
            using (Graphics g = Graphics.FromImage(internalBitmap))
            {
                g.Clear(Color.Black);
            }

            screenBitmap = new Bitmap(picScreen.ClientSize.Width, picScreen.ClientSize.Height);
            UpdateScreenBitmap();
            picScreen.Image = screenBitmap;
        }

        private void UpdateScreenBitmap()
        {
            using (Graphics g = Graphics.FromImage(screenBitmap))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.DrawImage(internalBitmap, 0, 0, screenBitmap.Width, screenBitmap.Height);
            }
            picScreen.Image = screenBitmap;
        }

        private void CreateComponents()
        {
            //Main panel
            TableLayoutPanel mainPanel = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill
            };

            //Create other forms
            _frmDebug = new frmDebug(_emulator);
            _frmAssembly = new frmAssembly(_emulator);

            //Main menu strip
            menuMain = CreateMenuStrip();


            //Screen image
            picScreen = new PictureBox()
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill
            };

            this.Text = "YACE";
            this.Controls.Add(mainPanel);
            mainPanel.Controls.Add(menuMain, 0, 0);
            mainPanel.Controls.Add(picScreen, 0, 1);
        }

        private MenuStrip CreateMenuStrip()
        {
            MenuStrip result = new MenuStrip();

            //Menu items
            ToolStripMenuItem dropdownFile = new ToolStripMenuItem()
            {
                Text = "File"
            };
            ToolStripMenuItem dropdownWindow = new ToolStripMenuItem()
            {
                Text = "Window"
            };

            //File menu subitems
            ToolStripMenuItem tsmiFileLoad = new ToolStripMenuItem()
            {
                Text = "Load ROM"
            };
            dropdownFile.DropDownItems.Add(tsmiFileLoad);

            //Window menu subitems
            ToolStripMenuItem tsmiWindowDebug = new ToolStripMenuItem()
            {
                Text = "Debug Window",
            };
            tsmiWindowDebug.Click += (_, __) => { _frmDebug.Show(); };
            dropdownWindow.DropDownItems.Add(tsmiWindowDebug);

            //Add menu strip items
            result.Items.Add(dropdownFile);
            result.Items.Add(dropdownWindow);

            return result;
        }

    }
}
