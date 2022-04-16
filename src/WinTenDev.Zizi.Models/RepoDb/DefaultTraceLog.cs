using RepoDb;
using RepoDb.Interfaces;
using Serilog;

namespace WinTenDev.Zizi.Models.RepoDb;

public class DefaultTraceLog : ITrace
{
    public void BeforeAverage(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterAverage(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeAverageAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterAverageAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeBatchQuery(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterBatchQuery(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeCount(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterCount(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeCountAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterCountAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeDelete(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterDelete(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeDeleteAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterDeleteAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeExists(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterExists(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeExecuteNonQuery(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterExecuteNonQuery(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeExecuteQuery(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterExecuteQuery(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeExecuteReader(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterExecuteReader(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeExecuteScalar(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterExecuteScalar(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeInsert(CancellableTraceLog log)
    {
        Log.Debug("RepoDB Trace: {@Log}", log.Statement);
    }

    public void AfterInsert(TraceLog log)
    {
        Log.Debug("RepoDB Trace - Insert Execution: {ExecutionTime} ", log.ExecutionTime);
    }

    public void BeforeInsertAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterInsertAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeMax(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterMax(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeMaxAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterMaxAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeMerge(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterMerge(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeMergeAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterMergeAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeMin(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterMin(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeMinAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterMinAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeQuery(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterQuery(TraceLog log)
    {
        Log.Debug("RepoDB Trace - AfterQuery. {@Log} ", log);
    }

    public void BeforeQueryAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterQueryAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeQueryMultiple(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterQueryMultiple(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeSum(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterSum(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeSumAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterSumAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeTruncate(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterTruncate(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeUpdate(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterUpdate(TraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void BeforeUpdateAll(CancellableTraceLog log)
    {
        throw new System.NotImplementedException();
    }

    public void AfterUpdateAll(TraceLog log)
    {
        throw new System.NotImplementedException();
    }
}