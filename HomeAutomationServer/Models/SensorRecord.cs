using System;
using System.ComponentModel.DataAnnotations;
using HomeAutomationLibrary;

namespace HomeAutomationServer.Models
{
    public class SensorRecord
    {
        /// <summary>
        /// database ID
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User custom defined id for each switch.
        /// </summary>
        public int UserDefinedId { get; set; }

        public SensorTypeEnum SensorType { get; set; }

        /// <summary>
        /// true if the sensor is connected
        /// </summary>
        public bool IsConnected { get; set; }

        public DateTime Created { get; set; }

        public virtual ApplicationUser User {get; set;}
    }
}