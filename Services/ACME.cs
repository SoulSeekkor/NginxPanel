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