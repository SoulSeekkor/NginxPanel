using System.Diagnostics;

namespace NginxPanel.Shared
{
    public class CLI
    {
        public static void RunCommand(string command, string arguments, out string output, out string error)
        {
            output = string.Empty;
            error = string.Empty;

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
                    output = p.StandardOutput.ReadToEnd().Trim();
                    error = p.StandardError.ReadToEnd().Trim();
                    p.WaitForExit();
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                }

                
            };
        }
    }
}