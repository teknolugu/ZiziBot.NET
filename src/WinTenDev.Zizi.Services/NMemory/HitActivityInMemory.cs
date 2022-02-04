﻿using NMemory;
using NMemory.Tables;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Services.NMemory;

public class HitActivityInMemory : Database
{
    public HitActivityInMemory()
    {
        FloodActivities = Tables.Create<HitActivity, string>(entity => entity.Guid);
        MataActivities = Tables.Create<HitActivity, string>(entity => entity.Guid);
    }

    public ITable<HitActivity> FloodActivities { get; set; }
    public ITable<HitActivity> MataActivities { get; set; }
}