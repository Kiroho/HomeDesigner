using Microsoft.Xna.Framework;

namespace HomeDesigner
{
    public class BlueprintObject
    {
        public string ModelKey { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public float Scale { get; set; }
        public Matrix CachedWorld { get; set; }
        public bool Selected { get; set; } = false;
        public BoundingBox BoundingBox { get; set; }
    }
}
