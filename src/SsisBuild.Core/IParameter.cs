using System;
using System.Xml;

namespace SsisBuild.Core
{
    public interface IParameter
    {
        string Name { get; }
        Type ParameterDataType { get; }
        bool Sensitive { get; }
        ParameterSource Source { get; }
        string Value { get; }

        void SetValue(string value, ParameterSource source);
    }
}