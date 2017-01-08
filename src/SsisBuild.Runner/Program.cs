using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SsisBuild.Logger;

namespace SsisBuild.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var switches = Switches.ProcessArgs(args);
                var builder = new Builder(new ConsoleLogger());
                builder.Execute(switches);
            }
            catch (ArgumentProcessingException x)
            {
                Console.WriteLine(x.Message);
                Usage();
                Environment.Exit(1);
            }
            catch
            {
                Environment.Exit(1);
            }

        }

        private static void Usage()
        {
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("Usage:");
            Console.WriteLine("");
            Console.WriteLine("Syntax:                ssisbuild [Project File] [-<Switch Name> <Value>] [...[-<Switch Name> <Value>]] [-Parameter:<Name> <Value>] [...[-Parameter:<Name> <Value>]]");
            Console.WriteLine("");
            Console.WriteLine("Description:           Builds an SSIS project into an ispac file.");
            Console.WriteLine("");
            Console.WriteLine("Switches:");
            Console.WriteLine("");
            Console.WriteLine("  Project File:        Full path to a SSIS project file (with dtproj extension). If a project file is not specified, ssisbuild searches current working directory");
            Console.WriteLine("                       for a file with dtproj extension and uses that file.");
            Console.WriteLine("");
            Console.WriteLine("  -Configuration:      Required. Name of project configuration to use.");
            Console.WriteLine("");
            Console.WriteLine("  -OutputFolder:       Full path to a folder where the ispac file will be created. If ommitted, then the ispac file will be created in the");
            Console.WriteLine("                       bin/<Configuration> subfolder of the project folder.");
            Console.WriteLine("");
            Console.WriteLine("  -ProtectionLevel:    Overrides current project protection level. Available values are DontSaveSensitive, EncryptAllWithPassword, EncryptSensitiveWithPassword.");
            Console.WriteLine("");
            Console.WriteLine("  -Password:           Password to decrypt original project data if its current protection level is either EncryptAllWithPassword or EncryptSensitiveWithPassword, ");
            Console.WriteLine("                       in which case the value should be supplied, otherwise build will fail.");
            Console.WriteLine("");
            Console.WriteLine("  -NewPassword:        Password to encrypt resulting project if its resulting protection level is either EncryptAllWithPassword or EncryptSensitiveWithPassword.");
            Console.WriteLine("                       If ommitted, the value of the <Password> switch is used for encryption, unless original protection level was DontSaveSensitive,");
            Console.WriteLine("                       in which case the value should be supplied, otherwise build will fail.");
            Console.WriteLine("");
            Console.WriteLine("  -Parameter:          Project or Package parameter. Name is a standard full parameter name including the scope. For example Project::Parameter1. During the build,");
            Console.WriteLine("                       these values will replace existing values regardless of what these values were originally.");
            Console.WriteLine("");
            Console.WriteLine("  -SensitiveParameter: Project or Package parameter forced to be sensitive. Name is a standard full parameter name including the scope. For example Project::Parameter1. During the build,");
            Console.WriteLine("                       these values will replace existing values regardless of what these values were originally.");
            Console.WriteLine("  -ReleaseNotes:       Path to a release notes file. File can have simple or complex release notes format.");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine("     ssisbuild example.dtproj -Configuration Release -Parameter:SampleParameter \"some value\"");
        }
    }
}
