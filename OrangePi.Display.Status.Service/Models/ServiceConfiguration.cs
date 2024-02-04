using System.Text;

namespace OrangePi.Display.Status.Service.Models
{
    public class ServiceConfiguration
    {
        public ServiceConfiguration()
        {
            
        }

        /// <summary>
        /// Display device bus id
        /// </summary>
        public int BusId { get; set; }

        /// <summary>
        /// Display device address
        /// </summary>
        public string DeviceAddressHex { get; set; }

        /// <summary>
        /// Display change interval
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Flip the display
        /// </summary>
        public bool Rotate { get; set; }

        /// <summary>
        /// How long the display will be on
        /// </summary>
        public int TimeOn { get; set; }

        public TimeSpan TimeOnTimeSpan
        {
            get
            {
                return TimeSpan.FromSeconds(TimeOn);
            }
        }

        public TimeSpan IntervalTimeSpan
        {
            get
            {
                return TimeSpan.FromSeconds(Interval);
            }
        }

        public int DeviceAddress { get
            {
                return Convert.ToInt32(this.DeviceAddressHex, 16);
            } 
        }
    }
}
