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
using System.Media;

namespace YACE_WinForms
{
    public partial class frmMain : Form
    {
        private Emulator _emulator;

        Bitmap internalBitmap;
        Bitmap screenBitmap;

        private frmDebug _frmDebug;
        private frmDisassembly _frmDisassembly;
        private frmMemoryViewer _frmMemoryViewer;

        private MenuStrip menuMain;
        private PictureBox picScreen;
        private OpenFileDialog _loadROMDialog;

        private bool _loopRunning = false;
        private DateTime _lastBeepTime;

        public frmMain()
        {
            InitializeComponent();

            _emulator = new Emulator();
            _emulator.Graphics.ScreenRefresh += Graphics_ScreenRefresh;

            _emulator.Paused += () =>
            {
                this.Text = $"YACE - Paused";
            };
            _emulator.LateResumed += () =>
            {
                this.Text = "YACE";
                RunLoop();
            };
            _emulator.CPU.Beeped += CPU_Beeped;

            CreateComponents();

            InitialiseBitmap();

            this.Shown += FrmMain_Shown;
            this.Move += FrmMain_Move;
            this.KeyDown += FrmMain_KeyDown;
            this.KeyUp += FrmMain_KeyUp;
        }

        private void CPU_Beeped()
        {
            //We do this check to prevent multiple beeps occuring in a short time.
            //This is because the ROM may take several ticks to update the sound timer, meaning the Beeped event can be called multiple times.
            if ((DateTime.Now - _lastBeepTime).TotalSeconds > 1)
            {
                _lastBeepTime = DateTime.Now;
                Task.Run(() =>
                {
                    Console.Beep(1200, 200);
                });
            }
        }



        //Events
        private void FrmMain_KeyUp(object sender, KeyEventArgs e)
        {
            byte key = GetKeyFromKeyCode(e.KeyCode);
            if (key <= 0xF)
                _emulator.Input.SetKeyState(key, false);
        }

        private void FrmMain_KeyDown(object sender, KeyEventArgs e)
        {
            byte key = GetKeyFromKeyCode(e.KeyCode);
            if (key <= 0xF)
                _emulator.Input.SetKeyState(key, true);
        }

        private void FrmMain_Move(object sender, EventArgs e)
        {
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
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
            if (_loopRunning == true)
                return;

            while (_emulator.IsPaused == false)
            {
                _loopRunning = true;
                _emulator.Tick();

                this.Invalidate();
                this.Update();
                this.Refresh();
                Application.DoEvents();
            }
            _loopRunning = false;
        }

        private byte GetKeyFromKeyCode(Keys KeyCode)
        {
            switch (KeyCode)
            {
                case Keys.NumPad0:
                    return 0;
                case Keys.NumPad1:
                    return 1;
                case Keys.NumPad2:
                    return 2;
                case Keys.NumPad3:
                    return 3;
                case Keys.NumPad4:
                    return 4;
                case Keys.NumPad5:
                    return 5;
                case Keys.NumPad6:
                    return 6;
                case Keys.NumPad7:
                    return 7;
                case Keys.NumPad8:
                    return 8;
                case Keys.NumPad9:
                    return 9;
                case Keys.A:
                    return 0xA;
                case Keys.B:
                    return 0xB;
                case Keys.C:
                    return 0xC;
                case Keys.D:
                    return 0xD;
                case Keys.E:
                    return 0xE;
                case Keys.F:
                    return 0xF;
            }
            return byte.MaxValue;
        }

        private void LoadROM(string ROMFilename)
        {
            byte[] rom = File.ReadAllBytes(ROMFilename);
            _emulator.LoadROM(rom);

            RunLoop();
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
            _frmDisassembly = new frmDisassembly(_emulator);
            _frmMemoryViewer = new frmMemoryViewer(_emulator);

            //Load ROM dialog
            _loadROMDialog = new OpenFileDialog()
            {
                InitialDirectory = AppContext.BaseDirectory,
                CheckFileExists = true,
                Multiselect = false,
                RestoreDirectory = false
            };

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
            this.StartPosition = FormStartPosition.CenterScreen;
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
            ToolStripMenuItem dropdownEmulator = new ToolStripMenuItem()
            {
                Text = "Emulator"
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
            tsmiFileLoad.Click += (_, __) =>
            {
                DialogResult dResult = _loadROMDialog.ShowDialog();
                if (dResult == DialogResult.OK)
                {
                    LoadROM(_loadROMDialog.FileName);
                }
            };
            dropdownFile.DropDownItems.Add(tsmiFileLoad);

            //Emulator menu subitems
            ToolStripMenuItem tsmiEmulatorPause = new ToolStripMenuItem()
            {
                Text = "Pause",
                Enabled = false
            };
            tsmiEmulatorPause.Click += (_, __) =>
            {
                if (_emulator.IsPaused == true)
                    _emulator.Resume();
                else
                    _emulator.Pause();
            };

            _emulator.ROMLoaded += (_) => { tsmiEmulatorPause.Enabled = true; };
            _emulator.Paused += () => { tsmiEmulatorPause.Text = "Resume"; };
            _emulator.Resumed += () => { tsmiEmulatorPause.Text = "Pause"; };
            dropdownEmulator.DropDownItems.Add(tsmiEmulatorPause);

            //Window menu subitems
            ToolStripMenuItem tsmiWindowDebug = new ToolStripMenuItem()
            {
                Text = "Debug",
            };
            tsmiWindowDebug.Click += (_, __) => { _frmDebug.Show();  };

            ToolStripMenuItem tsmiWindowDisassembly = new ToolStripMenuItem()
            {
                Text = "Disassembly"
            };
            tsmiWindowDisassembly.Click += (_, __) => { _frmDisassembly.Show(); };

            ToolStripMenuItem tsmiWindowMemoryViewer = new ToolStripMenuItem()
            {
                Text = "Memory Viewer"
            };
            tsmiWindowMemoryViewer.Click += (_, __) => { _frmMemoryViewer.Show();  };

            dropdownWindow.DropDownItems.AddRange(new ToolStripItem[]
            {
                tsmiWindowDebug,
                tsmiWindowDisassembly,
                tsmiWindowMemoryViewer
            });

            //Add menu strip items
            result.Items.Add(dropdownFile);
            result.Items.Add(dropdownEmulator);
            result.Items.Add(dropdownWindow);

            return result;
        }

    }
}
