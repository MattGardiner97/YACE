using System;
using System.Collections.Generic;
using System.Text;

namespace YACE_WinForms
{
    public class Instruction
    {
        public Instruction(int Location, int Opcode, string Assembly)
        {
            this.Location = Location;
            this.Opcode = Opcode;
            this.Assembly = Assembly;
        }

        public int Location { get; private set; }
        public int Opcode { get; private set; }
        public string Assembly { get; private set; }

    }
}
