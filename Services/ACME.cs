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
			
			if (FileExists($"{_CLI.HomePath}/.acme.sh/acme.sh"))
			{
				_CLI.RunCommand($"{_CLI.HomePath}/.acme.sh/acme.sh -v", sudo: false);
				_version = _CLI.StandardOut.Split("\n")[1];
			}
			else
			{
				_version = "Unknown";
			}
		}

		private bool FileExists(string file)
		{
			_CLI.RunCommand($"bash -c \"(sudo ls {file} >> /dev/null 2>&1 && echo 1) || echo 0\"");
			if (_CLI.StandardOut.Contains("1"))
				return true;
			else
				return false;
		}
	}
}