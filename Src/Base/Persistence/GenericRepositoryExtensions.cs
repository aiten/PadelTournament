namespace Base.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;

public static class GenericRepositoryExtensions
{
    public static void Sync<TEntity, TKey>(
        this ICollection<TEntity> inDb,
        ICollection<TEntity>      toDb,
        Func<TEntity, TKey>       keySelector,
        Action<TEntity, TEntity>? copyToDb   = null,
        Action<TEntity>?          setAdd     = null,
        Action<TEntity>?          setDelete  = null,
        bool                      skipAdd    = false,
        bool                      skipDelete = false)
    {
        foreach (var ed in inDb.Join(toDb, keySelector, keySelector, (eDb, tDb) => (eDb, tDb)))
        {
            copyToDb?.Invoke(ed.eDb, ed.tDb);
        }

        var toAdd = toDb.ExceptBy(inDb.Select(keySelector), keySelector).ToList();
        var toDel = inDb.ExceptBy(toDb.Select(keySelector), keySelector).ToList();

        if (!skipAdd)
        {
            foreach (var add in toAdd)
            {
                inDb.Add(add);
                setAdd?.Invoke(add);
            }
        }

        if (!skipDelete)
        {
            foreach (var del in toDel)
            {
                setDelete?.Invoke(del);
                inDb.Remove(del);
            }
        }
    }
}