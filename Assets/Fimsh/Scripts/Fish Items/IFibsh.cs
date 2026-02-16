using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFibsh
{
    static HashSet<IFibsh> aliveFibsh = new();
    [RinCore.Initialize(-99)]
    private static void Reinitialize()
    {
        if (aliveFibsh == null)
            aliveFibsh = new();
        else
            aliveFibsh.Clear();
    }
    public static int TotalFishItems
    {
        get
        {
            int count = 0;
            foreach (var item in aliveFibsh)
            {
                if (item == null)
                    continue;
                count++;
            }
            return count;
        }
    }
    protected static void BindFibsh(IFibsh f)
    {
        aliveFibsh.Add(f);
    }
    protected static void ReleaseFibsh(IFibsh f)
    {
        aliveFibsh.Remove(f);
    }
    public bool TryCollect(FishPlayer p);
}
