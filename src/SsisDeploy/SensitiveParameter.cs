using System;

namespace SsisDeploy
{
    public class SensitiveParameter
    {
        public string Name { get; set; }
        public Type DataType { get; set; }
        public string Value { get; set; }
    }
}