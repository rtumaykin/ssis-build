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

            // relaunch the application with the base directory of Visual Studio Private assemblies
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                if (Directory.Exists(vsPath))
                {
                    var appDomainSetup = AppDomain.CurrentDomain.SetupInformation;

                    appDomainSetup.ApplicationBase = vsPath;
                    var workerDomain = AppDomain.CreateDomain("Worker Domain", null, appDomainSetup);
                    workerDomain.ExecuteAssembly(typeof(Program).Assembly.Location, args);
                }
                else
                {
                    throw new Exception("SQL Server Data Tools for Visual Studio 2015 are not installed.");
                }
            }
            else
            {
                Parameters parameters = null;
                try
                {
                    parameters = Parameters.ProcessArgs(args);
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

                EchoParameters(parameters);

                BuildIspac(parameters.ProjectPath, parameters.BuildArguments);
            }
        }

        private static void BuildIspac(string projectPath, IDictionary<string, string> buildArguments)
        {
            var builder = new SsisBuild.Builder(new ConsoleLogger());
            builder.Execute(projectPath, null, "Deployment", buildArguments);
        }

        private static void EchoParameters(Parameters parameters)
        {
            Console.WriteLine("Running SSIS Build with the following parameters:");
            Console.WriteLine();
            Console.WriteLine($"Test Parameters Only:     {parameters.TestOnly}");
            Console.WriteLine($"Project File:             {parameters.ProjectPath}");
            Console.WriteLine();
            Console.WriteLine("Project parameters:");
            foreach (var buildArgument in parameters.BuildArguments)
            {
                Console.WriteLine($"     {buildArgument.Key}:\t{buildArgument.Value}");
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
            Console.WriteLine("SSIS Build Engine");
            Console.WriteLine("Syntax:      ssisbuild [project file] [parameters]");
            Console.WriteLine("");
            Console.WriteLine("Description: Builds an SSIS project into an ispac file.");
            Console.WriteLine("             If a project file is not specified, ssisbuild");
            Console.WriteLine("             searches current working directory for a file");
            Console.WriteLine("             with dtproj extension and uses that file.");
            Console.WriteLine("");
            Console.WriteLine("             Parameters (project or package level) are passed");
            Console.WriteLine("             in a form of /<n>=<v> where <n> is a parameter");
            Console.WriteLine("             name, <v> is a parameter value. Each parameter");
            Console.WriteLine("             must be passed separately.");
            Console.WriteLine("Example:");
            Console.WriteLine("     ssisbuild test.dtproj /param1=\"some value\" /param2=2");
        }
    }
}
