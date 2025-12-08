using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    static Dictionary<Type, object> services = new Dictionary<Type, object>();

    public static void Register<T>(T service)
    {
        var t = typeof(T);
        if (services.ContainsKey(t)) services[t] = service;
        else services.Add(t, service);
    }

    public static T Get<T>()
    {
        var t = typeof(T);
        if (services.TryGetValue(t, out var s)) return (T)s;
        throw new Exception($"Service {t} not registered");
    }

    public static bool TryGet<T>(out T service)
    {
        var t = typeof(T);
        if (services.TryGetValue(t, out var s))
        {
            service = (T)s;
            return true;
        }
        service = default;
        return false;
    }
}
