//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------

using nanoFramework.Runtime.Events;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace nanoFramework.Networking
{
    public class NetworkHelpers
    {
        // this is required if the device is connecting through Wi-Fi (typically ESP32 devices)
        private const string c_SSID = "myssid";
        private const string c_AP_PASSWORD = "mypassword";

        private static bool _requiresDateTime;

        /// <summary>
        /// Event signalling that the target has a valid IP address.
        /// </summary>
        static public ManualResetEvent IpAddressAvailable = new ManualResetEvent(false);

        /// <summary>
        /// Event signalling that the target has valid <see cref="DateTime"/>.
        /// </summary>
        static public ManualResetEvent DateTimeAvailable = new ManualResetEvent(false);

        public static void SetupAndConnectNetwork(bool requiresDateTime = false)
        {
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);

            _requiresDateTime = requiresDateTime;

            new Thread(WorkingThread).Start();
        }

        internal static void WorkingThread()
        {
            do
            {
                Debug.WriteLine("Waiting for network available...");

                Thread.Sleep(500);
            }
            while (!NetworkInterface.GetIsNetworkAvailable());

            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();

            if (nis.Length > 0)
            {
                // get the first interface
                NetworkInterface ni = nis[0];

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    // network interface is Wi-Fi
                    Debug.WriteLine("Network connection is: Wi-Fi");

                    Wireless80211Configuration wc = Wireless80211Configuration.GetAllWireless80211Configurations()[ni.SpecificConfigId];

                    // note on checking the 802.11 configuration
                    // on secure devices (like the TI CC3220SF) the password can't be read
                    // so we can't use the code block bellow to automatically set the profile
                    if ((wc.Ssid != c_SSID && wc.Password != c_AP_PASSWORD) &&
                         (wc.Ssid != "" && wc.Password == ""))
                    {
                        // have to update Wi-Fi configuration
                        wc.Ssid = c_SSID;
                        wc.Password = c_AP_PASSWORD;
                        wc.SaveConfiguration();
                    }
                    else
                    {   // Wi-Fi configuration matches
                    }
                }
                else
                {
                    // network interface is Ethernet
                    Debug.WriteLine("Network connection is: Ethernet");
                }

                // check if we have an IP
                CheckIP();

                if (_requiresDateTime)
                {
                    IpAddressAvailable.WaitOne();

                    SetDateTime();
                }
            }
            else
            {
                throw new NotSupportedException("ERROR: there is no network interface configured.\r\nOpen the 'Edit Network Configuration' in Device Explorer and configure one.");
            }
        }

        private static void SetDateTime()
        {
            Debug.WriteLine("Setting up system clock...");

            // if SNTP is available and enabled on target device this can be skipped because we should have a valid date & time
            while (DateTime.UtcNow.Year < 2018)
            {
                Debug.WriteLine("Waiting for valid date time...");
                // wait for valid date & time
                Thread.Sleep(1000);
            }

            DateTimeAvailable.Set();
        }

        private static void CheckIP()
        {
            var myAddress = IPGlobalProperties.GetIPAddress();

            if (myAddress != IPAddress.Any)
            {
                Debug.WriteLine($"We have and IP: {myAddress}");
                IpAddressAvailable.Set();
            }
            else
            {
                Debug.WriteLine("No IP...");
            }
        }

        static void AddressChangedCallback(object sender, EventArgs e)
        {
            CheckIP();
        }
    }
}
