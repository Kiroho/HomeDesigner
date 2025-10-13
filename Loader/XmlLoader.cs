using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace HomeDesigner.Loader
{
    public static class XmlLoader
    {
        /// <summary>
        /// Lädt eine XML-Datei und gibt sie als XDocument zurück.
        /// </summary>
        public static XDocument LoadXml(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Dateipfad darf nicht leer sein.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Die Datei wurde nicht gefunden: {filePath}");

            return XDocument.Load(filePath);
        }

        /// <summary>
        /// Gibt den mapName aus dem root-Element zurück.
        /// </summary>
        public static string GetMapName(XDocument doc)
        {
            return doc.Root?.Attribute("mapName")?.Value ?? "Unbekannte Karte";
        }


        /// <summary>
        /// Liest den Map-Namen aus der XML-Datei aus.
        /// </summary>
        public static string GetMapNameFromPath(string filePath)
        {
            try
            {
                var doc = LoadXml(filePath);
                var decorations = doc.Element("Decorations");
                if (decorations != null && decorations.Attribute("mapName") != null)
                {
                    return decorations.Attribute("mapName").Value;
                }
            }
            catch
            {
                // Ignorieren → Fallback unten
            }

            return "Unbekannte Karte";
        }





        /// <summary>
        /// Kombiniert alle &lt;prop&gt;-Elemente mehrerer XML-Dateien in ein neues XML-Dokument.
        /// </summary>
        /// <param name="filePaths">Liste von XML-Dateipfaden</param>
        /// <returns>Ein XDocument mit einem &lt;Decorations&gt;-Element, das alle Props enthält</returns>
        public static XDocument MergeTemplates(IEnumerable<XDocument> templates)
        {
            if (templates == null || !templates.Any())
                throw new ArgumentNullException(nameof(templates));

            XDocument firstTemplate = templates.FirstOrDefault(t => t?.Element("Decorations") != null);
            if (firstTemplate == null)
                throw new InvalidOperationException("Keine gültigen Templates vorhanden.");

            // Root-Element aus der ersten Vorlage übernehmen
            var firstRoot = firstTemplate.Element("Decorations");
            var mergedDecorations = new XElement(firstRoot.Name, firstRoot.Attributes());

            // mapId der ersten Vorlage merken
            var firstMapId = firstRoot.Attribute("mapId")?.Value;

            foreach (var doc in templates)
            {
                if (doc == null) continue;

                try
                {
                    var decorations = doc.Element("Decorations");
                    if (decorations == null) continue;

                    // Nur Templates mit gleicher mapId übernehmen
                    var mapId = decorations.Attribute("mapId")?.Value;
                    if (mapId != firstMapId) continue;

                    foreach (var prop in decorations.Elements("prop"))
                    {
                        mergedDecorations.Add(new XElement(prop));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Mergen eines Templates: {ex.Message}");
                }
            }

            return new XDocument(mergedDecorations);
        }













        /// <summary>
        /// Fügt einen neuen prop hinzu.
        /// </summary>
        public static void AddProp(XDocument doc, string id, string name, string pos, string rot, string scl)
        {
            if (doc.Root == null) throw new InvalidOperationException("XML-Dokument hat kein Root-Element.");

            XElement newProp = new XElement("prop",
                new XAttribute("id", id),
                new XAttribute("name", name),
                new XAttribute("pos", pos),
                new XAttribute("rot", rot),
                new XAttribute("scl", scl)
            );

            doc.Root.Add(newProp);
        }

        /// <summary>
        /// Entfernt ein Prop anhand seiner Attribute.
        /// </summary>
        public static void RemoveProp(XDocument doc, string id, string name, string pos, string rot, string scl)
        {
            if (doc.Root == null) throw new InvalidOperationException("XML-Dokument hat kein Root-Element.");

            var toRemove = doc.Root.Elements("prop").FirstOrDefault(p =>
                (string)p.Attribute("id") == id &&
                (string)p.Attribute("name") == name &&
                (string)p.Attribute("pos") == pos &&
                (string)p.Attribute("rot") == rot &&
                (string)p.Attribute("scl") == scl
            );

            toRemove?.Remove();
        }

        /// <summary>
        /// Speichert das XML-Dokument an einem Pfad.
        /// </summary>
        public static void SaveXml(XDocument doc, string filePath)
        {
            doc.Save(filePath);
        }

        /// <summary>
        /// Prüft, ob ein prop schon existiert (alle Attribute gleich).
        /// </summary>
        public static bool PropExists(XDocument doc, string id, string name, string pos, string rot, string scl)
        {
            if (doc.Root == null) return false;

            return doc.Root.Elements("prop").Any(p =>
                (string)p.Attribute("id") == id &&
                (string)p.Attribute("name") == name &&
                (string)p.Attribute("pos") == pos &&
                (string)p.Attribute("rot") == rot &&
                (string)p.Attribute("scl") == scl
            );
        }
    }
}
