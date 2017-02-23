using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SsisBuild
{
    public class CommandLineParsingException : Exception
    {
        public CommandLineParsingException(string message) : base (message) { }
    }
}
