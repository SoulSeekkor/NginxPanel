using System.Diagnostics;

namespace NginxPanel.Services
{
	public class CLI
	{
		private string _runningAsUser = string.Empty;
		private string _homePath = string.Empty;

		private string _standardOut = string.Empty;
		private string _standardError = string.Empty;

		public string RunningAsUser
		{
			get { return _runningAsUser; }
		}

		public string HomePath
		{
			get { return _homePath; }
		}

		public string StandardOut
		{
			get { return (_standardOut ?? string.Empty).Trim(); }
		}

		public string StandardError
		{
			get { return (_standardError ?? string.Empty).Trim(); }
		}

		public CLI()
		{
			// Determine user this is running as
			RunCommand("whoami", "");
			_runningAsUser = StandardOut;

			// Determine user's home path
#if DEBUG
			if (_runningAsUser == "root")
				_homePath = "/" + Environment.UserName;
			else
				_homePath = "/home/" + Environment.UserName;
#else
			RunCommand("echo", "$HOME");
			_homePath = StandardOut;
#endif
		}

		public void RunCommand(string command, string arguments, string working_dir = "")
		{
			_standardOut = string.Empty;
			_standardError = string.Empty;

			using (Process p = new Process())
			{
				p.StartInfo = new ProcessStartInfo()
				{
					FileName = command,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					Arguments = arguments
				};

				if (!String.IsNullOrWhiteSpace(working_dir))
					p.StartInfo.WorkingDirectory = working_dir;
				else
					p.StartInfo.WorkingDirectory = _homePath;

				try
				{
					p.Start();
					_standardOut = p.StandardOutput.ReadToEnd().Trim();
					_standardError = p.StandardError.ReadToEnd().Trim();
					p.WaitForExit();
				}
				catch (Exception ex)
				{
					_standardError = ex.ToString();
				}
			};
		}
	}
}