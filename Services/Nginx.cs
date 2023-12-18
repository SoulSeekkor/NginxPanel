using System.Text.RegularExpressions;

namespace NginxPanel.Services
{
    public class Nginx
    {
        #region Classes

        public class ConfigFile
        {
            public bool Enabled = false;
            public string ConfigPath = string.Empty;

            public ConfigFile(string rootPath, string configPath)
            {
                ConfigPath = configPath;
                Enabled = File.Exists(Path.Combine(rootPath, "sites-enabled", new FileInfo(configPath).Name));
            }
        }

        #endregion

        #region Enums

        public enum enuServiceStatus
        {
            Unknown,
            Running,
            Stopped
        }

        public enum enuServiceAction
        {
            Start,
            Stop
        }

        #endregion

        #region Variables

        private CLI _CLI;

        private string _version = "";
        private string _rootConfig = "";
        private enuServiceStatus _serviceStatus = enuServiceStatus.Unknown;

        private List<ConfigFile> configs = new List<ConfigFile>();

        #endregion

        #region Properties

        public string Version
        {
            get { return _version; }
        }

        public enuServiceStatus ServiceStatus
        {
            get { return _serviceStatus; }
        }

        #endregion

        #region Constructors

        public Nginx(CLI CLI)
        {
            _CLI = CLI;

            GetVersion();
        }

        #endregion

        public void GetVersion()
        {
            _version = "";

            _CLI.RunCommand("nginx", "-V");

            Match match = new Regex("(?si)version:\\s(?<version>.*?)\\n.*--conf-path=(?<config>.*?)\\s").Match(_CLI.StandardError);

            if (match.Success)
            {
                _version = match.Groups["version"].Value;
                _rootConfig = match.Groups["config"].Value;

                GetServiceStatus();
                RefreshFiles();
            }
            else
            {
                _serviceStatus = enuServiceStatus.Unknown;
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

        public void PerformServiceAction(enuServiceAction serviceAction)
        {
            if (serviceAction == enuServiceAction.Start)
            {
                _CLI.RunCommand("sudo", "systemctl start nginx");
            }
            else if (serviceAction == enuServiceAction.Stop)
            {
                _CLI.RunCommand("sudo", "systemctl stop nginx");
            }

            GetServiceStatus();
        }

        public string ReturnServiceActionString()
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

        public string TestConfig()
        {
            _CLI.RunCommand("nginx", "-t");

            return _CLI.StandardOut + _CLI.StandardError;
        }

        public void RefreshFiles()
        {
            configs.Clear();

            string rootPath = new FileInfo(_rootConfig).DirectoryName ?? "";

            if (!string.IsNullOrWhiteSpace(rootPath) && Directory.Exists(rootPath))
            {
                foreach (string file in Directory.GetFiles(Path.Combine(rootPath, "sites-available")))
                {
                    configs.Add(new ConfigFile(rootPath, file));
                }
            }
        }
    }
}