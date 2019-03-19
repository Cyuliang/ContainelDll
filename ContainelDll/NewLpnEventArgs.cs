using System;

namespace ContainelDll
{
    public class NewLpnEventArgs:EventArgs
    {
        public int TriggerTime { get; set; }
        public int LaneNum { get; set; }
        public int Lpn { get; set; }
        public int Color { get; set; }
    }
}
