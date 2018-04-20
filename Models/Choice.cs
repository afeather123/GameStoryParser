namespace StoryParser.Models
{

    public class Choice
    {
        public Redirect[] redirects;
        public string text;
        public Condition[] setconditions;
        public Condition[] conditions;
    }

}