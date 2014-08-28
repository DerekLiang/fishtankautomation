using Microsoft.SqlServer.Server;

namespace HomeAutomationLibrary
{
    public class RestResponseResult<T>
    {
        public T data { get; set; }

        public bool success { get; set; }

        public override string ToString()
        {
            return success.ToString().ToLower() + " " + data;
        }
    }
}