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
				SANDomains = (sanDomains == "no" ? "" : sanDomains);
				KeyLength = keyLength.Trim('\"');
				CA = ca;
				Created = created;
				Renew = renew;
			}
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
					while (_accountConf.Contains(Environment.NewLine + Environment.NewLine))
					{
						_accountConf = _accountConf.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
					}
					_accountConf = _accountConf.Trim();

					// Split into lines and parse key/value pairs
					string[] lines = _accountConf.Split(Environment.NewLine);
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

		public bool HasAccountConfValue(enuAccountConfKey key)
		{
			return _accountConfDic.ContainsKey(key.ToString());
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
						_accountConf += Environment.NewLine + $"{key.ToString()}='{value}'";
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

		public bool IssueCertificate(List<string> domains)
		{
			bool result = false;

			try
			{
				// Build issue certificate command
				string cmd = $"{_CLI.HomePath}/.acme.sh/acme.sh";

#if DEBUG
				cmd += " --test";
#endif

				// Check if we are using a Cloudflare API token
				if (GetAccountConfValue(enuAccountConfKey.SAVED_CF_Token) != string.Empty)
				{
					cmd += " --dns dns_cf";
				}

				// Build list of domains portion of the command
				cmd += " --issue";
				foreach (string domain in domains)
				{
					cmd += $" -d {domain}";
				}

				// Execute command to issue certificate
				_CLI.RunCommand(cmd, sudo: false);

				if (_CLI.StandardOut.Contains("Cert success."))
				{
					result = true;
					RefreshCertificates();
				}
			}
			catch
			{
				// Placeholder
			}

			return result;
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

		public bool DeleteCertificate(Certificate cert)
		{
			bool result = false;

			try
			{
				// Execute command to delete certificate
				_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh --remove --domain {cert.MainDomain}", sudo: false);

				if (_CLI.StandardOut.Contains($"{cert.MainDomain} is removed"))
					result = true;
			}
			catch
			{
				// Placeholder
			}

			return result;
		}

		public bool ForceRenewCertificate(Certificate cert, ref string response)
		{
			bool result = false;

			try
			{
				// Execute command to delete certificate
				_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh --test --renew --force --domain {cert.MainDomain}", sudo: false);

				// Check if the renewal was successful
				if (_CLI.StandardOut.Contains("Cert success.") || _CLI.StandardOut.Contains("Skip, Next renewal time"))
				{
					response = "Renewal success.";
					result = true;
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

			return result;
		}

		public void RefreshCertificates()
		{
			_certificates.Clear();

			if (Installed)
			{
				// Refresh list of certificates
				_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh --list", sudo: false);

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