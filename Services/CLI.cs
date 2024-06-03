using System.Diagnostics;

namespace NginxPanel.Services
{
    public class CLI
    {
        private string _standardOut = string.Empty;
        private string _standardError = string.Empty;

        public string StandardOut
        {
            get { return (_standardOut ?? string.Empty).Trim(); }
        }
        public string StandardError
        {
            get { return (_standardError ?? string.Empty).Trim(); }
        }

        public void RunCommand(string command, string arguments)
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