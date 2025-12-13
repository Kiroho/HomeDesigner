using Microsoft.Xna.Framework;

namespace HomeDesigner
{
    public class BlueprintObject
    {
        public int InternalId { get; set; }
        public bool IsOriginal { get; set; }
        public string ModelKey { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public float Scale { get; set; }
        public Matrix CachedWorld { get; set; }
        public bool Selected { get; set; } = false;
        public BoundingBox BoundingBox { get; set; }

        public string payloadPT { get; set; }
        public string payloadV { get; set; }
        public string payloadValue { get; set; }

        public Quaternion RotationQuaternion { get; set; } = Quaternion.Identity;

        public BlueprintObject()
        {
            RotationQuaternion = Quaternion.Identity;
        }
    }
}
