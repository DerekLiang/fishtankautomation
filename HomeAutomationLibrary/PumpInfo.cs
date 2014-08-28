using System;

namespace HomeAutomationLibrary
{
    public class PumpInfo
    {
        public bool IsOn { get; set; }

        /// <summary>
        /// 0 seconds, iff no delay
        /// </summary>
        public double DelayToTimeInSeconds { get; set; }
    }
}