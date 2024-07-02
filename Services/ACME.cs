using System.Text;
using System.Text.RegularExpressions;

namespace NginxPanel.Services
{
	public class ACME
	{
		#region Classes

		public class Certificate
		{
			public string MainDomain { get { return ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_Domain); } }
			public string SANDomains { get { return ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_Alt); } }
			public string KeyLength { get { return ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_Keylength); } }
			public string API { get { return ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_API); } }
			public string Webroot { get { return ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_Webroot); } }
			public string RealKeyPath { get { return ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_RealKeyPath); } }
			public string RealFullChainPath { get { return ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_RealFullChainPath); } }

			public bool Installed
			{
				get {
					if (ConfigFile.HasConfValue(ConfigFile.enuConfKey.Le_RealKeyPath))
					{
						if (File.Exists(ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_RealKeyPath)))
							return true;
					}
					return false;
				}
			}

			public DateTime? Created;
			public DateTime? Renew;

			public ConfigFile ConfigFile;

			public Certificate(string configPath)
			{
				ConfigFile= new ConfigFile(configPath);

				// Set Created
				if (DateTime.TryParse(ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_CertCreateTimeStr), out _))
				{
					Created = DateTime.Parse(ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_CertCreateTimeStr));
				}

				// Set Renew
				if (DateTime.TryParse(ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_NextRenewTimeStr), out _))
				{
					Renew = DateTime.Parse(ConfigFile.GetConfValue(ConfigFile.enuConfKey.Le_NextRenewTimeStr));
				}
			}
		}

		public class CertAuthority
		{
			public string DisplayName { get; set; }
			public string CmdValue { get; set; }
			public bool Disabled { get; set; }

			public CertAuthority(string displayName, string cmdValue, bool disabled)
			{
				DisplayName = displayName;
				CmdValue = cmdValue;
				Disabled = disabled;
			}
		}

		public class ConfigFile
		{
			private string _path = string.Empty;
			private string _config = string.Empty;
			private Dictionary<string, string> _dicValues = new Dictionary<string, string>();

			private const string _certBase64Prefix = "__ACME_BASE64__START_";
			private const string _certBase64Suffix = "__ACME_BASE64__END_";

			#region ConfKeyEnum

			public enum enuConfKey
			{
				// Account conf
				LOG_FILE,
				AUTO_UPGRADE,
				SAVED_CF_Token,
				SAVED_CF_Account_ID,
				SAVED_SMTP_BIN,
				SAVED_SMTP_FROM,
				SAVED_SMTP_TO,
				SAVED_SMTP_HOST,
				SAVED_SMTP_SECURE,
				NOTIFY_HOOK,
				DEFAULT_ACME_SERVER,
				// Certificate conf
				Le_Domain,
				Le_Alt,
				Le_Webroot,
				Le_PreHook,
				Le_PostHook,
				Le_RenewHook,
				Le_API,
				Le_Keylength,
				Le_OrderFinalize,
				Le_LinkOrder,
				Le_LinkCert,
				Le_CertCreateTime,
				Le_CertCreateTimeStr,
				Le_NextRenewTimeStr,
				Le_NextRenewTime,
				Le_RealCertPath,
				Le_RealCACertPath,
				Le_RealKeyPath,
				Le_ReloadCmd,
				Le_RealFullChainPath,
				Le_PFXPassword
			}

			#endregion

			public string Config
			{
				get { return _config; }
			}

			public ConfigFile()
			{
				// Placeholder for deleted certificates that have no config file
			}

			public ConfigFile(string path)
			{
				_path = path;
				Refresh();
			}

			public void Refresh()
			{
				_config = string.Empty;
				_dicValues.Clear();

				// Check if file exists first
				if (File.Exists(_path))
				{
					// Read in entire file
					_config = File.ReadAllText(_path);

					// Cleanup file a bit first
					while (_config.Contains(Environment.NewLine + Environment.NewLine))
					{
						_config = _config.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
					}
					_config = _config.Trim();

					// Attempt to parse config file, split into lines and parse key/value pairs
					string[] lines = _config.Split(Environment.NewLine);
					string[] split;
					string key;

					foreach (string line in lines)
					{
						if (!String.IsNullOrWhiteSpace(line))
						{
							split = line.Split("=", 2);
							key = split[0].Trim();

							if (!_dicValues.ContainsKey(key))
								_dicValues.Add(key, split[1].Trim().Trim('\''));
						}
					}
				}
			}

			public string GetConfValue(enuConfKey key)
			{
				if (_dicValues.ContainsKey(key.ToString()))
				{
					// Check if this is a Base64-encoded value
					if (key == enuConfKey.Le_ReloadCmd)
					{
						return Encoding.UTF8.GetString(Convert.FromBase64String(_dicValues[key.ToString()].Replace(_certBase64Prefix, "").Replace(_certBase64Suffix, "")));
					}
					else
					{
						return _dicValues[key.ToString()];
					}
				}

				return string.Empty;
			}

			public bool HasConfValue(enuConfKey key)
			{
				return _dicValues.ContainsKey(key.ToString());
			}

			public bool SetConfValue(enuConfKey key, string value)
			{
				try
				{
					// Update local dictionary
					if (!String.IsNullOrWhiteSpace(value))
					{
						// Check if this is a Base64-encoded value
						if (key == enuConfKey.Le_ReloadCmd)
						{
							value = _certBase64Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + _certBase64Suffix;
						}

						if (_dicValues.ContainsKey(key.ToString()))
						{
							// Update the value
							_dicValues[key.ToString()] = value;
						}
						else
						{
							// Add the value
							_dicValues.Add(key.ToString(), value);
						}
					}
					else
					{
						// Remove the value
						_dicValues.Remove(key.ToString());
					}

					// Update config file
					Regex configKey = new Regex($"({key.ToString()}='[^']*')");

					if (!String.IsNullOrWhiteSpace(value))
					{
						// Update config file
						if (configKey.Match(_config).Success)
						{
							// Key exists in config, update it
							_config = configKey.Replace(_config, $"{key.ToString()}='{value}'");
						}
						else
						{
							// Add new key to the config
							_config += Environment.NewLine + $"{key.ToString()}='{value}'";
						}
					}
					else
					{
						// Remove value from config file entirely
						_config = configKey.Replace(_config, string.Empty);
					}

					// Output new config to file
					File.WriteAllText(_path, _config);

					return true;
				}
				catch
				{
					// Placeholder
				}

				return false;
			}
		}

		#endregion

		#region Variables

		private CLI _CLI;

		private string _version = "";

		private ConfigFile _accountConf;
		private List<Certificate> _certificates = new List<Certificate>();
		private List<CertAuthority> _certAuthorities = new List<CertAuthority>();

		#endregion

		#region Properties

		public bool Installed
		{
			get { return !(String.IsNullOrWhiteSpace(_version)) && File.Exists($"{ACMEPath}/acme.sh"); }
		}

		public string Version
		{
			get { return _version; }
		}

		public ConfigFile AccountConf
		{
			get { return _accountConf; }
		}

		public List<Certificate> Certificates
		{
			get { return _certificates; }
		}

		public List<CertAuthority> CertAuthorities
		{
			get { return _certAuthorities; }
		}

		public string ACMEPath
		{
			get { return $"{_CLI.HomePath}/.acme.sh"; }
		}

		public string AccountConfPath
		{
			get { return $"{ACMEPath}/account.conf"; }
		}

		#endregion

		#region Constructors

		public ACME(CLI CLI)
		{
			_CLI = CLI;
			_accountConf = new ConfigFile(AccountConfPath);
			BuildAvailableCAs();
			Refresh();
		}

		#endregion

		private void BuildAvailableCAs()
		{
			_certAuthorities.Clear();
			_certAuthorities.Add(new CertAuthority("Default CA", "", false));
			_certAuthorities.Add(new CertAuthority("ZeroSSL", "zerossl", false));
			_certAuthorities.Add(new CertAuthority("LetsEncrypt", "letsencrypt", false));
			_certAuthorities.Add(new CertAuthority("LetsEncrypt Test", "letsencrypt_test", false));
			_certAuthorities.Add(new CertAuthority("BuyPass", "buypass", false));
			_certAuthorities.Add(new CertAuthority("BuyPass Test", "buypass_test", false));
			_certAuthorities.Add(new CertAuthority("SSLCom", "sslcom", false));
			_certAuthorities.Add(new CertAuthority("Google", "google", false));
			_certAuthorities.Add(new CertAuthority("Google Test", "googletest", false));
		}

		public void Refresh()
		{
			GetVersion();
			_accountConf.Refresh();
			RefreshCertificates();
		}

		public void GetVersion()
		{
			_version = "";
			
			if (File.Exists($"{ACMEPath}/acme.sh"))
			{
				try
				{
					_CLI.RunCommand($"{ACMEPath}/acme.sh --version", sudo: false);
					_version = _CLI.StandardOut.Split(Environment.NewLine)[1];
				}
				catch
				{
					_version = "Unknown";
				}
			}
		}

		
		public bool SetDefaultCA(string CA)
		{
			if (Installed)
			{
				try
				{
					if (String.IsNullOrWhiteSpace(CA))
						AccountConf.SetConfValue(ConfigFile.enuConfKey.DEFAULT_ACME_SERVER, string.Empty);
					else
						_CLI.RunCommand($"{ACMEPath}/acme.sh --set-default-ca --server {CA}", sudo: false);


					if (String.IsNullOrWhiteSpace(CA) || _CLI.StandardOut.Contains("Changed default CA"))
					{
						_accountConf.Refresh();
						return true;
					}
				}
				catch
				{
					// Placeholder
				}
			}

			return false;
		}

		public bool IssueCertificate(List<string> domains, string CA, ref string result)
		{
			try
			{
				// Build issue certificate command
				string cmd = $"{ACMEPath}/acme.sh";

				// Check if we are using a Cloudflare API token
				if (_accountConf.GetConfValue(ConfigFile.enuConfKey.SAVED_CF_Token) != string.Empty)
				{
					cmd += " --dns dns_cf";
				}

				// Build list of domains portion of the command
				cmd += " --issue";
				foreach (string domain in domains)
				{
					cmd += $" -d {domain}";
				}

				if (!String.IsNullOrWhiteSpace(CA))
				{
					cmd += $" --server {CA}";
				}

				// Execute command to issue certificate
				_CLI.RunCommand(cmd, sudo: false);

				if (_CLI.StandardOut.Contains("Cert success."))
				{
					RefreshCertificates();
					return true;
				}
				else
					result = _CLI.StandardError;
			}
			catch
			{
				// Placeholder
			}

			return false;
		}

		public bool InstallCertificate(string domainsCmd, string rootPath, string keyPath, string fullChainPath, string reloadCmd, ref string result)
		{
			try
			{
				// Build location to save certificate files to (private/public keys)
				string command = $"--installcert {domainsCmd}";

				command += $" --key-file {rootPath}/{keyPath}";
				command += $" --fullchain-file {rootPath}/{fullChainPath}";

				// Build reload command
				if (!String.IsNullOrWhiteSpace(reloadCmd))
					command += $" --reloadcmd \"{reloadCmd}\"";
				
				// Execute command to install certificate
				_CLI.RunCommand($"{ACMEPath}/acme.sh {command}", sudo: false);

				if (_CLI.StandardOut.Contains("Installing key") && _CLI.StandardOut.Contains("Installing full chain"))
					return true;
				else
					result = _CLI.StandardError;
			}
			catch
			{
				// Placeholder
			}

			return false;
		}

		public bool DeleteCertificate(Certificate cert)
		{
			try
			{
				// Execute command to delete certificate
				_CLI.RunCommand($"{ACMEPath}/acme.sh --remove --domain {cert.MainDomain}", sudo: false);

				if (_CLI.StandardOut.Contains($"{cert.MainDomain} is removed"))
					return true;
			}
			catch
			{
				// Placeholder
			}

			return false;
		}

		public bool ForceRenewCertificate(Certificate cert, ref string result)
		{
			try
			{
				// Execute command to delete certificate
				_CLI.RunCommand($"{ACMEPath}/acme.sh --renew --force --domain {cert.MainDomain}", sudo: false);

				// Check if the renewal was successful
				if (_CLI.StandardOut.Contains("Cert success.") || _CLI.StandardOut.Contains("Skip, Next renewal time"))
				{
					result = "Renewal success.";
					return true;
				}
				else if (_CLI.StandardError.Contains("rateLimited"))
					result = "Too many certificates issued for this exact set of domains.";
				else
					result = _CLI.StandardError;
			}
			catch
			{
				// Placeholder
			}

			return false;
		}

		public void RefreshCertificates()
		{
			_certificates.Clear();

			if (Directory.Exists(ACMEPath))
			{
				// Iterate through all ACME subdirectories looking for certificates
				FileInfo[] confs;
				FileInfo? conf;
				foreach (string dir in Directory.GetDirectories(ACMEPath)){
					// Retrieve config files and filter out the CSR conf
					confs = new DirectoryInfo(dir).GetFiles("*.conf");
					conf = confs.Where((x) => !x.Name.EndsWith("csr.conf")).FirstOrDefault();

					if (!(conf is null))
					{
						// Certificate config found
						_certificates.Add(new Certificate(conf.FullName));
					}
				}
			}
		}
	}
}