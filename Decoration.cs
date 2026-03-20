using System.Collections.Generic;

namespace HomeDesigner
{
    public class Decoration
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int max_count { get; set; }
        public string icon { get; set; }
        public List<int> categories { get; set; }

        public Decoration()
        {
            categories = new List<int>();
        }
    }
}
