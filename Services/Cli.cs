using System.Diagnostics;

namespace NginxPanel.Services
{
    public class Cli
    {
        private readonly string _runningAsUser = Environment.UserName;
        private readonly string _homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

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

        public void RunCommand(string command, string working_dir = "", bool sudo = true, bool parseArgs = true)
        {
            _standardOut = string.Empty;
            _standardError = string.Empty;

            // Split command by pipe
            List<string> commands = command.Split('|').Select(s=> s.Trim()).ToList();
            string standardOut = string.Empty;
            string standardError = string.Empty;

            // Iterate through all commands and continue input/output as necessary
            foreach (string cmd in commands)
            {
                string arguments = string.Empty;

                if (parseArgs)
                {
                    if (sudo && !AppConfig.IsRunningInContainer)
                    {
                        arguments = cmd;
                        command = "sudo";
                    }
                    else if (cmd.Trim().Contains(" "))
                    {
                        arguments = cmd.Substring(cmd.IndexOf(" ") + 1);
                        command = cmd.Substring(0, cmd.IndexOf(" "));
                    }
                }
                else if (sudo && !AppConfig.IsRunningInContainer)
                {
                    arguments = cmd;
                    command = "sudo";
                }

                using (Process p = new Process())
                {
                    p.StartInfo = new ProcessStartInfo()
                    {
                        FileName = command.Trim(),
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Arguments = arguments.Trim()
                    };

                    if (!String.IsNullOrWhiteSpace(working_dir))
                        p.StartInfo.WorkingDirectory = working_dir;
                    else
                        p.StartInfo.WorkingDirectory = _homePath;

                    try
                    {                        
                        p.Start();
                        p.StandardInput.Write(standardOut);
                        p.StandardInput.Write(standardError);
                        p.StandardInput.Close();

                        standardOut = p.StandardOutput.ReadToEnd().Trim();
                        standardError = p.StandardError.ReadToEnd().Trim();
                        p.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        standardError = ex.ToString();
                    }

                    // Append command results
                    _standardOut += standardOut;
                    _standardError += standardError;
                }
            }
        }
    }
}