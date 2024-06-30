using System.Text.RegularExpressions;

namespace NginxPanel.Services
{
	public class ACME
	{
		#region Classes

		public class Certificate
		{
			public string MainDomain = string.Empty;
			public string SANDomains = string.Empty;
			public string KeyLength = string.Empty;
			public string CA = string.Empty;
			public DateTime? Created;
			public DateTime? Renew;

			public Certificate(string mainDomain, string sanDomains, string keyLength, string ca, DateTime? created, DateTime? renew)
			{
				MainDomain = mainDomain;
				SANDomains = (sanDomains == "no" ? "" : sanDomains.Replace(",", ", "));
				KeyLength = keyLength.Trim('\"');
				CA = ca;
				Created = created;
				Renew = renew;
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

			public string Config
			{
				get { return _config; }
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
					return _dicValues[key.ToString()];

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
					// Update local tracking
					if (!String.IsNullOrWhiteSpace(value))
					{
						// Add/Update the value
						if (_dicValues.ContainsKey(key.ToString()))
							_dicValues[key.ToString()] = value;
						else
							_dicValues.Add(key.ToString(), value);
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

		#region Enums

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

		#region Variables

		private CLI _CLI;

		private string _version = "";

		private ConfigFile _accountConf;
		private List<Certificate> _certificates = new List<Certificate>();
		private List<CertAuthority> _certAuthorities = new List<CertAuthority>();

		private string _certBase64Prefix = "__ACME_BASE64__START_";
		private string _certBase64Suffix = "__ACME_BASE64__END_";

		#endregion

		#region Properties

		public bool Installed
		{
			get { return !(String.IsNullOrWhiteSpace(_version) && File.Exists($"{ACMEPath}/acme.sh")); }
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
					_CLI.RunCommand($"{ACMEPath}/acme.sh --set-default-ca --server {CA}", sudo: false);

					if (_CLI.StandardOut.Contains("Changed default CA"))
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

		public bool IssueCertificate(List<string> domains, string CA)
		{
			try
			{
				// Build issue certificate command
				string cmd = $"{ACMEPath}/acme.sh";

				// Check if we are using a Cloudflare API token
				if (_accountConf.GetConfValue(enuConfKey.SAVED_CF_Token) != string.Empty)
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
			}
			catch
			{
				// Placeholder
			}

			return false;
		}

		public bool InstallCertificate(List<string> domains, string KeyPath, string FullChainPath)
		{
			return false;

			try
			{
				// Build list of domains portion of the command
				string domainsCmd = "";
				foreach (string domain in domains)
				{
					domainsCmd += $" -d {domain}";
				}
				domainsCmd = domainsCmd.Trim();

				// Build location to save certificate files to (private/public keys)
				string command = $"--installcert {domainsCmd}";

				command += $" --key-file /etc/acme.sh/{domains.First().ToLower()}/{domains.First().ToLower()}.key";
				command += $" --fullchain-file /etc/acme.sh/{domains.First().ToLower()}/{domains.First().ToLower()}.cert";

				// Build reload command, include PFX export if included
				command += " --reloadcmd \"service nginx force-reload";
				//if (!String.IsNullOrWhiteSpace(PFXpassword))
				//	command += $" && /root/.acme.sh/acme.sh --to-pkcs12 {domainsCmd} --password {PFXpassword}";
				
				command += "\"";

				// Execute command to install certificate
				_CLI.RunCommand($"{ACMEPath}/acme.sh {command}", sudo: false);

				return true;
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

		public bool ForceRenewCertificate(Certificate cert, ref string response)
		{
			try
			{
				// Execute command to delete certificate
				_CLI.RunCommand($"{ACMEPath}/acme.sh --renew --force --domain {cert.MainDomain}", sudo: false);

				// Check if the renewal was successful
				if (_CLI.StandardOut.Contains("Cert success.") || _CLI.StandardOut.Contains("Skip, Next renewal time"))
				{
					response = "Renewal success.";
					return true;
				}
				else if (_CLI.StandardError.Contains("rateLimited"))
				{
					response = "Too many certificates issued for this exact set of domains.";
				}
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

			if (Installed)
			{
				// Refresh list of certificates
				_CLI.RunCommand($"{ACMEPath}/acme.sh --list", sudo: false);

				string listing = _CLI.StandardOut;

				if (listing.IndexOf(Environment.NewLine) == -1)
				{
					// No certificates currently
					return;
				}

				string header = listing.Substring(0, listing.IndexOf(Environment.NewLine));

				// Cleanup the header
				while (header.Contains("  "))
					header = header.Replace("  ", " ");

				if (header != "Main_Domain KeyLength SAN_Domains CA Created Renew")
				{
					// Unable to parse, headers are not as expected!
					return;
				}

				if (listing.Contains(Environment.NewLine))
				{
					// First line are headers, remove it
					listing = listing.Substring(listing.IndexOf(Environment.NewLine)).Trim();

					List<string> split;
					DateTime? created = null;
					DateTime? renew = null;
					foreach (string line in listing.Split(Environment.NewLine))
					{
						split = line.Split(" ").ToList();
						split.RemoveAll((x) => String.IsNullOrWhiteSpace(x));

						if (split.Count == 4 || String.IsNullOrWhiteSpace(split[4]))
							created = null;
						else
							created = DateTime.Parse(split[4]);

						if (split.Count == 4 || String.IsNullOrWhiteSpace(split[5]))
							renew = null;
						else
							renew = DateTime.Parse(split[5]);

						_certificates.Add(new Certificate(split[0], split[2], split[1], split[3], created, renew));
					}
				}
			}
		}
	}
}