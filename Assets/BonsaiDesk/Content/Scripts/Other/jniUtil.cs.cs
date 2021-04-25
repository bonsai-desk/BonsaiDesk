/*
  Copyright 2017 Captive Reality Ltd
  
  Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is 
  hereby granted, provided that the above copyright notice and this permission notice appear in all copies.
  THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE 
  INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE
  FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM 
  LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, 
  ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
  
  Source: 
  Description: 
  A very simple static helper (for Unity) that you can use to call a static Java method in a Java class using Jni
  
  Examples: 
  int result = CaptiveReality.Jni.Util.StaticCall("getAMethodWhichReturnsInt", 1, "com.yourandroidlib.example.ClassName");
  string result = CaptiveReality.Jni.Util.StaticCall("getAMethodWhichReturnsString", "UNKNOWN", "com.yourandroidlib.example.ClassName");
    
*/

using System;
using UnityEngine;

namespace CaptiveReality.Jni
{
    internal class Util
    {
        /// <summary>
        ///     StaticCall - Call a static Java method in a class using Jni
        /// </summary>
        /// <typeparam name="T">The return type of the method in the class you are calling</typeparam>
        /// <param name="methodName">The name of the method you want to call</param>
        /// <param name="defaultValue">The value you want to return if there was a problem</param>
        /// <param name="androidJavaClass">
        ///     The name of the Package and Class eg, packagename.myClassName or
        ///     com.yourandroidlib.example.ClassName
        /// </param>
        /// <returns>Generic</returns>
        public static string StaticCall(string methodName, string defaultValue, string androidJavaClass)
        {
            string result;

            // Only works on Android!
            if (Application.platform != RuntimePlatform.Android)
            {
                return defaultValue;
            }

            try
            {
                using (var androidClass = new AndroidJavaClass(androidJavaClass))
                {
                    if (null != androidClass)
                    {
                        result = androidClass.CallStatic<string>(methodName);
                    }
                    else
                    {
                        result = defaultValue;
                    }
                }
            }
            catch (Exception ex)
            {
                // If there is an exception, do nothing but return the default value
                // Uncomment this to see exceptions in Unity Debug Log....
                Debug.Log(string.Format("{0}.{1} Exception:{2}", androidJavaClass, methodName, ex.ToString() ));
                return defaultValue;
            }

            return result;
        }
    }
}