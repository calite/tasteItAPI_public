using Neo4j.Driver;
using Neo4jClient;

namespace TasteItApi.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int difficulty { get; set; }
        public string image { get; set; }
        public  string dateCreated { get; set; }
        public  string country { get; set; }
        public float rating { get; set; }
        public List<string> ingredients { get; set; }
        public List<string> tags { get; set; }
        public List<string> steps { get; set; }

    }

}
