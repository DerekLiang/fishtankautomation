using System;

namespace HomeAutomationServer.Controllers
{
    public class SensorLogDto
    {
        public int id { get; set; }
        public DateTime created { get; set; }
        public bool isConnected { get; set; }

        public SensorLogDto(int id, DateTime created, bool isConnected)
        {
            this.id = id;
            this.created = created;
            this.isConnected = isConnected;
        }
    }
}