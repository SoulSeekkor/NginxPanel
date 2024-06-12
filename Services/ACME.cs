namespace NginxPanel.Services
{
	public class ACME
	{

		#region Variables

		private CLI _CLI;

		private string _version = "";

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
		}

		public void GetVersion()
		{
			_version = "";
			
			if (File.Exists($"{_CLI.HomePath}/.acme.sh/acme.sh"))
			{
				_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh -v", sudo: false);
				_version = _CLI.StandardOut.Split("\n")[1];
			}
			else
			{
				_version = "Unknown";
			}
		}
	}
}