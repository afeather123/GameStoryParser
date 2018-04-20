using System.Collections.Generic;
using System;

namespace StoryParser.Utilities
{

    public class Messenger<T>
    {

        List<Action<T>> listeners = new List<Action<T>>();

        private void Subscribe(Action<T> callback)
        {
            listeners.Add(callback);
        }

        private void Unsubscribe(Action<T> callback)
        {
            listeners.Add(callback);
        }

        public void SendMessage(T message)
        {
            foreach (var listener in listeners)
            {
                listener.Invoke(message);
            }
        }

        public int NumbeOfSubscribers()
        {
            return listeners.Count;
        }

        public SubscriptionService<T> SubscriptionService()
        {
            return new SubscriptionService<T>(Subscribe, Unsubscribe);
        }
    }

    public class SubscriptionService<T>
    {
        Action<Action<T>> subscribe;
        Action<Action<T>> unsubscribe;

        public SubscriptionService(Action<Action<T>> subscribe, Action<Action<T>> unsubscribe)
        {
            this.subscribe = subscribe;
            this.unsubscribe = unsubscribe;
        }

        public Subscription Subscribe(Action<T> callback)
        {
            subscribe.Invoke(callback);
            return new Subscription(() => { Unsubscribe(callback); });
        }

        public void Unsubscribe(Action<T> callback)
        {
            unsubscribe.Invoke(callback);
        }
    }

    public class Messenger
    {

        List<Action> listeners = new List<Action>();

        private void Subscribe(Action callback)
        {
            listeners.Add(callback);
        }

        private void Unsubscribe(Action callback)
        {
            listeners.Add(callback);
        }

        public void SendMessage()
        {
            foreach (var listener in listeners)
            {
                listener.Invoke();
            }
        }

        public SubscriptionService SubscriptionService()
        {
            return new SubscriptionService(Subscribe, Unsubscribe);
        }

        public int NumbeOfSubscribers()
        {
            return listeners.Count;
        }
    }

    public class SubscriptionService
    {
        Action<Action> subscribe;
        Action<Action> unsubscribe;

        public SubscriptionService(Action<Action> subscribe, Action<Action> unsubscribe)
        {
            this.subscribe = subscribe;
            this.unsubscribe = unsubscribe;
        }

        public Subscription Subscribe(Action callback)
        {
            subscribe.Invoke(callback);
            return new Subscription(() => { Unsubscribe(callback); });
        }

        public void Unsubscribe(Action callback)
        {
            unsubscribe.Invoke(callback);
        }
    }

    public class Subscription
    {
        Action unsubscribe;
        public Subscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe;
        }

        public void Unsubscribe()
        {
            unsubscribe.Invoke();
        }

    }

    public class SubscriptionSystem<T>
    {
        public Messenger<T> messenger;
        public SubscriptionService<T> subscriptionService;

        public SubscriptionSystem(Messenger<T> messenger, SubscriptionService<T> subscriptionService)
        {
            this.messenger = messenger;
            this.subscriptionService = subscriptionService;
        }
    }

}