using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SsisDeploy
{
    public class InvalidArgumentException : Exception
    {
        public InvalidArgumentException(string argumentName) : base ($"Invalid argument {argumentName}.")
        {}
    }
}
