using System.Threading;
using System.Threading.Tasks;
using RepoDb;
using RepoDb.Interfaces;
using Serilog;

namespace WinTenDev.Zizi.Models.RepoDb;

public class DefaultTraceLog : ITrace
{
    public void BeforeExecution(CancellableTraceLog log)
    {
        Log.Debug("RepoDB: {Id} - {Statement}",
            log.SessionId,
            log.Statement
        );
    }
    public void AfterExecution<TResult>(ResultTraceLog<TResult> log)
    {
        Log.Debug("RepoDB: {Id} - {Key}: {ExecutionTime}",
            log.SessionId,
            log.Key,
            log.ExecutionTime
        );
    }
    public async Task BeforeExecutionAsync(
        CancellableTraceLog log,
        CancellationToken cancellationToken = new CancellationToken())
    {
        Log.Debug("RepoDB: {Id} - {Statement}",
            log.SessionId,
            log.Statement
        );
    }
    public async Task AfterExecutionAsync<TResult>(
        ResultTraceLog<TResult> log,
        CancellationToken cancellationToken = new CancellationToken())
    {
        Log.Debug("RepoDB: {Id} - {Key}: {ExecutionTime} ",
            log.SessionId,
            log.Key,
            log.ExecutionTime
        );
    }
}