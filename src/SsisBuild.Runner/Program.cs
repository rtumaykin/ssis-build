using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using SsisBuild.Logger;

namespace SsisBuild.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var vsPath = GetVisualStudioPrivateAssembliesPath();

            //// relaunch the application with the base directory of Visual Studio Private assemblies
            //if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            //{
            //    if (Directory.Exists(vsPath))
            //    {
            //        var appDomainSetup = AppDomain.CurrentDomain.SetupInformation;

            //        appDomainSetup.ApplicationBase = vsPath;
            //        var workerDomain = AppDomain.CreateDomain("Worker Domain", null, appDomainSetup);
            //        workerDomain.ExecuteAssembly(typeof(Program).Assembly.Location, args);
            //    }
            //    else
            //    {
            //        throw new Exception("SQL Server Data Tools for Visual Studio 2015 are not installed.");
            //    }
            //}
            //else
            {
                DisplayHeader();

                Switches switches;
                try
                {
                    switches = Switches.ProcessArgs(args);
                }
                catch (ArgumentProcessingException x)
                {
                    Console.WriteLine(x.Message);
                    Usage();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                EchoParameters(switches);

                var builder = new Builder(new ConsoleLogger());
                builder.Execute(switches.ProjectPath, switches.ProtectionLevel, switches.Password, switches.NewPassword, switches.OutputFolder, switches.Configuration, switches.Parameters);
            }
        }

        private static void DisplayHeader()
        {
            Console.WriteLine("SSIS Build Engine");
            Console.WriteLine("Copyright (c) 2017 Roman Tumaykin");

        }

        private static void EchoParameters(Switches switches)
        {
            Console.WriteLine("Executing SSIS Build with the following switches:");
            Console.WriteLine();
            Console.WriteLine($"Project File:         {switches.ProjectPath}");

            if (!string.IsNullOrWhiteSpace(switches.ProtectionLevel))
                Console.WriteLine($"-ProtectionLevel:     {switches.ProtectionLevel}");

            if (!string.IsNullOrWhiteSpace(switches.Password))
                Console.WriteLine($"-Password:            {switches.Password}");

            if (!string.IsNullOrWhiteSpace(switches.NewPassword))
                Console.WriteLine($"-NewPassword:         {switches.NewPassword}");

            if (!string.IsNullOrWhiteSpace(switches.OutputFolder))
                Console.WriteLine($"-OutputFolder:        {switches.OutputFolder}");

            if (!string.IsNullOrWhiteSpace(switches.Configuration))
                Console.WriteLine($"-Configuration:       {switches.Configuration}");

            Console.WriteLine();
            Console.WriteLine("Project parameters:");
            foreach (var parameter in switches.Parameters)
            {
                Console.WriteLine($"  {parameter.Key}:\t{parameter.Value}");
            }
        }

        private static string GetVisualStudioPrivateAssembliesPath()
        {
            var path = (string) Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\14.0", "InstallDir", null);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\VisualStudio\\14.0", "InstallDir", null);
            }

            return Path.Combine(path, "PrivateAssemblies");
        }

        private static void Usage()
        {
            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("Usage:");
            Console.WriteLine("");
            Console.WriteLine("Syntax:              ssisbuild [Project File] [-<Switch Name> <Value>] [...[-<Switch Name> <Value>]] [-Parameter:<Name> <Value>] [...[-Parameter:<Name> <Value>]]");
            Console.WriteLine("");
            Console.WriteLine("Description:         Builds an SSIS project into an ispac file.");
            Console.WriteLine("");
            Console.WriteLine("Switches:");
            Console.WriteLine("");
            Console.WriteLine("  Project File:      Full path to a SSIS project file (with dtproj extension). If a project file is not specified, ssisbuild searches current working directory");
            Console.WriteLine("                     for a file with dtproj extension and uses that file.");
            Console.WriteLine("");
            Console.WriteLine("  -Configuration:    Required. Name of configuration to use. If such configuration does not exist in the project, then the first configuration from dtproj file");
            Console.WriteLine("                     will be copied into requested configuration name.");
            Console.WriteLine("");
            Console.WriteLine("  -OutputFolder:     Full path to a folder where the ispac file will be created. If ommitted, then the ispac file will be created in the");
            Console.WriteLine("                     bin/<Configuration> subfolder of the project folder.");
            Console.WriteLine("");
            Console.WriteLine("  -ProtectionLevel:  Overrides current project protection level. Available values are DontSaveSensitive, EncryptAllWithPassword, EncryptSensitiveWithPassword.");
            Console.WriteLine("");
            Console.WriteLine("  -Password:         Password to decrypt original project data if its current protection level is either EncryptAllWithPassword or EncryptSensitiveWithPassword, ");
            Console.WriteLine("                     in which case the value should be supplied, otherwise build will fail.");
            Console.WriteLine("");
            Console.WriteLine("  -NewPassword:      Password to encrypt resulting project if its resulting protection level is either EncryptAllWithPassword or EncryptSensitiveWithPassword.");
            Console.WriteLine("                     If ommitted, the value of the <Password> switch is used for encryption, unless original protection level was DontSaveSensitive,");
            Console.WriteLine("                     in which case the value should be supplied, otherwise build will fail.");
            Console.WriteLine("");
            Console.WriteLine("  -Parameter:        Project or Package parameter. Name is a standard full parameter name including the scope. For example Project::Parameter1. During the build,");
            Console.WriteLine("                     these values will replace existing values regardless of what these values were originally.");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine("     ssisbuild example.dtproj -Configuration Release -Parameter:SampleParameter \"some value\"");
        }
    }
}
