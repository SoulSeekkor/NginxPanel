using System.Text.RegularExpressions;

namespace NginxPanel.Services
{
    public class Nginx
    {
        #region Classes

        public class ConfigFile
        {
            public bool Enabled = false;
            public string Name = string.Empty;
            public string ConfigPath = string.Empty;

            public ConfigFile(string rootPath, string configPath)
            {
                ConfigPath = configPath;
                Name = new FileInfo(configPath).Name;
                Enabled = File.Exists(Path.Combine(rootPath, "sites-enabled", Name));
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
        private string _rootPath = "";
        private enuServiceStatus _serviceStatus = enuServiceStatus.Unknown;

        private List<ConfigFile> _siteConfigs = new List<ConfigFile>();

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

        public List<ConfigFile> SiteConfigs
        {
            get { return _siteConfigs; }
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
                _rootPath = new FileInfo(_rootConfig).DirectoryName ?? "";

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
            _siteConfigs.Clear();

            if (!string.IsNullOrWhiteSpace(_rootPath) && Directory.Exists(_rootPath))
            {
                foreach (string file in Directory.GetFiles(Path.Combine(_rootPath, "sites-available")))
                {
                    _siteConfigs.Add(new ConfigFile(_rootPath, file));
                }
            }
        }

        public void ToggleEnabled(ConfigFile config)
        {
            if (config.Enabled)
            {
                // Remove from sites-enabled
                _CLI.RunCommand("sudo", "rm " + Path.Combine(_rootPath, "sites-enabled", config.Name));
            }
            else
            {
                // Add to sites-enabled
                _CLI.RunCommand("sudo", "ln -s " + config.ConfigPath + " " + Path.Combine(_rootPath, "sites-enabled", config.Name));
            }
        }
    }
}