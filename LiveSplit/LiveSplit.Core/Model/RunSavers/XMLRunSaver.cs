﻿using LiveSplit.UI;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;

namespace LiveSplit.Model.RunSavers
{
    public class XMLRunSaver : IRunSaver
    {
        private XmlElement CreateImageElement(XmlDocument document, string elementName, Image image)
        {
            var element = document.CreateElement(elementName);

            if (image != null)
            {
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();

                    bf.Serialize(ms, image);
                    var data = ms.ToArray();
                    var cdata = document.CreateCDataSection(Convert.ToBase64String(data));
                    element.InnerXml = cdata.OuterXml;
                }
            }

            return element;
        }

        public void Save(IRun run, Stream stream)
        {
            var document = new XmlDocument();

            XmlNode docNode = document.CreateXmlDeclaration("1.0", "UTF-8", null);
            document.AppendChild(docNode);

            var parent = document.CreateElement("Run");
            parent.Attributes.Append(SettingsHelper.ToAttribute(document, "version", "1.6.0"));
            document.AppendChild(parent);

            var gameIcon = CreateImageElement(document, "GameIcon", run.GameIcon);
            parent.AppendChild(gameIcon);

            var gameName = document.CreateElement("GameName");
            gameName.InnerText = run.GameName;
            parent.AppendChild(gameName);

            var categoryName = document.CreateElement("CategoryName");
            categoryName.InnerText = run.CategoryName;
            parent.AppendChild(categoryName);

            var offset = document.CreateElement("Offset");
            offset.InnerText = run.Offset.ToString();
            parent.AppendChild(offset);

            var attemptCount = document.CreateElement("AttemptCount");
            attemptCount.InnerText = run.AttemptCount.ToString();
            parent.AppendChild(attemptCount);

            var runHistory = document.CreateElement("AttemptHistory");
            foreach (var attempt in run.AttemptHistory)
            {
                runHistory.AppendChild(attempt.ToXml(document));
            }
            parent.AppendChild(runHistory);

            var segmentElement = document.CreateElement("Segments");
            parent.AppendChild(segmentElement);

            var bf = new BinaryFormatter();

            foreach (var segment in run)
            {
                var splitElement = document.CreateElement("Segment");
                segmentElement.AppendChild(splitElement);

                var name = document.CreateElement("Name");
                name.InnerText = segment.Name;
                splitElement.AppendChild(name);

                var icon = CreateImageElement(document, "Icon", segment.Icon);
                splitElement.AppendChild(icon);

                var splitTimes = document.CreateElement("SplitTimes");
                foreach (var comparison in run.CustomComparisons)
                {
                    var splitTime = segment.Comparisons[comparison].ToXml(document, "SplitTime");
                    splitTime.Attributes.Append(SettingsHelper.ToAttribute(document, "name", comparison));
                    splitTimes.AppendChild(splitTime);
                }
                splitElement.AppendChild(splitTimes);

                var goldSplit = segment.BestSegmentTime.ToXml(document, "BestSegmentTime");
                splitElement.AppendChild(goldSplit);

                var history = document.CreateElement("SegmentHistory");
                foreach (var historySegment in segment.SegmentHistory)
                {
                    history.AppendChild(historySegment.ToXml(document));
                }
                splitElement.AppendChild(history);
            }

            var autoSplitterSettings = document.CreateElement("AutoSplitterSettings");
            if (run.IsAutoSplitterActive())
                autoSplitterSettings.InnerXml = run.AutoSplitter.Component.GetSettings(document).InnerXml;
            parent.AppendChild(autoSplitterSettings);

            var metadata = document.CreateElement("Metadata");
            var platform = document.CreateElement("Platform");
            platform.Attributes.Append(SettingsHelper.ToAttribute(document, "id", run.Metadata.PlatformID));
            metadata.AppendChild(platform);
            var region = document.CreateElement("Region");
            region.Attributes.Append(SettingsHelper.ToAttribute(document, "id", run.Metadata.RegionID));
            metadata.AppendChild(region);
            var variables = document.CreateElement("Variables");
            foreach (var variable in run.Metadata.VariableValueIDs)
            {
                var variableElement = document.CreateElement("Variable");
                variableElement.Attributes.Append(SettingsHelper.ToAttribute(document, "id", variable.Key));
                variableElement.Attributes.Append(SettingsHelper.ToAttribute(document, "valueId", variable.Value));
                variables.AppendChild(variableElement);
            }
            metadata.AppendChild(variables);
            parent.AppendChild(metadata);

            document.Save(stream);
        }
    }
}
