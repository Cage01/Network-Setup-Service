using System;
using System.Management;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Text.RegularExpressions;

namespace Network_Adaptation
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer();
        string WIFI_NAME = null;
        string ETHERNET_NAME = null;

        bool doActivate = false;
        bool isActivated = false;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var query = new ObjectQuery("SELECT * FROM Win32_NetworkAdapter");
            using (var searcher = new ManagementObjectSearcher(query))
            {
                var queryCollection = searcher.Get();

                foreach (ManagementObject m in queryCollection)
                {
                    try
                    {
                        string name = m["Name"].ToString();

                        if (CleanupString(name).Contains("wifi"))
                        {
                            WIFI_NAME = name;
                        }

                        else if (!IsVPN(name) && CleanupString(name).Contains("ethernet"))
                        {
                            ETHERNET_NAME = name;
                        }
                    }
                    catch (NullReferenceException e) {  /* Do Nothing */ }
                }
            }

            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 2000; //number in milisecinds  
            timer.Enabled = true;
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            var query = new ObjectQuery("SELECT * FROM Win32_NetworkAdapter");
            using (var searcher = new ManagementObjectSearcher(query))
            {
                var queryCollection = searcher.Get();

                foreach (ManagementObject m in queryCollection)
                {

                    string name = m["Name"].ToString();
                
                    if (name == WIFI_NAME)
                    {
                        try
                        {
                            bool wifiEnabled = Boolean.Parse(m["NetEnabled"].ToString());
                            

                            if(isActivated && wifiEnabled)
                            {
                                doActivate = false;
                            }

                        } catch(NullReferenceException ex)
                        {
                            doActivate = true;
                        }
                    }
                
                    else if (name == ETHERNET_NAME)
                    {
                        try
                        {
                            if(doActivate && !isActivated)
                            {
                                m.InvokeMethod("Enable", null);
                                isActivated = true;
                            }

                            if(!doActivate && isActivated)
                            {
                                m.InvokeMethod("Disable", null);
                                isActivated = false;
                            }
                        }
                        catch (NullReferenceException ex)
                        {

                        }
                    }

                }
            }
        }

        protected override void OnStop()
        {
        }

        static string CleanupString(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled).ToLower();
        }

        static bool IsVPN(string str)
        {
            if (str.ToUpper().Contains("VPN"))
                return true;
            else
                return false;
        }
    }
}
