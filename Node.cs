namespace StoryParser.Models
{

    public class Node
    {
        public string text;
        public Data[] data;
        public Condition[] setconditions;
        public Redirect[] redirects;
        public Choice[] choices;
    }

}
