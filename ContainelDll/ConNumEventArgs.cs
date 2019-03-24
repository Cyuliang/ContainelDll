using System;

namespace ContainelDll
{
    public class ConNumEventArgs : EventArgs
    {
        public DateTime TriggerTime { get; set; }
        public int LaneNum { get; set; }
        public int ContainerType { get; set; }
        public string ContainerNum1 { get; set; }
        public string CheckNum1 { get; set; }
        public string ISO1 { get; set; }
        public string ContainerNum2 { get; set; }
        public string CheckNum2 { get; set; }
        public string ISO2 { get; set; }
    }
}
