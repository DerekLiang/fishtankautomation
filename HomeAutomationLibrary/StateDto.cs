using System;
using System.Collections.Generic;
using System.Linq;

namespace HomeAutomationLibrary
{
    public class StateDto
    {
        public StateDto()
        {
            ApiKey = string.Empty;
            SensorInfos = new List<SensorInfo>();
            PowerPumpSchedule = new List<TimeSlot>();
            PumpInfo = new PumpInfo();
        }

        public string  ApiKey { get; set; }

        public List<SensorInfo> SensorInfos { get; set; }

        public PumpInfo PumpInfo { get; set; }

        public List<TimeSlot> PowerPumpSchedule { get; set; }

        public static IEnumerable<TimeSlot> getDefaultPowerPumpSchedule()
        {
            foreach (int dayOfWeek in Enumerable.Range(0, 7))
            {
                foreach (var hour in Enumerable.Range(0, 24))
                {
                    if (dayOfWeek == (int)DayOfWeek.Sunday || dayOfWeek == (int)DayOfWeek.Saturday)
                        yield return new TimeSlot((DayOfWeek)dayOfWeek, hour, hour < 7 || hour == 13);
                    else
                        yield return new TimeSlot((DayOfWeek)dayOfWeek, hour,
                                                                           hour == 7
                                                                        || hour == 8
                                                                        || hour == 9
                                                                        || hour > 17
                                                                        );
                }
            }
        }
    }
}