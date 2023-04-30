using System.Diagnostics;

namespace NginxPanel.Shared.State
{
    public class CLI
    {
        private Process? _cli = null;

        public DataReceivedEventHandler? OutReceived;
        public DataReceivedEventHandler? ErrorReceived;

        public CLI()
        {
            _cli = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "bash",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            _cli.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
            _cli.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

            _cli.Start();
            _cli.BeginOutputReadLine();
            _cli.BeginErrorReadLine();
        }

        public async Task RunCommandAsync(string command)
        {
            if (!(_cli is null))
                await _cli.StandardInput.WriteLineAsync(command);
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            OutReceived?.Invoke(sender, e);
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            ErrorReceived?.Invoke(sender, e);
        }
    }
}