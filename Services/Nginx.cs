using System.Text.RegularExpressions;

namespace NginxPanel.Services
{
    public class Nginx
    {
        #region Classes

        public class ConfigFile
        {
            public string Name = string.Empty;
            public string ConfigPath = string.Empty;

            public bool Enabled = false;
            public string FileContents = string.Empty;

            public bool busySaving = false;

            public ConfigFile(string rootPath, string configPath)
            {
                ConfigPath = configPath;
                Name = new FileInfo(configPath).Name;
                Enabled = File.Exists(Path.Combine(rootPath, "sites-enabled", Name));
                FileContents = File.ReadAllText(configPath);
            }
        }

        #endregion

        #region Enums

        public enum enuServiceStatus
        {
            Unknown,
            Running,
            Stopped,
            Failed
        }

        public enum enuServiceAction
        {
            Start,
            Stop,
            Restart
        }

        #endregion

        #region Variables

        private CLI _CLI;

        private string _version = "";
        private string _rootConfig = "";
        private string _rootPath = "";
        private enuServiceStatus _serviceStatus = enuServiceStatus.Unknown;
        private string _serviceDetails = "";

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

        public string ServiceDetails
        {
            get { return _serviceDetails; }
        }

        public string SitesAvailable
        {
            get { return Path.Combine(_rootPath, "sites-available"); }
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

            _CLI.RunCommand("sudo", "nginx -V");

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
            _CLI.RunCommand("sudo", "systemctl status nginx");

            if (!string.IsNullOrWhiteSpace(_CLI.StandardError))
            {
                if (_CLI.StandardError == "Unit nginxd.service could not be found.")
                {
                    _serviceStatus = enuServiceStatus.Unknown;
                }
            }
            else if (!string.IsNullOrWhiteSpace(_CLI.StandardOut))
            {
                _serviceDetails = string.Empty;

                if (_CLI.StandardOut.Contains("Active: inactive"))
                {
                    _serviceStatus = enuServiceStatus.Stopped;
                }
                else if (_CLI.StandardOut.Contains("Active: active"))
                {
                    _serviceStatus = enuServiceStatus.Running;
                }
                else if (_CLI.StandardOut.Contains("Active: failed"))
                {
                    _serviceStatus = enuServiceStatus.Failed;

                    // Parse failures from the output
                    Match match = new Regex(@"(?si)reverse proxy server\.\.\.(?<failures>.*?)\n[^\n]*nginx\.service").Match(_CLI.StandardOut);

                    if (match.Success)
                    {
                        _serviceDetails = "The following errors were returned by the service when attempting to start:<br />";

                        string failures = match.Groups["failures"].Value.Trim();

                        // Attempt to match individual failures
                        MatchCollection matches = new Regex(@"(?si)nginx:\s(?<failure>[^\n]*)").Matches(failures);

                        if (matches.Count > 0)
                        {
                            for (int i = 0; i < matches.Count; i++)
                            {
                                _serviceDetails += "&emsp;" + (i+1).ToString() + ")&nbsp;" + matches[i].Groups["failure"].Value + "<br />";
                            }
                        }
                        else
                        {
                            _serviceDetails += failures.Replace(Environment.NewLine, "<br />");
                        }
                    }
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
            else if (serviceAction == enuServiceAction.Restart)
            {
                _CLI.RunCommand("sudo", "systemctl restart nginx");
            }

            GetServiceStatus();
        }

        public string TestConfig()
        {
            _CLI.RunCommand("sudo", "nginx -t");

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
                _siteConfigs.Sort((a,b) => a.Name.CompareTo(b.Name));
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

            config.Enabled = !config.Enabled;
        }
    }
}