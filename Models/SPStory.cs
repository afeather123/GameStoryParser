using System.Collections.Generic;

namespace StoryParser.Models
{
    public class SPStory
    {
        public Variables variables;
        public Dictionary<string, Interactable> interactables;

        public SPStory()
        {
            variables = new Variables();
            interactables = new Dictionary<string, Interactable>();
        }
    }
}