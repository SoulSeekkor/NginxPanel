using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;

namespace NginxPanel.Shared
{
    public class CLI
    {
        private static bool _initialized = false;

        private static string _standardOut = string.Empty;
        private static string _standardError = string.Empty;

        public static bool Initialized
        {
            get { return _initialized; }
            set { _initialized = value; }
        }

        public static string StandardOut
        {
            get { return (_standardOut ?? string.Empty).Trim(); }
        }
        public static string StandardError
        {
            get { return (_standardError ?? string.Empty).Trim(); }
        }

        public static void RunCommand(string command, string arguments)
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