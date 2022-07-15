using NMemory;
using NMemory.Tables;

namespace WinTenDev.Zizi.Services.NMemory;

public class HitActivityInMemory : Database
{
    public HitActivityInMemory()
    {
        FloodActivities = Tables.Create<HitActivity, string>(entity => entity.Guid);
    }

    public ITable<HitActivity> FloodActivities { get; set; }
}