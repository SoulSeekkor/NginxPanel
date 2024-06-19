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

		#region Variables

		private CLI _CLI;

		private string _version = "";

		private List<Certificate> _certificates = new List<Certificate>();

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

		#endregion

		#region Constructors

		public ACME(CLI CLI)
		{
			_CLI = CLI;
			Refresh();
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

		public void IssueCertificate(List<string> domains)
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

				// Execute command to install certificate
				_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh --test --issue {domainsCmd}", sudo: false);
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