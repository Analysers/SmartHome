using System;

namespace SmartHome.Utils
{
    public class EventProxy<TEventArgs> where TEventArgs : EventArgs
    {
        private event EventHandler<TEventArgs> ProxiedEvent;

        public static EventProxy<TEventArgs> operator +(EventProxy<TEventArgs> eventProxy,
            EventHandler<TEventArgs> eventHandler)
        {
            eventProxy.ProxiedEvent += eventHandler;
            return eventProxy;
        }

        public void Invoke(object sender, TEventArgs e)
        {
            ProxiedEvent?.Invoke(sender, e);
        }
    }
}