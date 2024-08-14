using System.Diagnostics;

namespace NginxPanel.Services
{
	public class CLI
	{
		private string _runningAsUser = string.Empty;
		private string _homePath = string.Empty;

		private string _standardOut = string.Empty;
		private string _standardError = string.Empty;

		public string RunningAsUser => _runningAsUser;

		public string HomePath => _homePath;

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
			_runningAsUser = Environment.UserName;

			// Determine user's home path
			_homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}

		public void RunCommand(string command, string working_dir = "", bool sudo = true, bool parseArgs = true)
		{
			_standardOut = string.Empty;
			_standardError = string.Empty;

			string arguments = string.Empty;

			if (parseArgs)
			{
				if (sudo)
				{
					arguments = command;
					command = "sudo";
				}
				else if (command.Contains(" "))
				{
					arguments = command.Substring(command.IndexOf(" ") + 1);
					command = command.Substring(0, command.IndexOf(" "));
				}
			}
			else if (sudo)
			{
				arguments = command;
				command = "sudo";
			}

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