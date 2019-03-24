using System;

namespace ContainelDll
{
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string FunName { get; set; }
    }

}
