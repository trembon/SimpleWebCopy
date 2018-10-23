using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleWebCopy.Events
{
    public class ThreadCompleteEventArgs : EventArgs
    {
        public int Thread { get; set; }

        public ThreadCompleteEventArgs(int thread)
        {
            this.Thread = thread;
        }
    }
}
