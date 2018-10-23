using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleWebCopy.Extensions
{
    public static class EventExtensions
    {
        public static void Trigger(this EventHandler eventHandler, object sender, EventArgs eventArgs)
        {
            if (eventHandler != null)
            {
                eventHandler.Invoke(sender, eventArgs);
            }
        }

        public static void Trigger<TEventArgs>(this EventHandler<TEventArgs> eventHandler, object sender, TEventArgs eventArgs) where TEventArgs : EventArgs
        {
            if (eventHandler != null)
            {
                eventHandler.Invoke(sender, eventArgs);
            }
        }
    }
}
