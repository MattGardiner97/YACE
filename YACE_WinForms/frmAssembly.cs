using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using YACE;

namespace YACE_WinForms
{
    public partial class frmAssembly : Form
    {
        private Emulator _emulator;
        private Disassembler _disassembler;

        private ListView listAssembly;

        public frmAssembly(Emulator Emulator)
        {
            _emulator = Emulator;
            byte[] rom = _emulator.Memory.ReadROM();

            _disassembler = new Disassembler(_emulator.Memory.ROMBaseAddress);
            _disassembler.LoadROM(rom);
            _disassembler.Disassemble();

            _emulator.Paused += () => { UpdateSelection(); };
            _emulator.CPU.ValueChanged += () => { UpdateSelection(); };
            _emulator.Ticked += () => { UpdateSelection(); };


            InitializeComponent();
            CreateComponents();

            UpdateDisassembly();
        }

        private void CreateComponents()
        {
            listAssembly = new ListView()
            {
                View = View.Details,
                Width = 300,
                Height = 300,
                FullRowSelect= true,
                MultiSelect = false,
            };
            listAssembly.Columns.Add("Location",(int)(listAssembly.Width * 0.25 -10));
            listAssembly.Columns.Add("Opcode", (int)(listAssembly.Width * 0.25 -10));
            listAssembly.Columns.Add("Assembly",(int)(listAssembly.Width* 0.5 -10));

            this.Controls.Add(listAssembly);

            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Text = "Disassembler";
        }

        public void UpdateDisassembly()
        {
            for (int i = 0; i < _disassembler.Instructions.Count; i++)
            {
                Instruction instruction = _disassembler.Instructions[i];

                ListViewItem item = new ListViewItem(instruction.Location.ToString("X4"));
                item.SubItems.Add(instruction.Opcode.ToString("X4"));
                item.SubItems.Add(instruction.Assembly);
                listAssembly.Items.Add(item);
            }

            
        }

        public void UpdateSelection()
        {
            string searchValue = _emulator.CPU.PC.ToString("X4");
            for (int i = 0; i < listAssembly.Items.Count; i++)
            {
                if (listAssembly.Items[i].Text == searchValue)
                {
                    listAssembly.Items[i].Selected = true;
                    listAssembly.Items[i].EnsureVisible();
                    break;
                }
            }
        }
    }
}
