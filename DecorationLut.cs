using System;
using System.Collections.Generic;

namespace HomeDesigner
{
    public class DecorationLUT
    {
        public Dictionary<int, Decorations> decorations;
        public DecorationLUT()
        {
            this.decorations = new Dictionary<int, Decorations>();
        }
    }
    public class Decorations
    {
        public int id;
        public string name;
        public string description;
        public int max_count;
        public int icon;
        public List<int> categories;
        public Decorations()
        {
            this.id = 0;
            this.name = null;
            this.description = null;
            this.max_count = 0;
            this.icon = 0;
            this.categories = new List<int>();
        }
    }
}