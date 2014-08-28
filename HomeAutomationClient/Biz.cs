using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using HomeAutomationLibrary;
using log4net;
using Newtonsoft.Json;
using Raspberry.IO.GeneralPurpose;
using Raspberry.IO.GeneralPurpose.Behaviors;
using Raspberry.Timers;

namespace HomeAutomationClient
{
    public class Biz : IDisposable
    {
       
        #region public method

        public Biz()
        {
            log.Info("Initializing...");
            InitializationTimeStamp = DateTime.Now;

            url = ConfigurationManager.AppSettings["serverUrl"];
            apiKey = ConfigurationManager.AppSettings["apikey"];
            if (!int.TryParse(ConfigurationManager.AppSettings["statusUpdateIntervalInSeconds"], out statusUpdateIntervalInSeconds))
            {
                statusUpdateIntervalInSeconds = 60;
            }
            if (!int.TryParse(ConfigurationManager.AppSettings["delaySwitchHours"], out delaySwitchHours))
            {
                delaySwitchHours = 2;
            }


            DrySensor = new List<ConnectorPin>(new[]
            {
                ConnectorPin.P1Pin11, //on extension board GPIO0
                ConnectorPin.P1Pin12, //on extension board GPIO1
                ConnectorPin.P1Pin13, //on extension board GPIO2
                ConnectorPin.P1Pin15, //on extension board GPIO3
            });
            WetSensor = new List<ConnectorPin>(new[]
            {
                ConnectorPin.P1Pin16, //on extension board GPIO4
                ConnectorPin.P1Pin18, //on extension board GPIO5
            });
            PowerRelay = ConnectorPin.P1Pin22; //on extension board GPIO6
            PowerDelaySwitch = ConnectorPin.P1Pin7; //on extension board GPIO7

            DelayToTime = DateTime.Now.AddSeconds(-1); //set in the past

            //default schdule, if the server is not ready or not avaialble
            PowerPumpSchedule = StateDto.getDefaultPowerPumpSchedule().ToList();
            IsUsingDefaultPowerPumpSchedule = true;

            log.Info("Setting_pin_property");
            DrySensorInput = DrySensor.ConvertAll(pin => pin.Input());
            WetSensorInput = WetSensor.ConvertAll(pin => pin.Input());
            PowerRelayOutput = PowerRelay.Output();
            PowerDelaySwitchInput = PowerDelaySwitch.Input().PullDown();

            log.Info("Creating_connections");
            Connections = new List<GpioConnection>();
            for (int index = 0; index < DrySensorInput.Count; index++)
            {
                var connectorPin = DrySensorInput[index];
                Connections.Add(getInputConnection(connectorPin, true, "Overflow" + index));
            }

            for (int index = 0; index < WetSensorInput.Count; index++)
            {
                var connectorPin = WetSensorInput[index];
                Connections.Add(getInputConnection(connectorPin, false, "WaterLevel" + index));
            }
            PowerRelayConnection = new GpioConnection(PowerRelayOutput);
            PowerDelaySwitchConnection = new GpioConnection(PowerDelaySwitchInput);
            PowerDelaySwitchConnection.PinStatusChanged += PowerDelaySwitchConnectionOnPinStatusChanged;

            log.Info("Setting_up_power_relay_timer");
            ScheduleCheckTimer = Timer.Create();
            ScheduleCheckTimer.Interval = statusUpdateIntervalInSeconds * 1000;
            ScheduleCheckTimer.Action = OnScheduleCheck;

            log.Info("Initialized");
        }


        public void execute()
        {
            ScheduleCheckTimer.Start(0);
            //nothing need to do
            Console.ReadLine();
        }

        public void Dispose()
        {
            Connections.ForEach(connection => connection.Close());
            PowerRelayConnection.Close();
            PowerDelaySwitchConnection.Close();
            ScheduleCheckTimer.Stop();
        }

        #endregion

        #region property

        private static log4net.ILog log = LogManager.GetLogger(typeof(Program));
        private int statusUpdateIntervalInSeconds;

        /// <summary>
        /// If the value is in the future, the power relay is off regardless the value of time schedule.
        /// 
        /// If the value is already set to the future, and power delay switch is pressed, it will half the delay time.
        /// 
        /// If the value is in the past, it has no effect.
        /// </summary>
        public DateTime DelayToTime { get; set; }

        public List<TimeSlot> PowerPumpSchedule { get; set; }
        public bool IsUsingDefaultPowerPumpSchedule { get; set; }
        public DateTime InitializationTimeStamp { get; set; }

        private GpioConnection PowerDelaySwitchConnection { get; set; }

        private InputPinConfiguration PowerDelaySwitchInput { get; set; }

        private ConnectorPin PowerDelaySwitch { get; set; }

        private ITimer ScheduleCheckTimer { get; set; }

        private GpioConnection PowerRelayConnection { get; set; }

        private OutputPinConfiguration PowerRelayOutput { get; set; }

        private List<InputPinConfiguration> WetSensorInput { get; set; }

        private List<InputPinConfiguration> DrySensorInput { get; set; }

        private List<GpioConnection> Connections { get; set; }
        private ConnectorPin PowerRelay { get; set; }

