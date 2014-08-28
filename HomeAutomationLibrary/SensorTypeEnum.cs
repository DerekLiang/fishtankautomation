namespace HomeAutomationLibrary
{
    /// <summary>
    /// IsDrySensor, true if the correct sensor state <see cref="SensorInfo.IsConnected"/> should be false.
    /// 
    ///              false if the correct sensor state <see cref="SensorInfo.IsConnected"/> should be true.
    /// </summary>
    public enum SensorTypeEnum
    {
        WetSensor = 1,
        DrySensor = 2,
        PumpSwitch = 3,
    } 
}