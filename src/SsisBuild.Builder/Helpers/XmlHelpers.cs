using System;
using System.Xml;

namespace SsisBuild.Helpers
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