        private List<ConnectorPin> WetSensor { get; set; }

        private List<ConnectorPin> DrySensor { get; set; }

        private int delaySwitchHours;

        public string url { get; set; }
        
        public string apiKey { get; set; }

        #endregion

        #region Event Handlers
        private void OnScheduleCheck()
        {
            try
            {
                UpdateStatus();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                log.Error(ex.StackTrace);
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    log.Error(ex.Message);
                }

            }

            // we need to update the power pump schedule from server for 5 minutes, 
            if (IsUsingDefaultPowerPumpSchedule && DateTime.Now - InitializationTimeStamp < TimeSpan.FromMinutes(5))
            {
                log.Warn("Power_pump_schedule is not updated yet!");
                return;
            }

            bool isOn = false;
            if (DelayToTime < DateTime.Now)
            {
                isOn = PowerPumpSchedule.First(ps => ps.DayOfWeek == DateTime.Now.DayOfWeek && ps.Hour == DateTime.Now.Hour).IsOn;
            }
            var existPowerRelayState = PowerRelayConnection[PowerRelay];

            if (existPowerRelayState != isOn)
            {
                log.Debug("Set_power_relay_status_to : " + isOn);
                PowerRelayConnection[PowerRelay] = isOn;
            }
        }

        private GpioConnection getInputConnection(InputPinConfiguration connectorPin, bool isDrySensor,
            string friendlyName)
        {
            connectorPin.OnStatusChanged(status =>
            {
                log.Debug(string.Format("Pin ({0}) status_changed_to {1}. Pin type : {2}. Name: '{3}'",
                    connectorPin.Pin.ToString(),
                    status,
                    isDrySensor,
                    friendlyName
                    ));
                if (status != isDrySensor)
                {
                    log.Error(string.Format("Sensor_is_in_wrong_state. wrong_{0}", friendlyName));
                }
            });

            return new GpioConnection(connectorPin);
        }

        private void PowerDelaySwitchConnectionOnPinStatusChanged(object sender, PinStatusEventArgs pinStatusEventArgs)
        {
            var driver = GpioConnectionSettings.DefaultDriver;
            var pressed = driver.Read(PowerDelaySwitch.ToProcessor());

            if (pressed) return;    //only response to press down/or event

            DelayToTime = DateTime.Now.AddHours(delaySwitchHours);

            log.Debug(string.Format("current_delay_to {0} in {1}:{2}.", DelayToTime , Math.Floor((DelayToTime - DateTime.Now).TotalHours).ToString("00"), (DelayToTime - DateTime.Now).Minutes.ToString("00")));

            PowerRelayConnection[PowerRelay] = false;
        }


        /// <summary>
        /// call the web server rest service to do 2 things
        /// 1. update the sensor information to server.
        /// 2. update the pump schedule information on the client.
        /// </summary>
        private void UpdateStatus()
        {
            var data = this.populateDto();

            using (var httpclient = new HttpClient())
            {
                var sContent = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                HttpResponseMessage response = new HttpResponseMessage();
                httpclient.PostAsync(url + "api/state/state", sContent).ContinueWith(responseMessage =>
                {
                    response = responseMessage.Result;
                    log.Debug("rest service call result:" + responseMessage.Result);
                }).Wait();

                RestResponseResult<StateDto> returneDto = new RestResponseResult<StateDto>();

                response.Content.ReadAsStringAsync().ContinueWith(taskJson =>
                {
                    log.Debug("rest service call json result:" + taskJson.Result);
                    returneDto = JsonConvert.DeserializeObject<RestResponseResult<StateDto>>(taskJson.Result);

                }).Wait();

                if (returneDto.success)
                {
                    this.PowerPumpSchedule = returneDto.data.PowerPumpSchedule;
                    IsUsingDefaultPowerPumpSchedule = false;
                    log.Debug("pump schedule is updated.");
                }
                else
                {
                    log.Error("rest call failed:");
                }
            }
        }

        /// <summary>
        /// populate dto with current input/output switch state,
        /// 
        /// NOTE: the reading of low (false), means the sensor is connected (true). This the result of the electronic circuit design.
        /// 
        /// PowerPump Schedule is empty, since we don't send it to the server
        /// 
        /// </summary>
        /// <returns></returns>
        private StateDto populateDto()
        {
            var driver = GpioConnectionSettings.DefaultDriver;

            var result = new StateDto()
            {
                ApiKey = this.apiKey
            };

            var id = 1;

            result.SensorInfos.AddRange(DrySensor.ConvertAll(
                drysensor => new SensorInfo (id++, !driver.Read(drysensor.ToProcessor()),true))
            );

            result.SensorInfos.AddRange(WetSensor.ConvertAll(
                drysensor => new SensorInfo(id++, !driver.Read(drysensor.ToProcessor()), false))
            );

            result.PumpInfo.IsOn = PowerRelayConnection[PowerRelay];

            int delayToTime = (DelayToTime - DateTime.Now ).Seconds;
            result.PumpInfo.DelayToTimeInSeconds = delayToTime <0 ? delayToTime : 0;
            
            return result;
        }

        #endregion

    }
}