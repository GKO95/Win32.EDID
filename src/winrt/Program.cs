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
            //==============================================================
            // EXTRACTING MONITOR EDID
            //==============================================================
            var devices = new EnumerateDevices(DisplayMonitor.GetDeviceSelector());
            devices.EnumDisplay();
        }
    }

    /*
        THE CURRENT C# project experiments on acquiring EDID using WinRT instead of Win32 API.
        Meaning, this C# application is only available on Windows 10. For those wishes to
        execute on Windows 7, 8, and 8.1, please refer to the .NET Framework 4 project.
    */
    class EnumerateDevices
    {
        readonly List<DeviceInformation> deviceList = new List<DeviceInformation>();

        /*
            THE CONSTRUCTOR automatically enumerates devices upon instantiation.
            Here, the project separated the class for acquiring EDID due to asynchronous
            property the WinRT API "FindAllAsync()" and "FromInterfaceIdAsync()" has.
        */
        public EnumerateDevices(string selector) 
        {
            EnumDevices(selector);
        }

        /*
            THE WINRT API "DeviceInformation.FindAllAsync()" finds every active device interface, and
            return them as a collection of DeviceInformation. When the Advanced Query Syntax(AQS)
            is given such as "DisplayMonitor.GetDeviceSelector()", the API only enumerates specified DeviceInformation.
        */
        private async void EnumDevices(string selector = null)
        {
            var devices = await DeviceInformation.FindAllAsync(selector);

            if (devices.Count > 0)
                for (var i = 0; i < devices.Count; i++) 
                    deviceList.Add(devices[i]);
        }

        /*
            GET EACH DisplayMonitor OBJECT from the DeviceInformation of the device interface ID,
            then acquire its descriptor (in this case, EDID).
        */
        public async void EnumDisplay()
        {
            for (var index = 0; index < deviceList.Count; index++)
            {
                if (index != 0) Console.WriteLine();

                DisplayMonitor display = await DisplayMonitor.FromInterfaceIdAsync(deviceList[index].Id);
                byte[] EDID = display.GetDescriptor(DisplayMonitorDescriptorKind.Edid);

                string hexBuffer = BitConverter.ToString(EDID).Replace("-", " ").ToLower();
                Console.WriteLine(string.Format("{0} : {1}", deviceList[index].Name, deviceList[index].Id));
                Console.Write(hexBuffer + "\n");
            }
        }
    }
}
