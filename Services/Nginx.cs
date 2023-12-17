﻿using System;

namespace NginxPanel.Services
{
    public class Nginx
    {
        public enum enuServiceStatus
        {
            Unknown,
            Running,
            Stopped
        }

        private CLI _CLI;

        private string _version = "";
        public enuServiceStatus _serviceStatus = enuServiceStatus.Unknown;

        public string Version
        {
            get { return _version; }
        }

        public enuServiceStatus ServiceStatus
        {
            get { return _serviceStatus; }
        }

        public Nginx(CLI CLI)
        {
            _CLI = CLI;

            GetVersion();
        }

        public void GetVersion()
        {
            _version = "";

            _CLI.RunCommand("nginx", "-v");

            if (_CLI.StandardError.Contains("nginx version:"))
            {
                _version = _CLI.StandardError.Substring(_CLI.StandardError.IndexOf("/") + 1);

                GetServiceStatus();
            }
        }

        public void GetServiceStatus()
        {
            _CLI.RunCommand("systemctl", "status nginx");

            if (!string.IsNullOrWhiteSpace(_CLI.StandardError))
            {
                if (_CLI.StandardError == "Unit nginxd.service could not be found.")
                {
                    _serviceStatus = enuServiceStatus.Unknown;
                }
            }
            else if (!string.IsNullOrWhiteSpace(_CLI.StandardOut))
            {
                if (_CLI.StandardOut.Contains("inactive"))
                {
                    _serviceStatus = enuServiceStatus.Stopped;
                }
                else if (_CLI.StandardOut.Contains("active"))
                {
                    _serviceStatus = enuServiceStatus.Running;
                }
            }
        }

        public string GetServiceAction()
        {
            if (_serviceStatus == enuServiceStatus.Running)
            {
                return "Stop";
            }
            else if (_serviceStatus == enuServiceStatus.Stopped)
            {
                return "Start";
            }
            else
            {
                return "N/A";
            }
        }
    }
}