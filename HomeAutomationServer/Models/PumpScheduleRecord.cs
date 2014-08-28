using System;
using System.ComponentModel.DataAnnotations;

namespace HomeAutomationServer.Models
{
    public class PumpScheduleRecord
    {
        /// <summary>
        /// database ID
        /// </summary>
        [Key]
        public int Id { get; set; }


        /// <summary>
        /// Monday to Statureday
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// true, iff the pump should be on
        /// </summary>
        public bool IsOn { get; set; }

        /// <summary>
        /// 0-23 hour per day
        /// </summary>
        public int Hour { get; set; }

        public virtual ApplicationUser User { get; set; }
    }
}