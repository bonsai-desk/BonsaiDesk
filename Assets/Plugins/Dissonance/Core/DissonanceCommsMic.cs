using System.Collections.Generic;
using Dissonance.Audio.Capture;

namespace Dissonance
{
    public partial class DissonanceComms
    {
        /// <summary>
        /// Get a list of valid microphone devices that can be used.
        /// </summary>
        /// <param name="output"></param>
        public void GetMicrophoneDevices(List<string> output)
        {
            // Try to get the mic component from the capture pipeline if it has already been started
            // If that finds nothing, try to get the component directly
            var mic = _capture.Microphone;
            if (mic == null)
                mic = GetComponent<IMicrophoneCapture>();

            // Convert the mic into a device list. If that fails try to get a device list directly.
            var list = mic as IMicrophoneDeviceList;
            if (list == null)
                list = GetComponent<IMicrophoneDeviceList>();

            // If the list is null just fall back to using the Unity method
            if (list != null)
                list.GetDevices(output);
            else
                output.AddRange(UnityEngine.Microphone.devices);
        }
    }
}
