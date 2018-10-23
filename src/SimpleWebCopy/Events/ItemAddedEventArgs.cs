using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleWebCopy.Events
{
    public class ItemAddedEventArgs : EventArgs
    {
        public string ItemURL { get; set; }

        public ItemAddedEventArgs(string itemURL)
        {
            this.ItemURL = itemURL;
        }
    }
}
