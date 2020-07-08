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
    public partial class frmDisassembly : Form
    {
        private Emulator _emulator;
        private Disassembler _disassembler;

        private ListView listAssembly;

        public frmDisassembly(Emulator Emulator)
        {
            _emulator = Emulator;

            _disassembler = new Disassembler(_emulator.Debugger.GetROMBaseAddress());

            _emulator.Paused += () => { UpdateSelection(); listAssembly.Enabled = true; } ;
            _emulator.Resumed += () => { listAssembly.Enabled = false; };
            _emulator.Debugger.Stepped += UpdateSelection;
            _emulator.ROMLoaded += (Span<byte> ROM) => { UpdateDisassembly(ROM); UpdateDisassemblyList(); } ;

            InitializeComponent();
            CreateComponents();
        }

        private void CreateComponents()
        {
            listAssembly = new ListView()
            {
                View = View.Details,
                Width = 300,
                Height = 300,
                FullRowSelect = true,
                MultiSelect = false,
                Enabled = false
            };
            listAssembly.Columns.Add("Location", (int)(listAssembly.Width * 0.25 - 10));
            listAssembly.Columns.Add("Opcode", (int)(listAssembly.Width * 0.25 - 10));
            listAssembly.Columns.Add("Assembly", (int)(listAssembly.Width * 0.5 - 10));

            ContextMenuStrip ctxListAssembly = new ContextMenuStrip();
            ctxListAssembly.Items.Add("Goto");
            ctxListAssembly.Items[0].Click += (_, __) =>
            {
                if (listAssembly.SelectedItems.Count == 0)
                    return;
                ListViewItem selectedItem = listAssembly.SelectedItems[0];
                ushort pc = ushort.Parse(selectedItem.Text, System.Globalization.NumberStyles.HexNumber, null);
                _emulator.Debugger.SetProgramCounter(pc);
            };
            listAssembly.ContextMenuStrip = ctxListAssembly;

            this.Controls.Add(listAssembly);

            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowIcon = false;
            this.Text = "Disassembler";
        }

        public void UpdateDisassembly(Span<byte> ROM)
        {
            _disassembler.Disassemble(ROM);
        }

        public void UpdateDisassemblyList()
        {
            listAssembly.Items.Clear();
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
            string searchValue = _emulator.Debugger.GetProgramCounter().ToString("X4");
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
