using System.Threading;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Models.Interfaces;

public interface IStartupTask
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
