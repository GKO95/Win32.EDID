using System;
using System.Collections.Generic;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;

namespace EDID.Core.cs
{
    class Program
    {
        static void Main(string[] args)
        {
            var displays = new EnumerateDevices(DisplayMonitor.GetDeviceSelector());
        }
    }

    class EnumerateDevices
    {
        /*
            DeviceInterface[0]
            * Name: "C27F390"
            * Id: "\\\\?\\DISPLAY#SAM0D32#5&2325f110&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}"
            
            DeviceInterface[1]
            * Name: "2D FHD LG TV"
            * Id: "\\\\?\\DISPLAY#GSM59C6#5&2325f110&0&UID4353#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}"
        */

        List<DeviceInformation> deviceList;

        public EnumerateDevices(string selector) 
        {
            EnumDevices(selector);
            EnumDisplay();
        }

        private async void EnumDevices(string selector)
        {
            var devices = await DeviceInformation.FindAllAsync(selector);
            deviceList = new List<DeviceInformation>();

            if (devices.Count > 0)
            {
                for (var i = 0; i < devices.Count; i++)
                {
                    deviceList.Add(devices[i]);
                }
            }
        }

        private async void EnumDisplay()
        {
            var display = await DisplayMonitor.FromInterfaceIdAsync(deviceList[0].Id);
            byte[] EDID = display.GetDescriptor(DisplayMonitorDescriptorKind.Edid);
            Console.WriteLine(EDID.ToString());
        }


        public DeviceInformation this[int index]
        {
            get { return deviceList[index]; }
        }
    }
}
