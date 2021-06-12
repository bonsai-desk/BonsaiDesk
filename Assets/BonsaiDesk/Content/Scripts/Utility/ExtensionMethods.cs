using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    public static T GetComponentCheck<T>(this Component component)
    {
#if DEVELOPMENT_BUILD
        var c = component.GetComponent<T>();

        if (c == null)
        {
            BonsaiLog.LogError("GetComponentCheck, c is null");
        }
        else if (c.ToString() == "null")
        {
            BonsaiLog.LogError("GetComponentCheck, c.ToString() is \"null\"");
        }

        return c;
#else
        return component.GetComponent<T>();
#endif
    }

    public static T GetComponentCheck<T>(this GameObject gameObject)
    {
#if DEVELOPMENT_BUILD
        var c = gameObject.GetComponent<T>();

        if (c == null)
        {
            BonsaiLog.LogError("GetComponentCheck, c is null");
        }
        else if (c.ToString() == "null")
        {
            BonsaiLog.LogError("GetComponentCheck, c.ToString() is \"null\"");
        }

        return c;
#else
        return gameObject.GetComponent<T>();
#endif
    }
}