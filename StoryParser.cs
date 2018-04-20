using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using StoryParser.Models;
using StoryParser.Utilities;

namespace StoryParser
{


    public class SParser
    {

        private SPStory story = new SPStory();
        private Interactable currentInteractable;
        private Messenger<string> dialogueMessenger = new Messenger<string>();
        private SubscriptionService<string> dialogueSubscriptionService;
        private Messenger<int> redirectMessenger = new Messenger<int>();
        private SubscriptionService<int> redirectSubscriptionService;
        private Messenger<List<Choice>> choiceMessenger = new Messenger<List<Choice>>();
        private SubscriptionService<List<Choice>> choiceSubscriptionService;
        private Messenger lastNodeMessenger = new Messenger();
        private SubscriptionService lastNodeSubscriptionService;
        private Messenger finishInteractionMessenger = new Messenger();
        private SubscriptionService finishInteractionSubscriptionService;
        private Dictionary<string, SubscriptionSystem<object>> dataSubscriptions = new Dictionary<string, SubscriptionSystem<object>>();

        public Subscription SubscribeToData(string dataName, Action<System.Object> callback)
        {
            if (dataSubscriptions[dataName] == null)
            {
                Messenger<object> messenger = new Messenger<object>();
                SubscriptionService<object> subService = messenger.SubscriptionService();
                dataSubscriptions[dataName] = new SubscriptionSystem<object>(messenger, subService);
                return subService.Subscribe(callback);
            }
            return dataSubscriptions[dataName].subscriptionService.Subscribe(callback);
        }

        public void UnSubscribeToData(string dataName, Action<System.Object> callback)
        {
            if (dataSubscriptions[dataName] != null)
            {
                dataSubscriptions[dataName].subscriptionService.Unsubscribe(callback);
            }
        }

        private void SendDataMessages(Data[] data)
        {
            foreach (var datum in data)
            {
                if (dataSubscriptions[datum.name] != null)
                {
                    dataSubscriptions[datum.name].messenger.SendMessage(datum.value);
                }
            }
        }

        Condition[] setconditions;

        public SubscriptionService<string> Dialogue
        {
            get
            {
                if (dialogueSubscriptionService == null)
                {
                    dialogueSubscriptionService = dialogueMessenger.SubscriptionService();
                }
                return dialogueSubscriptionService;
            }
        }

        public SubscriptionService<int> Redirect
        {
            get
            {
                if (redirectSubscriptionService == null)
                {
                    redirectSubscriptionService = redirectMessenger.SubscriptionService();
                }
                return redirectSubscriptionService;
            }
        }

        public SubscriptionService<List<Choice>> Choices
        {
            get
            {
                if (choiceSubscriptionService == null)
                {
                    choiceSubscriptionService = choiceMessenger.SubscriptionService();
                }
                return choiceSubscriptionService;
            }
        }

        public SubscriptionService LastNode
        {
            get
            {
                if (lastNodeSubscriptionService == null)
                {
                    lastNodeSubscriptionService = lastNodeMessenger.SubscriptionService();
                }
                return lastNodeSubscriptionService;
            }
        }

        public SubscriptionService FinishedInteraction
        {
            get
            {
                if (finishInteractionSubscriptionService == null)
                {
                    finishInteractionSubscriptionService = finishInteractionMessenger.SubscriptionService();
                }
                return finishInteractionSubscriptionService;
            }
        }

        public void LoadStory(string text)
        {
            StoryData storyData = JsonConvert.DeserializeObject<StoryData>(text);
            story = storyData.gameData;
        }

        public void LoadVariables(string variableFile)
        {
            story.variables = JsonConvert.DeserializeObject<Variables>(variableFile);
        }

        public void LoadInteractable(string name, string interactableFile)
        {
            story.interactables[name] = JsonConvert.DeserializeObject<Interactable>(interactableFile);
        }

        public void Interact(string name)
        {
            setconditions = null;
            if (!story.interactables.ContainsKey(name))
            {
                Console.WriteLine("No interactable by that name! Check your spelling");
                return;
            }
            currentInteractable = story.interactables[name];
            int? nodeID = null;
            foreach (var entryPoint in currentInteractable.entryPoints)
            {
                if (CheckConditions(entryPoint.conditions))
                {
                    nodeID = entryPoint.nodeID;
                }
            }
            if (nodeID == null)
            {
                Console.WriteLine("No valid node! Error");
                return;
            }
            NextNode((int)nodeID);
        }

        public void NextNode(int nodeID)
        {
            if (setconditions != null)
            {
                SetVariables(setconditions);
            }
            var dialogueNode = currentInteractable.nodes[nodeID];
            setconditions = dialogueNode.setconditions;
            int? foundRedirect = CheckRedirects(dialogueNode.redirects);
            bool foundChoice = false;
            if (foundRedirect == null)
            {
                foundChoice = true;
                if (CheckChoices(dialogueNode).Count <= 0)
                {
                    foundChoice = false;
                }
            }
            if (foundRedirect == null && !foundChoice)
            {
                lastNodeMessenger.SendMessage();
            }
            dialogueMessenger.SendMessage(dialogueNode.text);
            SendDataMessages(dialogueNode.data);

        }

