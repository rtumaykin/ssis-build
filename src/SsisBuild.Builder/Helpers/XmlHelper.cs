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

namespace SsisBuild.Core.Helpers
{
    public static class XmlHelpers
    {
        public static class Schemas
        {
            public static string SSIS = "www.microsoft.com/SqlServer/SSIS";
            public static string DTS = "www.microsoft.com/SqlServer/Dts";
        }
        public static XmlNamespaceManager GetNameSpaceManager(this XmlNode node)
        {
            var ownerDocument = node.GetDocument();

            if (ownerDocument == null)
                throw new Exception("Failed to fetch owner document for Xml Node.");

            var namespaceManager = new XmlNamespaceManager(ownerDocument.NameTable);
            namespaceManager.AddNamespace("SSIS", Schemas.SSIS);
            namespaceManager.AddNamespace("DTS", Schemas.DTS);
            return namespaceManager;
        }

        public static XmlDocument GetDocument(this XmlNode node)
            => node.NodeType == XmlNodeType.Document ? node as XmlDocument : node.OwnerDocument;

        
        internal static XmlAttribute GetAttribute(this XmlNode rootNode, string name) => rootNode.Attributes?.GetNamedItem(name, Schemas.SSIS) as XmlAttribute ??
                                                                                    rootNode.Attributes?.GetNamedItem(name) as XmlAttribute;
   

    }
}
