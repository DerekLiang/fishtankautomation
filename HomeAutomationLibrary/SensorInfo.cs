using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeAutomationLibrary
{
    public class SensorInfo
    {
        public SensorInfo() : this(-1, false, SensorTypeEnum.DrySensor)
        {
            
        }

        public SensorInfo(int id, bool isConnected, SensorTypeEnum sensorType)
        {

            this.Id = id;
            this.IsConnected = isConnected;
            this.SensorType = sensorType;
        }

        public SensorInfo(int id, bool isConnected, bool isDrySensor) : 
            this(id, isConnected, isDrySensor ? SensorTypeEnum.DrySensor : SensorTypeEnum.WetSensor)
        {

        }

        public int Id { get; set; }

        /// <summary>
        /// true, iff the sensor is connected, otherwise it will return false.
        /// </summary>
        public bool IsConnected { get; set; }

        public SensorTypeEnum SensorType { get; set; }
    }
}
