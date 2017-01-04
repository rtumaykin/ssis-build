using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SsisBuild.Logger
{
    public class ConsoleLogger : ILogger
    {
        public void LogMessage(string message)
        {
            Console.WriteLine(message);
        }

        public void LogError(string error)
        {
            Console.WriteLine(error);
            throw new Exception(error);
        }
    }
}
