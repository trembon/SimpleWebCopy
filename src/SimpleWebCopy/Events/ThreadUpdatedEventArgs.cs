using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleWebCopy.Events
{
    public class ThreadUpdatedEventArgs : EventArgs
    {
        public int Thread { get; set; }

        public string Item { get; set; }

        public string Status { get; set; }

        public ThreadUpdatedEventArgs(int thread, string item, string status)
        {
            this.Thread = thread;
            this.Item = item;
            this.Status = status;
        }
    }
}
