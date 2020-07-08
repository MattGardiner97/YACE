using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using YACE;

namespace YACE_WinForms
{
    public partial class frmMemoryViewer : Form
    {
        private const int LINE_SIZE = 16;

        private DataGridView gridMemory;

        private Emulator _emulator;

        public frmMemoryViewer(Emulator Emulator)
        {
            _emulator = Emulator;

            InitializeComponent();

            CreateComponents();

            _emulator.ROMLoaded += (_) => { UpdateMemory(); };
        }

        private void UpdateMemory()
        {
            byte[] RAM = _emulator.Debugger.GetRAM();

            gridMemory.RowCount = (int)Math.Ceiling((double)(RAM.Length / LINE_SIZE));
            
            for (int startIndex = 0; startIndex < RAM.Length; startIndex += LINE_SIZE)
            {
                var currentRow = gridMemory.Rows[startIndex/LINE_SIZE];
                currentRow.HeaderCell.Value = startIndex.ToString("X4");
                for (int i = 0; i < LINE_SIZE; i++)
                {
                    currentRow.Cells[i].Value = RAM[startIndex + i].ToString("X2");
                    currentRow.Cells[i].ToolTipText = (startIndex + i).ToString("X4");
                }
            }

            gridMemory.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);
            gridMemory.AutoResizeColumns();
            gridMemory.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        public void CreateComponents()
        {
            gridMemory = new DataGridView()
            {
                AutoSize = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };

            gridMemory.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridMemory.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            gridMemory.ColumnHeadersDefaultCellStyle.Font = new Font(gridMemory.Font, FontStyle.Bold);

            gridMemory.ColumnCount = 16;
            for (int i = 0; i < 16; i++)
            {
                gridMemory.Columns[i].HeaderText = i.ToString("X2");
                gridMemory.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                gridMemory.Columns[i].Frozen = false;
            }

            gridMemory.CellEndEdit += GridMemory_CellEndEdit;

            //this.Controls.Add(listMemory);
            this.Controls.Add(gridMemory);

            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MaximumSize = new Size(600, 400);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.Text = "Memory Viewer";
        }

        private void GridMemory_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var cell = gridMemory[e.ColumnIndex, e.RowIndex];
            string stringValue = (string)cell.Value;
            string stringAddress = (string)cell.ToolTipText;

            ushort address = ushort.Parse(stringAddress, System.Globalization.NumberStyles.HexNumber);
            byte parsedValue = 0;
            if (byte.TryParse(stringValue, System.Globalization.NumberStyles.HexNumber, null, out parsedValue))
            {
                _emulator.Debugger.GetRAM()[address] = parsedValue;
                cell.Value = _emulator.Debugger.GetRAM()[address].ToString("X2"); //Ensure the new value is formatted nicely
            }
            else
                cell.Value = _emulator.Debugger.GetRAM()[address].ToString("X2");
        }
    }
}
