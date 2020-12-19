using Dissonance.Editor;
using UnityEditor;

namespace Dissonance.Integrations.MirrorIgnorance.Editor
{
    [CustomEditor(typeof(MirrorIgnoranceCommsNetwork))]
    public class MirrorIgnoranceNetworkEditor
        : BaseDissonnanceCommsNetworkEditor<MirrorIgnoranceCommsNetwork, MirrorIgnoranceServer, MirrorIgnoranceClient, MirrorConn, Unit, Unit>
    {
    }
}