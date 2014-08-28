using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using HomeAutomationLibrary;
using log4net;
using Newtonsoft.Json;
using Raspberry.IO.GeneralPurpose;

namespace HomeAutomationClient
{
    class Program
    {
        private static log4net.ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
#if DEBUG
            log.Info("make sure the the debugger is runing as super user:");
#endif
            try
            {
                using (var biz = new Biz())
                {
                    biz.execute();   
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                log.Error(ex.StackTrace);
                throw;
            }
//
//            log.Info("use pin6 to indicate the water level");
//            var waterLevel = ConnectorPin.P1Pin18.Input();
//            waterLevel.OnStatusChanged((connected) => log.Info("water level :" + connected));
//            var WaterLevelConnection = new GpioConnection(waterLevel);
//
//            log.Info("connect LED to pin7 and see the LED blinks");
//
//            var led1 = ConnectorPin.P1Pin7.Output();
//
//            var connection = new GpioConnection(led1);
//
//            for (var i = 0; i < 1000; i++)
//            {
//                connection.Toggle(led1);
//                Thread.Sleep(250);
//            }
//
//            WaterLevelConnection.Close();
//            connection.Close();
//

        }
    }
}
