using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SsisBuild.Runner
{
    public class ArgumentProcessingException : Exception
    {
        public ArgumentProcessingException(string message) : base(message) {}
    }
}
