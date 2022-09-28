using System.ComponentModel;
using System.Diagnostics;

namespace Cronos.Scheduling;
public class CommandRunner
{
    [DisplayName("{0}")]
    public void RunCommand(string _, string commandParser, string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = commandParser,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = arguments
        };

        Process.Start(processInfo);
    }
}
