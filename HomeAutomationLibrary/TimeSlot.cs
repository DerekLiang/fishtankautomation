using System;

namespace HomeAutomationLibrary
{
    public class TimeSlot
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int Hour { get; set; }
        public bool IsOn { get; set; }

        public TimeSlot(DayOfWeek dayOfWeek, int hour, bool IsOn)
        {
            DayOfWeek = dayOfWeek;
            Hour = hour;
            this.IsOn = IsOn;
        }

        public TimeSlot() : this(DayOfWeek.Sunday, 0, false)
        {
            
        }
    }
}