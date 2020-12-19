using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobleConnect
{
    public class UnityLogger
    {
        public static void Init()
        {
            Logger.logger = Debug.Log;
            Logger.warnLogger = Debug.LogWarning;
            Logger.errorLogger = Debug.LogError;
            //Logger.logLevel = Logger.Level.Developer;
        }
    }
}
