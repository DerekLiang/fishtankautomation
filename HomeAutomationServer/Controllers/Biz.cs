using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc.Html;
using HomeAutomationLibrary;
using HomeAutomationServer.Models;
using log4net;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Newtonsoft.Json;

namespace HomeAutomationServer.Controllers
{
    public class Biz
    {
        private static log4net.ILog log = LogManager.GetLogger(typeof(Biz));

        public Biz()
        {
            UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>());
        }

        public ApplicationUser getAppUser(IPrincipal user)
        {
            ApplicationUser currentApplicationUser = null;
            if (user.Identity.IsAuthenticated)
            {
                currentApplicationUser = UserManager.FindById(user.Identity.GetUserId());

                createUserApiKeyIfNotExists(currentApplicationUser);
            }
            return currentApplicationUser;
        }

        private void createUserApiKeyIfNotExists(ApplicationUser currentApplicationUser)
        {
            if (currentApplicationUser != null && currentApplicationUser.ApiKey == Guid.Empty)
            {
                currentApplicationUser.ApiKey = Guid.NewGuid();
                UserManager.Update(currentApplicationUser);
            }
        }


        public UserManager<ApplicationUser> UserManager { get; set; }


        /// <summary>
        /// return true, iff the sensor is update correctly
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public StateDto UpdateSensorState(StateDto data)
        {
            Guid apiKeyGuid;

            if (Guid.TryParse(data.ApiKey, out apiKeyGuid))
            {
                using (var db = new ApplicationDbContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.ApiKey == apiKeyGuid);
                    if (user == null) 
                    {
                        return null;
                    }

                    return UpdateSensorStateCore(data, db, user);
                }
            }
            return null;
        }

        private static StateDto UpdateSensorStateCore(StateDto data, ApplicationDbContext db, ApplicationUser user)
        {
            UpdateSensorRecords(data.SensorInfos, db, user);

            UpdatePumpRecord(data.PumpInfo, db, user);

            UpdateUser(db, user, data.PumpInfo.DelayToTimeInSeconds, data.PumpInfo.IsOn);

            data.PowerPumpSchedule = GetPumpSchdule(db, user);

            db.SaveChanges();

            return data;
        }

        private static void UpdateUser(ApplicationDbContext db, ApplicationUser user, double pumpDelayToTime, bool isPumpOn)
        {
            user.LastUpdate = DateTime.UtcNow;
            user.PumpDelayTo = DateTime.UtcNow.AddSeconds(pumpDelayToTime);
            user.IsPumpOn = isPumpOn;
            db.Users.AddOrUpdate(user);
        }

        /// <summary>
        /// return pump schedule in the database, if any.
        /// Otherwise return default schedule.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static List<TimeSlot> GetPumpSchdule(ApplicationDbContext db, ApplicationUser user)
        {
            var dbPumpSchedule = (from s in db.PumpScheduleRecords
                                 where s.User.Id == user.Id
                                 select s).ToList();

            return dbPumpSchedule.Any() ? 
                        dbPumpSchedule.ConvertAll(timeslot => new TimeSlot(timeslot.DayOfWeek, timeslot.Hour, timeslot.IsOn)) : 
                        StateDto.getDefaultPowerPumpSchedule().ToList();
        }

        private static void UpdateSensorRecords(List<SensorInfo> sensorInfos, ApplicationDbContext db, ApplicationUser user)
        {
            int[] sensorIds = sensorInfos.ConvertAll(sensorInfo => sensorInfo.Id).ToArray();

            //the latest records for each sensors
            var sensorRecords = (   from s in db.SensorRecords
                                    where sensorIds.Contains(s.UserDefinedId) && s.User.Id == user.Id
                                            && (s.SensorType == SensorTypeEnum.WetSensor || s.SensorType == SensorTypeEnum.DrySensor)
                                    group s by s.UserDefinedId into g
                                    select g.OrderByDescending(t => t.Created).FirstOrDefault()
                                    )
                                .ToArray();

            foreach (var sensorInfo in sensorInfos)
            {
                var sensorRecord = sensorRecords.FirstOrDefault(sr => sr.UserDefinedId == sensorInfo.Id);

                //update only if there is no records or state was changed.
                if (sensorRecord == null || sensorRecord.IsConnected != sensorInfo.IsConnected)
                {
                    log.Debug(string.Format("insert new sensor record:{0} state {1}", sensorInfo.Id, sensorInfo.IsConnected));
                    db.SensorRecords.Add(new SensorRecord()
                    {
                        UserDefinedId = sensorInfo.Id,
                        SensorType = sensorInfo.SensorType,
                        IsConnected = sensorInfo.IsConnected,
                        Created = DateTime.UtcNow,
                        User = user,
                    });
                }
            }
        }
        
        private static void UpdatePumpRecord(PumpInfo pumpInfo, ApplicationDbContext db, ApplicationUser user)
        {
            var pumpRecord = (from p in db.SensorRecords
                                where p.SensorType == SensorTypeEnum.PumpSwitch && p.User.Id == user.Id
                                orderby p.Created descending 
                                select p)
                            .FirstOrDefault();

            if (pumpRecord == null || pumpRecord.IsConnected != pumpInfo.IsOn)
            {
                log.Debug(string.Format("insert new pump state :{0} ", pumpInfo.IsOn));

                db.SensorRecords.Add(new SensorRecord()
                {
                    UserDefinedId = 0,
                    SensorType = SensorTypeEnum.PumpSwitch,
                    IsConnected = pumpInfo.IsOn,
                    Created = DateTime.UtcNow,
                    User = user,
                });
            }
        }

        public StateDto getAllInfo(IPrincipal user)
        {
            using (var db = new ApplicationDbContext())
            {
                var appUser = this.getUserIdOrDefaultUserId(user, db);

                var sensorRecords = (from s in db.SensorRecords
                                     where s.User.Id == appUser.Id 
                                     group s by s.UserDefinedId into g
                                     select g.OrderByDescending(t => t.Created).FirstOrDefault()
                                     ).ToList();

                var pumpSchedule = (from s in db.PumpScheduleRecords
                                    where s.User.Id == appUser.Id
                                   select s).ToList();

                var result = new StateDto()
                {
                    PowerPumpSchedule = new List<TimeSlot>(
                            pumpSchedule.ConvertAll(s => new TimeSlot(s.DayOfWeek, s.Hour, s.IsOn))
                        ),
                    PumpInfo = new PumpInfo()
                    {
                        DelayToTimeInSeconds = Math.Max(((appUser.PumpDelayTo ?? new DateTime(2000, 1, 1)) - DateTime.UtcNow).TotalSeconds,0),
                        IsOn = appUser.IsPumpOn ?? false,
                    },
                    SensorInfos = sensorRecords.ConvertAll(s => new SensorInfo(s.UserDefinedId, s.IsConnected, s.SensorType)),
                    ApiKey = user.Identity.IsAuthenticated ? appUser.ApiKey.ToString() : string.Empty
                };

                if (result.PowerPumpSchedule.Count == 0)
                {
                    result.PowerPumpSchedule = StateDto.getDefaultPowerPumpSchedule().ToList();
                }

                NormalizeSchedule(result.PowerPumpSchedule);

//                //debug code.
//                var r = new Random();
//                result.SensorInfos = new List<SensorInfo>();
//                result.SensorInfos.Add(new SensorInfo(0, r.Next(2) == 0, SensorTypeEnum.DrySensor));
//                result.SensorInfos.Add(new SensorInfo(1, r.Next(2) == 0, SensorTypeEnum.DrySensor));
//                result.SensorInfos.Add(new SensorInfo(2, r.Next(2) == 0, SensorTypeEnum.WetSensor));
//                result.SensorInfos.Add(new SensorInfo(3, r.Next(2) == 0, SensorTypeEnum.DrySensor));
//                result.SensorInfos.Add(new SensorInfo(4, r.Next(2) == 0, SensorTypeEnum.WetSensor));
//                result.SensorInfos.Add(new SensorInfo(5, r.Next(2) == 0, SensorTypeEnum.DrySensor));

                return result;
            }
        }

        /// <summary>
        /// add any missing data in the case it is missing in the database.
        /// </summary>
        /// <param name="powerPumpSchedule"></param>
        private void NormalizeSchedule(List<TimeSlot> powerPumpSchedule)
        {
            for (int day = 0; day < 7; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    if (!powerPumpSchedule.Any(timeslot => (int) timeslot.DayOfWeek == day && timeslot.Hour == hour))
                    {
                        powerPumpSchedule.Add(new TimeSlot((DayOfWeek) day, hour, false));
                    }
                }
            }

        }

        public bool UpdatePumpSchedule(IPrincipal user, List<TimeSlot> powerPumpSchedule)
        {
            var userId = user.Identity.GetUserId();

            if (userId == null) return false;

            log.Debug("before insert/update pump schedule data:" + JsonConvert.SerializeObject(powerPumpSchedule));

            NormalizeSchedule(powerPumpSchedule);

            log.Debug("insert/update pump schedule data:" + JsonConvert.SerializeObject(powerPumpSchedule));

            using (var db = new ApplicationDbContext())
            {
                var currentApplicationUser = (from u in db.Users
                                             where u.Id == userId
                                             select u)
                                             .Single();

                var dbPumpSchedule = (from s in db.PumpScheduleRecords
                                      where s.User.Id == userId
                                      select s).ToList();

                db.PumpScheduleRecords.RemoveRange(dbPumpSchedule);

                // insert it into database, as the intialization value
                foreach (var timeSlot in powerPumpSchedule)
                {
                    db.PumpScheduleRecords.Add(new PumpScheduleRecord()
                    {
                        DayOfWeek = timeSlot.DayOfWeek,
                        Hour = timeSlot.Hour,
                        IsOn = timeSlot.IsOn,
                        User = currentApplicationUser,
                    });
                }
                db.SaveChanges();
            }
            return true;
        }

        public List<SensorLogDto> getSensorLog(IPrincipal user, int sensorId)
        {
            using (var db = new ApplicationDbContext())
            {
                var appUser = getUserIdOrDefaultUserId(user, db);

                var currentApplicationUser = (from u in db.Users
                                              where u.Id == appUser.Id
                                             select u)
                                             .Single();

                var sensorData = from sensorRecord in db.SensorRecords
                                 where sensorRecord.UserDefinedId == sensorId && currentApplicationUser.Id == sensorRecord.User.Id
                                 orderby sensorRecord.Created descending 
                                 select sensorRecord;

//                //debug code
//                return new List<SensorLogDto>(new[]
//                {
//                    new SensorLogDto(sensorId, DateTime.UtcNow.AddMinutes(-2), true), 
//                    new SensorLogDto(sensorId, DateTime.UtcNow.AddMinutes(-12), false), 
//                    new SensorLogDto(sensorId, DateTime.UtcNow.AddHours(-1), true), 
//                    new SensorLogDto(sensorId, DateTime.UtcNow.AddHours(-2.1), false), 
//                });

                return sensorData.Take(50).ToList().ConvertAll(s => new SensorLogDto(s.Id, s.Created, s.IsConnected));
            }

        }

        /// <summary>
        /// get the application user or the first user in the database.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private ApplicationUser getUserIdOrDefaultUserId(IPrincipal user, ApplicationDbContext db)
        {
            return user.Identity.IsAuthenticated ? getAppUser(user) : db.Users.OrderByDescending(u => u.LastUpdate).First();
        }
    }
}