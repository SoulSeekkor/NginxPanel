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
			public DateTime Created;
			public DateTime Renew;
		}

		#endregion

		#region Enums

		public enum enuAccountConfKey
		{
			LOG_FILE,
			AUTO_UPGRADE,
			SAVED_CF_Token,
			SAVED_CF_Account_ID,
			SAVED_SMTP_BIN,
			SAVED_SMTP_FROM,
			SAVED_SMTP_TO,
			SAVED_SMTP_HOST,
			SAVED_SMTP_SECURE,
			NOTIFY_HOOK
		}

		#endregion

		#region Variables

		private CLI _CLI;

		private string _version = "";

		private List<Certificate> _certificates = new List<Certificate>();

		private string _accountConfPath = string.Empty;
		private string _accountConf = string.Empty;
		private Dictionary<string, string> _accountConfDic = new Dictionary<string, string>();

		#endregion

		#region Properties

		public bool Installed
		{
			get { return !(_version == ""); }
		}

		public string Version
		{
			get { return _version; }
		}

		public List<Certificate> Certificates
		{
			get { return _certificates; }
		}

		public string AccountConf
		{
			get { return _accountConf; }
		}

		#endregion

		#region Constructors

		public ACME(CLI CLI)
		{
			_CLI = CLI;
			Refresh();
			ParseAccountConf();
		}

		#endregion

		public void Refresh()
		{
			GetVersion();
			RefreshCertificates();
		}

		public void GetVersion()
		{
			_version = "";
			
			if (File.Exists($"{_CLI.HomePath}/.acme.sh/acme.sh"))
			{
				try
				{
					_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh --version", sudo: false);
					_version = _CLI.StandardOut.Split(Environment.NewLine)[1];
				}
				catch
				{
					_version = "Unknown";
				}
			}
		}

		public void ParseAccountConf()
		{
			_accountConfDic.Clear();

			if (Installed)
			{
				_accountConfPath = $"{_CLI.HomePath}/.acme.sh/account.conf";

				// Attempt to parse config files
				if (File.Exists(_accountConfPath))
				{
					_accountConf = File.ReadAllText(_accountConfPath);

					// Cleanup file a bit first
					while (_accountConf.Contains("\n\n"))
					{
						_accountConf = _accountConf.Replace("\n\n", "\n");
					}
					_accountConf = _accountConf.Trim();

					// Split into lines and parse key/value pairs
					string[] lines = _accountConf.Split("\n");
					string[] split;
					string key;

					foreach (string line in lines)
					{
						if (!String.IsNullOrWhiteSpace(line))
						{
							split = line.Split("=", 2);
							key = split[0].Trim();

							if (!_accountConfDic.ContainsKey(key))
								_accountConfDic.Add(key, split[1].Trim().Trim('\''));
						}
					}
				}
			}
		}

		public string GetAccountConfValue(enuAccountConfKey key)
		{
			if (_accountConfDic.ContainsKey(key.ToString()))
				return _accountConfDic[key.ToString()];

			return string.Empty;
		}

		public bool SetAccountConfValue(enuAccountConfKey key, string value)
		{
			bool result = false;

			try
			{
				// Update local tracking
				if (!String.IsNullOrWhiteSpace(value))
				{
					// Add/Update the value
					if (_accountConfDic.ContainsKey(key.ToString()))
						_accountConfDic[key.ToString()] = value;
					else
						_accountConfDic.Add(key.ToString(), value);
				}
				else
				{
					// Remove the value
					_accountConfDic.Remove(key.ToString());
				}

				// Update config file
				Regex configKey = new Regex($"({key.ToString()}='[^']*')");

				if (!String.IsNullOrWhiteSpace(value))
				{
					// Update config file
					if (configKey.Match(_accountConf).Success)
					{
						// Key exists in config, update it
						_accountConf = configKey.Replace(_accountConf, $"{key.ToString()}='{value}'");
					}
					else
					{
						// Add new key to the config
						_accountConf += $"\n{key.ToString()}='{value}'\n";
					}
				}
				else
				{
					// Remove value from config file entirely
					_accountConf = configKey.Replace(_accountConf, string.Empty);
				}

				// Output new config to file
				File.WriteAllText(_accountConfPath, _accountConf);

				result = true;
			}
			catch
			{
				// Placeholder
			}

			return result;
		}

		public void IssueCertificate(List<string> domains, string CFToken)
		{
			try
			{
				// Build list of domains portion of the command
				string domainsCmd = "";
				foreach (string domain in domains)
				{
					domainsCmd += $" -d {domain}";
				}
				domainsCmd = domainsCmd.Trim();

				// Set environment variable(s)
				Environment.SetEnvironmentVariable("CF_TOKEN", CFToken);
				//_CLI.RunCommand("printenv CF_TOKEN", sudo: false);  // testing only

				// Execute command to install certificate
				//_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh --test --issue --dns dns_cf {domainsCmd}", sudo: false);
				_CLI.RunCommand($"CF_TOKEN={CFToken} bash -c '{_CLI.HomePath}/.acme.sh/acme.sh --test --issue --dns dns_cf {domainsCmd}'", sudo: false, parseArgs: false);
			}
			catch
			{
				// Placeholder
			}
		}

		public void InstallCertificate(List<string> domains, string PFXpassword)
		{
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
				if (!String.IsNullOrWhiteSpace(PFXpassword))
					command += $" && /root/.acme.sh/acme.sh --to-pkcs12 {domainsCmd} --password {PFXpassword}";
				
				command += "\"";

				// Execute command to install certificate
				_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh --test {command}", sudo: false);
			}
			catch
			{
				// Placeholder
			}
		}

		public void RefreshCertificates()
		{
			_certificates.Clear();

			if (Installed)
			{
				// Refresh list of certificates
				_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh --list", sudo: false);

				string listing = _CLI.StandardOut;
				
				if (!listing.StartsWith("Main_Domain  KeyLength  SAN_Domains  CA  Created  Renew"))
				{
					// Unable to parse, headers are not as expected!
					return;
				}

				if (listing.Contains(Environment.NewLine))
				{
					// First line are headers, remove it
					listing = listing.Substring(listing.IndexOf(Environment.NewLine)).Trim();

					foreach (string line in listing.Split(Environment.NewLine))
					{

					}
				}
			}
		}
	}
}