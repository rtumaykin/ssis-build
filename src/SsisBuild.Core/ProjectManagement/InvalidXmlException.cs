//-----------------------------------------------------------------------
//   Copyright 2017 Roman Tumaykin
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Xml;

namespace SsisBuild.Core.ProjectManagement
{
    public class InvalidXmlException : Exception
    {
        public string NodeXml { get; }
        public string Path { get; }
        public override string Message { get; }

        public InvalidXmlException(string message, XmlNode errorNode) 
        {
            NodeXml = errorNode?.OuterXml;
            Path = GetPath(errorNode);
            Message = $"{message}. (Path: {Path})";
        }

        private static string GetPath(XmlNode errorNode)
        {
            var nodeWalker = errorNode;
            var path = string.Empty;
            while (nodeWalker.NodeType != XmlNodeType.Document && nodeWalker.ParentNode != null)
            {
                path = $"{nodeWalker.Name}/{path}";
                nodeWalker = nodeWalker.ParentNode;
            }

            return path;
        }
    }
}
