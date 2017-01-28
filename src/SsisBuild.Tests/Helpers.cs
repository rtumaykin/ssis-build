using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SsisBuild.Tests
{
    public class Helpers
    {
        public static void SetBuildArgumentsValue(BuildArguments buldArguments, string propertyName, object value)
        {
            (typeof(BuildArguments).GetProperty(propertyName, BindingFlags.NonPublic) ?? typeof(BuildArguments).GetProperty(propertyName)).SetValue(buldArguments, value);
        }
    }
}
