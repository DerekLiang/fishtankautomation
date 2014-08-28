

using System;
using System.Collections.Generic;
using System.Configuration;
//using System.Web.Http;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.WebSockets;
using HomeAutomationLibrary;
using log4net;
using Newtonsoft.Json;

namespace HomeAutomationServer.Controllers
{
    [System.Web.Mvc.AllowAnonymous]
    public class StateController : ApiController
    {
        private static log4net.ILog log = LogManager.GetLogger(typeof(StateController));

        [HttpPost]
        public object State(StateDto data)
        {
            var dtoResult = new Biz().UpdateSensorState(data);

            if (dtoResult == null)
            {
                log.Debug("update failed!");
                return new { success = false };
            }

            log.Debug("update successful:" + JsonConvert.SerializeObject(dtoResult));
            return new { success = true, data = dtoResult };
        }

        [HttpGet]
        public async Task<object> SensorLog(int id)
        {
            var logs = new Biz().getSensorLog(User, id);
            return new {success = true, data= logs};
        }


        [HttpGet]
        public object State()
        {
            return new {success = true, data = new Biz().getAllInfo(User)};
        }

        [HttpPost]
        public object UpdatePumpScheduleTimeSlot([FromBody]List<TimeSlot> timeSlots)
        {
            return new {success = new Biz().UpdatePumpSchedule(User, timeSlots)};
        }
    }
}