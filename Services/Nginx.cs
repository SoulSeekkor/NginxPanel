using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Timers;

namespace NginxPanel.Services
{
	public class Nginx
	{
		#region Classes

		public class ConfigFile
		{
			public enum enuConfigType
			{
				Site = 0,
				Shared = 1
			}

			private string _fileContents = string.Empty;

			public enuConfigType ConfigType { get; set; }
			public string Name = string.Empty;
			public string ConfigPath = string.Empty;

			public bool Enabled = false;
			public string FileContents
			{
				get { return _fileContents; }
				set {
					_fileContents = value;
					ContentsDirty = true;
				}
			}
			public bool ContentsDirty = false;

			public bool busySaving = false;

			public ConfigFile(enuConfigType configType, string rootPath, string configPath)
			{
				ConfigType = configType;
				ConfigPath = configPath;
				Name = new FileInfo(configPath).Name;

				if (configType == enuConfigType.Site)
					Enabled = File.Exists(Path.Combine(rootPath, "sites-enabled", Name));

				_fileContents = File.ReadAllText(configPath);
			}

			public async Task Save()
			{
				await Task.Run(() =>
				{
					File.WriteAllText(ConfigPath, _fileContents);
					ContentsDirty = false;
				} );
			}

			public async Task Revert()
			{
				await Task.Run(() =>
				{
					_fileContents = File.ReadAllText(ConfigPath);
					ContentsDirty = false;
				});
			}
		}

		#endregion

		#region Enums

		public enum enuServiceStatus
		{
			[Description("Checking...")]
			Checking,
			Unknown,
			[Description("Not Found")]
			NotFound,
			[Description("Not Installed")]
			NotInstalled,
			Running,
			[Description("Starting...")]
			Starting,
			[Description("Restarting...")]
			Restarting,
			Stopped,
			[Description("Stopping...")]
			Stopping,
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

		private enuServiceStatus _serviceStatus = enuServiceStatus.Checking;
		private string _serviceDetails = "";
		private string _lastTestResults = "";

		private List<ConfigFile> _configs = new List<ConfigFile>();

		private System.Timers.Timer _refreshService = new System.Timers.Timer(1500);

		public event ServiceStatusChangedHandler? ServiceStatusChanged;
		public delegate void ServiceStatusChangedHandler();

		#endregion

		#region Properties

		public bool Installed
		{
			get { return !(_rootConfig == "") && Directory.Exists(_rootPath); }
		}

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

		public string LastTestResults
		{
			get { return _lastTestResults; }
		}

		public string SitesAvailable
		{
			get { return Path.Combine(_rootPath, "sites-available"); }
		}

		public string SitesEnabled
		{
			get { return Path.Combine(_rootPath, "sites-enabled"); }
		}

		public string SharedFiles
		{
			get { return Path.Combine(_rootPath, "shared-files"); }
		}

		public List<ConfigFile> Configs
		{
			get { return _configs; }
		}

		#endregion

		#region Constructors

		public Nginx(CLI CLI)
		{
			_CLI = CLI;
			Refresh();

			_refreshService.Elapsed += _refreshService_Elapsed;
			_refreshService.Start();
		}

		private void _refreshService_Elapsed(object? sender, ElapsedEventArgs e)
		{
			try
			{
				_refreshService.Stop();
				GetServiceStatus();
			}
			finally
			{
				// Make sure our timer is started again no matter what
				_refreshService.Start();
			}
		}

		#endregion

		public void Refresh()
		{
			_rootConfig = string.Empty;
			_rootPath = string.Empty;

			GetVersion();
			GetServiceStatus();
			RefreshFiles();
		}

		private void GetVersion()
		{
			_version = string.Empty;

			_CLI.RunCommand("nginx -V");

			Match match = new Regex("(?si)version:\\s(?<version>.*?)\\n.*--conf-path=(?<config>.*?)\\s").Match(_CLI.StandardError);

			if (match.Success)
			{
				_version = match.Groups["version"].Value;
				_rootConfig = match.Groups["config"].Value;
				_rootPath = new FileInfo(_rootConfig).DirectoryName ?? "";
			}
			else
			{
				_serviceStatus = enuServiceStatus.Unknown;
			}
		}

		public void GetServiceStatus()
		{
			enuServiceStatus oldStatus = _serviceStatus;

			if (Installed)
			{
				_CLI.RunCommand("systemctl status nginx");

				if (!string.IsNullOrWhiteSpace(_CLI.StandardError))
				{
					// Default to unknown for existence of errors
					_serviceStatus = enuServiceStatus.Unknown;

					if (_CLI.StandardError == "Unit nginxd.service could not be found.")
					{
						_serviceStatus = enuServiceStatus.NotFound;
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
									_serviceDetails += "&emsp;" + (i + 1).ToString() + ")&nbsp;" + matches[i].Groups["failure"].Value + "<br />";
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
			else
				_serviceStatus = enuServiceStatus.NotInstalled;

			if (!(oldStatus == _serviceStatus) && !(ServiceStatusChanged is null))
				ServiceStatusChanged();
		}

		public void PerformServiceAction(enuServiceAction serviceAction)
		{

			if (serviceAction == enuServiceAction.Start)
				_serviceStatus = enuServiceStatus.Starting;
			else if (serviceAction == enuServiceAction.Stop)
				_serviceStatus = enuServiceStatus.Stopping;
			else if (serviceAction == enuServiceAction.Restart)
				_serviceStatus = enuServiceStatus.Restarting;

			if (!(ServiceStatusChanged is null))
				ServiceStatusChanged();

			if (serviceAction == enuServiceAction.Start)
				_CLI.RunCommand("systemctl start nginx");
			else if (serviceAction == enuServiceAction.Stop)
				_CLI.RunCommand("systemctl stop nginx");
			else if (serviceAction == enuServiceAction.Restart)
				_CLI.RunCommand("systemctl restart nginx");

			GetServiceStatus();
		}

		public void TestConfig()
		{
			_lastTestResults = string.Empty;

			_CLI.RunCommand("nginx -t");

			if (_CLI.StandardError.Contains("test is successful"))
			{
				_lastTestResults = "Successful test.";
			}
			else
			{
				_lastTestResults = _CLI.StandardError.Replace(Environment.NewLine, "<br />");
			}
		}

		public void RefreshFiles()
		{
			_configs.Clear();

			if (Installed)
			{
				// Load site configs first
				foreach (string file in Directory.GetFiles(SitesAvailable))
				{
					_configs.Add(new ConfigFile(ConfigFile.enuConfigType.Site, _rootPath, file));
				}

				// Load shared configs next, make sure the folder exists first
				if (!Directory.Exists(SharedFiles))
					Directory.CreateDirectory(SharedFiles);

				foreach (string file in Directory.GetFiles(SharedFiles))
				{
					_configs.Add(new ConfigFile(ConfigFile.enuConfigType.Shared, _rootPath, file));
				}

				// Sort the configs
				_configs.OrderBy(x => x.ConfigType).ThenBy(x => x.Name);
			}
		}

		public void ToggleSiteEnabled(ConfigFile config)
		{
			if (config.ConfigType == ConfigFile.enuConfigType.Site)
			{
				if (config.Enabled)
				{
					// Remove from sites-enabled
					_CLI.RunCommand("rm \"" + Path.Combine(SitesEnabled, config.Name) + "\"");
				}
				else
				{
					// Add to sites-enabled
					_CLI.RunCommand("ln -s \"" + config.ConfigPath + "\" \"" + Path.Combine(SitesEnabled, config.Name) + "\"");
				}

				config.Enabled = !config.Enabled;
			}
		}
	}
}