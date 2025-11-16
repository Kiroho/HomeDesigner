using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace HomeDesigner.Loader
{
    public static class XmlLoader
    {

        public static string GetHomesteadFolder()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = Path.Combine(docs, "Guild Wars 2", "Homesteads");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

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
        /// Clamp-Funktion für float
        /// </summary>
        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Konvertiert einen rot-String aus XML (X Y Z in Radiant) in einen Quaternion.
        /// Reihenfolge: Pitch (X), Yaw (Y), Roll (Z)
        /// </summary>
        public static Quaternion EulerRadiantStringToQuaternion(string rotString)
        {
            if (string.IsNullOrWhiteSpace(rotString))
                return Quaternion.Identity;

            var parts = rotString.Split(' ');
            if (parts.Length != 3)
                return Quaternion.Identity;

            float pitch = 0f, yaw = 0f, roll = 0f;

            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out pitch); // X
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out yaw);   // Y
            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out roll);  // Z

            roll = -roll;

            // Wichtig: CreateFromYawPitchRoll erwartet Yaw, Pitch, Roll
            return Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
        }

        /// <summary>
        /// Zerlegt einen Quaternion in Yaw (Y), Pitch (X) und Roll (Z) in Radiant.
        /// Entspricht der Reihenfolge von Quaternion.CreateFromYawPitchRoll (Y–X–Z).
        /// </summary>
        public static void QuaternionToYawPitchRoll(Quaternion q, out float yaw, out float pitch, out float roll)
        {
            q = Quaternion.Normalize(q);

            // Quelle: DirectXMath (XMQuaternionToYawPitchRoll), angepasst auf XNA-Notation

            // Yaw (Y)
            float t0 = 2.0f * (q.W * q.Y + q.X * q.Z);
            float t1 = 1.0f - 2.0f * (q.Y * q.Y + q.X * q.X);
            yaw = (float)Math.Atan2(t0, t1);

            // Pitch (X)
            float t2 = 2.0f * (q.W * q.X - q.Z * q.Y);
            t2 = Clamp(t2, -1.0f, 1.0f);
            pitch = (float)Math.Asin(t2);

            // Roll (Z)
            float t3 = 2.0f * (q.W * q.Z + q.Y * q.X);
            float t4 = 1.0f - 2.0f * (q.Z * q.Z + q.X * q.X);
            roll = (float)Math.Atan2(t3, t4);
            roll = -roll;
        }


        /// <summary>
        /// Konvertiert einen Quaternion in einen rot-String für XML (X Y Z in Radiant)
        /// </summary>
        public static string QuaternionToEulerRadiantString(Quaternion q)
        {
            QuaternionToYawPitchRoll(q, out float yaw, out float pitch, out float roll);

            // XML-Format: "X Y Z" → Pitch, Yaw, Roll
            return $"{pitch.ToString("F6", CultureInfo.InvariantCulture)} " +
                   $"{yaw.ToString("F6", CultureInfo.InvariantCulture)} " +
                   $"{roll.ToString("F6", CultureInfo.InvariantCulture)}";
        }


        public static float mapToPlayer(float x)
        {
            const float a = 0.0254f;
            const float b = 0.041617201797244596f;
            return a * x;
        }

        public static double playerToMap(double x)
        {
            const double a = 0.0254;
            const double b = 0.041617201797244596;
            return x / a;
        }

        /// <summary>
        /// Speichert eine Liste von BlueprintObjects als XML-Datei im angegebenen Ordner und Dateiname.
        /// </summary>
        public static XDocument SaveBlueprintObjectsToXml(List<BlueprintObject> objects, String mapId)
        {
            if (objects == null)
                return null;
            
            var mapName = "unknown";
            if(mapId == "1596")
            {
                mapName = "Comosus Isle";
            }
            else if(mapId == "1558")
            {
                mapName = "Hearth's Glow";
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("Decorations",
                    new XAttribute("version", "1"),
                    new XAttribute("mapId", "1596"),
                    new XAttribute("mapName", mapName),
                    new XAttribute("type", "0")
                )
            );

            var root = doc.Root;

            foreach (var obj in objects)
            {
                var posX = playerToMap(obj.Position.X);
                var posY = playerToMap(obj.Position.Y);
                var posZ = playerToMap(obj.Position.Z)*-1;
                string posString = $"{posX.ToString("F6", CultureInfo.InvariantCulture)} " +
                                   $"{posY.ToString("F6", CultureInfo.InvariantCulture)} " +
                                   $"{posZ.ToString("F6", CultureInfo.InvariantCulture)}";


                //string posString = $"{obj.Position.X.ToString("F6", CultureInfo.InvariantCulture)} " +
                //                   $"{obj.Position.Y.ToString("F6", CultureInfo.InvariantCulture)} " +
                //                   $"{obj.Position.Z.ToString("F6", CultureInfo.InvariantCulture)}";

                string rotString = QuaternionToEulerRadiantString(obj.RotationQuaternion);

                string sclString = obj.Scale.ToString("F6", CultureInfo.InvariantCulture);
                //string sclString = "1.000000";

                root.Add(
                    new XElement("prop",
                        new XAttribute("id", obj.Id),
                        new XAttribute("name", obj.Name),
                        new XAttribute("pos", posString),
                        new XAttribute("rot", rotString),
                        new XAttribute("scl", sclString)
                    )
                );
            }

            return doc;
        }




        /// <summary>
        /// Lädt eine XML-Datei und gibt eine Liste von BlueprintObjects zurück.
        /// </summary>
        public static List<BlueprintObject> LoadBlueprintObjectsFromXml(string filePath)
        {
            var result = new List<BlueprintObject>();
            if (!File.Exists(filePath))
                throw new Exception("File not found");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            var propNodes = xmlDoc.SelectNodes("/Decorations/prop");
            if (propNodes == null)
                throw new Exception("XML file has the wrong formate");

            foreach (XmlNode node in propNodes)
            {
                if (node.Attributes == null) continue;

                var obj = new BlueprintObject();

                obj.ModelKey = node.Attributes["id"]?.Value ?? "unknown";
                obj.Name = node.Attributes["name"]?.Value;

                if (int.TryParse(node.Attributes["id"].Value, out int intId))
                {
                    obj.Id = intId;
                }
                else
                {
                    throw new Exception("XML file has the wrong formate");
                }



                //Debug.WriteLine($"[--------] Obj Modelkey = {obj.Name}");
                //Debug.WriteLine($"[--------] Obj ID = {obj.Id}");

                // Position
                var posStr = node.Attributes["pos"]?.Value;
                if (!string.IsNullOrEmpty(posStr))
                {
                    var parts = posStr.Split(' ');
                    if (parts.Length == 3 &&
                        float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                        float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                    {
                        
                        x = mapToPlayer(x);
                        y = mapToPlayer(y);
                        z = mapToPlayer(z);

                        obj.Position = new Vector3(x, y, -z);
                    }
                    else
                    {
                        throw new Exception("XML file has the wrong formate");
                    }
                }





                // Rotation
                var rotStr = node.Attributes["rot"]?.Value;
                if (!string.IsNullOrEmpty(rotStr))
                {
                    obj.RotationQuaternion = EulerRadiantStringToQuaternion(rotStr);
                }

                // Scale
                var sclStr = node.Attributes["scl"]?.Value;
                if (!string.IsNullOrEmpty(sclStr) &&
                    float.TryParse(sclStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float scl))
                {
                    obj.Scale = scl;
                }

                result.Add(obj);
            }

            return result;
        }




        /////////___________________________________________________________________
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
