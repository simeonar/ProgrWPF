using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace ProgrWPF.Data
{
    public static class CmmDataLoader
    {
        public static List<CmmPoint> LoadPoints(string filePath)
        {
            var points = new List<CmmPoint>();
            var doc = new XmlDocument();
            doc.Load(filePath);

            foreach (XmlNode node in doc.SelectNodes("//Point"))
            {
                var coordinatesNode = node.SelectSingleNode("Coordinates");
                var featureNode = node.SelectSingleNode("Feature");

                // More robust check to ensure all nodes and attributes exist
                if (node.Attributes?["Name"] != null && 
                    coordinatesNode?.Attributes?["X"] != null &&
                    coordinatesNode?.Attributes?["Y"] != null &&
                    coordinatesNode?.Attributes?["Z"] != null &&
                    featureNode?.Attributes?["Type"] != null &&
                    featureNode?.Attributes?["Description"] != null)
                {
                    points.Add(new CmmPoint
                    {
                        Name = node.Attributes["Name"].Value,
                        X = double.Parse(coordinatesNode.Attributes["X"].Value, CultureInfo.InvariantCulture),
                        Y = double.Parse(coordinatesNode.Attributes["Y"].Value, CultureInfo.InvariantCulture),
                        Z = double.Parse(coordinatesNode.Attributes["Z"].Value, CultureInfo.InvariantCulture),
                        FeatureType = featureNode.Attributes["Type"].Value,
                        FeatureDescription = featureNode.Attributes["Description"].Value
                    });
                }
            }

            return points;
        }
    }
}