        public void PickChoice(Choice choice)
        {
            SetVariables(choice.setconditions);
            int? nextNodeID = CheckRedirects(choice.redirects);
            if (nextNodeID != null)
            {
                NextNode((int)nextNodeID);
            }
        }

        public void FinishInteraction()
        {
            if (setconditions != null)
            {
                SetVariables(setconditions);
            }
            finishInteractionMessenger.SendMessage();
        }

        private int? CheckRedirects(Redirect[] redirects)
        {
            if (redirects != null && redirects.Length > 0)
            {
                int? nodeID = null;
                foreach (var redirect in redirects)
                {
                    bool validRedirect = CheckConditions(redirect.conditions);
                    if (validRedirect)
                        nodeID = redirect.nodeID;
                }
                if (nodeID != null)
                {
                    redirectMessenger.SendMessage((int)nodeID);
                    return nodeID;
                }
            }
            return null;
        }

        private List<Choice> CheckChoices(Node dialogueNode)
        {
            List<Choice> validChoices = new List<Choice>();
            foreach (var choice in dialogueNode.choices)
            {
                if (CheckConditions(choice.conditions))
                {
                    validChoices.Add(choice);
                }
            }
            choiceMessenger.SendMessage(validChoices);
            return validChoices;
        }

        private bool CheckConditions(Condition[] conditions)
        {
            foreach (var condition in conditions)
            {
                if (condition.Operator == "=")
                {
                    if (!condition.value.Equals(story.variables.vars[condition.varID]))
                        return false;
                }
                else if (condition.Operator == "!=")
                {
                    if (condition.value.Equals(story.variables.vars[condition.varID]))
                        return false;
                }
                else if (condition.Operator == ">")
                {
                    float value = Convert.ToSingle(condition.value);
                    float varValue = Convert.ToSingle(story.variables.vars[condition.varID]);
                    if (varValue <= value)
                        return false;
                }
                else if (condition.Operator == "<")
                {
                    float value = Convert.ToSingle(condition.value);
                    float varValue = Convert.ToSingle(story.variables.vars[condition.varID]);
                    if (varValue >= value)
                        return false;
                }
                else if (condition.Operator == ">=")
                {
                    float value = Convert.ToSingle(condition.value);
                    float varValue = Convert.ToSingle(story.variables.vars[condition.varID]);
                    if (varValue < value)
                        return false;
                }
                else if (condition.Operator == "<=")
                {
                    float value = Convert.ToSingle(condition.value);
                    float varValue = Convert.ToSingle(story.variables.vars[condition.varID]);
                    if (varValue > value)
                        return false;
                }
            }
            return true;
        }

        private void SetVariables(Condition[] setconditions)
        {
            foreach (var setter in setconditions)
            {
                if (setter.Operator == "=")
                {
                    story.variables.vars[setter.varID] = setter.value;
                }
                else if (setter.Operator == "+=")
                {
                    float value = Convert.ToSingle(story.variables.vars[setter.varID]);
                    story.variables.vars[setter.varID] = value + Convert.ToSingle(setter.value);
                }
                else if (setter.Operator == "-=")
                {
                    float value = Convert.ToSingle(story.variables.vars[setter.varID]);
                    story.variables.vars[setter.varID] = value - Convert.ToSingle(setter.value);
                }
                else if (setter.Operator == "*=")
                {
                    float value = Convert.ToSingle(story.variables.vars[setter.varID]);
                    story.variables.vars[setter.varID] = value * Convert.ToSingle(setter.value);
                }
                else if (setter.Operator == "/=")
                {
                    float value = Convert.ToSingle(story.variables.vars[setter.varID]);
                    story.variables.vars[setter.varID] = value + Convert.ToSingle(setter.value);
                }
                else if (setter.Operator == "toggle")
                {
                    story.variables.vars[setter.varID] = !Convert.ToBoolean(story.variables.vars[setter.varID]);
                }
            }
        }

        public object CheckVariable(string name)
        {
            if(story.variables.varNames.ContainsKey(name))
            {
                object variable = story.variables.vars[story.variables.varNames[name]];
                return variable;
            } else
            {
                Console.WriteLine("Could not find variable of name '" + name + "'. Check for spelling errors or wrong names.");
                return null;
            }
        }

        public void SetVariable(string name, object value)
        {
            if (story.variables.varNames.ContainsKey(name))
            {
                story.variables.vars[story.variables.varNames[name]] = value;
            }
            else
            {
                Console.WriteLine("Could not find variable of name '" + name + "'. Check for spelling errors or wrong names.");
            }
        }
    }

    

}