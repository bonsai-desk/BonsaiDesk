using UnityEngine;
using System.Collections;
using Mirror;
//using UnityEngine.Networking;
using System;

namespace Smooth
{
    /// <summary>
    /// The StateMirror of an object: timestamp, position, rotation, scale, velocity, angular velocity.
    /// </summary>
    public class StateMirror
    {
        /// <summary>
        /// The network timestamp of the owner when the StateMirror was sent.
        /// </summary>
        public float ownerTimestamp;
        /// <summary>
        /// The position of the owned object when the StateMirror was sent.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The rotation of the owned object when the StateMirror was sent.
        /// </summary>
        public Quaternion rotation;
        /// <summary>
        /// The scale of the owned object when the StateMirror was sent.
        /// </summary>
        public Vector3 scale;
        /// <summary>
        /// The velocity of the owned object when the StateMirror was sent.
        /// </summary>
        public Vector3 velocity;
        /// <summary>
        /// The angularVelocity of the owned object when the StateMirror was sent.
        /// </summary>
        public Vector3 angularVelocity;
        /// <summary>
        /// If this StateMirror is tagged as a teleport StateMirror, it should be moved immediately to instead of lerped to.
        /// </summary>
        public bool teleport;
        /// <summary>
        /// If this StateMirror is tagged as a positional rest StateMirror, it should stop extrapolating position on non-owners.
        /// </summary>
        public bool atPositionalRest;
        /// <summary>
        /// If this StateMirror is tagged as a rotational rest StateMirror, it should stop extrapolating rotation on non-owners.
        /// </summary>
        public bool atRotationalRest;

        /// <summary>
        /// The time on the server when the StateMirror is validated. Only used by server for latestVerifiedStateMirror.
        /// </summary>
        public float receivedOnServerTimestamp;

        /// <summary>The localTime that a state was received on a non-owner.</summary>
        public float receivedTimestamp;

        /// <summary>This value is incremented each time local time is reset so that non-owners can detect and handle the reset.</summary>
        public int localTimeResetIndicator;

        /// <summary>
        /// Used in Deserialize() so we don't have to make a new Vector3 every time.
        /// </summary>
        public Vector3 reusableRotationVector;

        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayPosition = false;
        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayRotation = false;
        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayScale = false;
        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayVelocity = false;
        /// <summary>
        /// The server will set this to true if it is received so we know to relay the information back out to other clients.
        /// </summary>
        public bool serverShouldRelayAngularVelocity = false;

        /// <summary>
        /// Default constructor. Does nothing.
        /// </summary>
        public StateMirror() { }

        /// <summary>
        /// Copy an existing StateMirror.
        /// </summary>
        public StateMirror copyFromState(StateMirror state)
        {
            ownerTimestamp = state.ownerTimestamp;
            position = state.position;
            rotation = state.rotation;
            scale = state.scale;
            velocity = state.velocity;
            angularVelocity = state.angularVelocity;
            receivedTimestamp = state.receivedTimestamp;
            localTimeResetIndicator = state.localTimeResetIndicator;
            return this;
        }

        /// <summary>
        /// Returns a Lerped StateMirror that is between two StateMirrors in time.
        /// </summary>
        /// <param name="start">Start StateMirror</param>
        /// <param name="end">End StateMirror</param>
        /// <param name="t">Time</param>
        /// <returns></returns>
        public static StateMirror Lerp(StateMirror targetTempStateMirror, StateMirror start, StateMirror end, float t)
        {
            targetTempStateMirror.position = Vector3.Lerp(start.position, end.position, t);
            targetTempStateMirror.rotation = Quaternion.Lerp(start.rotation, end.rotation, t);
            targetTempStateMirror.scale = Vector3.Lerp(start.scale, end.scale, t);
            targetTempStateMirror.velocity = Vector3.Lerp(start.velocity, end.velocity, t);
            targetTempStateMirror.angularVelocity = Vector3.Lerp(start.angularVelocity, end.angularVelocity, t);

            targetTempStateMirror.ownerTimestamp = Mathf.Lerp(start.ownerTimestamp, end.ownerTimestamp, t);

            return targetTempStateMirror;
        }

        public void resetTheVariables()
        {
            ownerTimestamp = 0;
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.zero;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            atPositionalRest = false;
            atRotationalRest = false;
            teleport = false;
            receivedTimestamp = 0;
            localTimeResetIndicator = 0;
        }

        public void copyFromSmoothSync(SmoothSyncMirror smoothSyncScript)
        {
            ownerTimestamp = smoothSyncScript.localTime;
            position = smoothSyncScript.getPosition();
            rotation = smoothSyncScript.getRotation();
            scale = smoothSyncScript.getScale();

            if (smoothSyncScript.hasRigidbody)
            {
                velocity = smoothSyncScript.rb.velocity;
                angularVelocity = smoothSyncScript.rb.angularVelocity * Mathf.Rad2Deg;
            }
            else if (smoothSyncScript.hasRigidbody2D)
            {
                velocity = smoothSyncScript.rb2D.velocity;
                angularVelocity.x = 0;
                angularVelocity.y = 0;
                angularVelocity.z = smoothSyncScript.rb2D.angularVelocity;
            }
            else
            {
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
            }
            localTimeResetIndicator = smoothSyncScript.localTimeResetIndicator;
            //atPositionalRest = smoothSyncScript.sendAtPositionalRestMessage;
            //atRotationalRest = smoothSyncScript.sendAtRotationalRestMessage;
        }
    }

    /// <summary>
    /// Wraps the StateMirror in the NetworkMessage so we can send it over the network.
    /// </summary>
    /// <remarks>
    /// This only sends and receives the parts of the StateMirror that are enabled on the SmoothSync component.
    /// </remarks>
    public struct NetworkStateMirror : NetworkMessage
    {
        /// <summary>
        /// The SmoothSync object associated with this StateMirror.
        /// </summary>
        public SmoothSyncMirror smoothSync;
        /// <summary>
        /// The StateMirror that will be sent over the network
        /// </summary>
        public StateMirror state;

        /// <summary>
        /// Copy the SmoothSync object to a NetworkStateMirror.
        /// </summary>
        /// <param name="smoothSyncScript">The SmoothSync object</param>
        public void copyFromSmoothSync(SmoothSyncMirror smoothSyncScript)
        {
            this.smoothSync = smoothSyncScript;
            state.copyFromSmoothSync(smoothSyncScript);
        }
    }

    public static class SyncProjectilesMessageFunctions
    {
        /// <summary>
        /// Serialize the message over the network.
        /// </summary>
        /// <remarks>
        /// Only sends what it needs and compresses floats if you chose to.
        /// </remarks>
        public static void Serialize(this NetworkWriter writer, NetworkStateMirror msg)
        {
            bool sendPosition, sendRotation, sendScale, sendVelocity, sendAngularVelocity, sendAtPositionalRestTag, sendAtRotationalRestTag;

            var smoothSync = msg.smoothSync;
            var state = msg.state;

            // If is a server trying to relay client information back out to other clients.
            if (NetworkServer.active && !smoothSync.hasControl)
            {
                sendPosition = state.serverShouldRelayPosition;
                sendRotation = state.serverShouldRelayRotation;
                sendScale = state.serverShouldRelayScale;
                sendVelocity = state.serverShouldRelayVelocity;
                sendAngularVelocity = state.serverShouldRelayAngularVelocity;
                sendAtPositionalRestTag = state.atPositionalRest;
                sendAtRotationalRestTag = state.atRotationalRest;
            }
            else // If is a server or client trying to send controlled object information across the network.
            {
                sendPosition = smoothSync.sendPosition;
                sendRotation = smoothSync.sendRotation;
                sendScale = smoothSync.sendScale;
                sendVelocity = smoothSync.sendVelocity;
                sendAngularVelocity = smoothSync.sendAngularVelocity;
                sendAtPositionalRestTag = smoothSync.sendAtPositionalRestMessage;
                sendAtRotationalRestTag = smoothSync.sendAtRotationalRestMessage;
            }
            // Only set last sync StateMirrors on clients here because the server needs to send multiple Serializes.
            if (!NetworkServer.active)
            {
                if (sendPosition) smoothSync.lastPositionWhenStateWasSent = state.position;
                if (sendRotation) smoothSync.lastRotationWhenStateWasSent = state.rotation;
                if (sendScale) smoothSync.lastScaleWhenStateWasSent = state.scale;
                if (sendVelocity) smoothSync.lastVelocityWhenStateWasSent = state.velocity;
                if (sendAngularVelocity) smoothSync.lastAngularVelocityWhenStateWasSent = state.angularVelocity;
            }

            writer.WriteByte(encodeSyncInformation(sendPosition, sendRotation, sendScale,
                sendVelocity, sendAngularVelocity, sendAtPositionalRestTag, sendAtRotationalRestTag));
            writer.WriteNetworkIdentity(smoothSync.netID);
            writer.WriteUInt32((uint)smoothSync.syncIndex);
            writer.WriteSingle(state.ownerTimestamp);

            // Write position.
            if (sendPosition)
            {
                if (smoothSync.isPositionCompressed)
                {
                    if (smoothSync.isSyncingXPosition)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.position.x));
                    }
                    if (smoothSync.isSyncingYPosition)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.position.y));
                    }
                    if (smoothSync.isSyncingZPosition)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.position.z));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXPosition)
                    {
                        writer.WriteSingle(state.position.x);
                    }
                    if (smoothSync.isSyncingYPosition)
                    {
                        writer.WriteSingle(state.position.y);
                    }
                    if (smoothSync.isSyncingZPosition)
                    {
                        writer.WriteSingle(state.position.z);
                    }
                }
            }
            // Write rotation.
            if (sendRotation)
            {
                Vector3 rot = state.rotation.eulerAngles;
                if (smoothSync.isRotationCompressed)
                {
                    // Convert to radians for more accurate Half numbers
                    if (smoothSync.isSyncingXRotation)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(rot.x * Mathf.Deg2Rad));
                    }
                    if (smoothSync.isSyncingYRotation)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(rot.y * Mathf.Deg2Rad));
                    }
                    if (smoothSync.isSyncingZRotation)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(rot.z * Mathf.Deg2Rad));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXRotation)
                    {
                        writer.WriteSingle(rot.x);
                    }
                    if (smoothSync.isSyncingYRotation)
                    {
                        writer.WriteSingle(rot.y);
                    }
                    if (smoothSync.isSyncingZRotation)
                    {
                        writer.WriteSingle(rot.z);
                    }
                }
            }
            // Write scale.
            if (sendScale)
            {
                if (smoothSync.isScaleCompressed)
                {
                    if (smoothSync.isSyncingXScale)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.scale.x));
                    }
                    if (smoothSync.isSyncingYScale)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.scale.y));
                    }
                    if (smoothSync.isSyncingZScale)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.scale.z));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXScale)
                    {
                        writer.WriteSingle(state.scale.x);
                    }
                    if (smoothSync.isSyncingYScale)
                    {
                        writer.WriteSingle(state.scale.y);
                    }
                    if (smoothSync.isSyncingZScale)
                    {
                        writer.WriteSingle(state.scale.z);
                    }
                }
            }
            // Write velocity.
            if (sendVelocity)
            {
                if (smoothSync.isVelocityCompressed)
                {
                    if (smoothSync.isSyncingXVelocity)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.velocity.x));
                    }
                    if (smoothSync.isSyncingYVelocity)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.velocity.y));
                    }
                    if (smoothSync.isSyncingZVelocity)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.velocity.z));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXVelocity)
                    {
                        writer.WriteSingle(state.velocity.x);
                    }
                    if (smoothSync.isSyncingYVelocity)
                    {
                        writer.WriteSingle(state.velocity.y);
                    }
                    if (smoothSync.isSyncingZVelocity)
                    {
                        writer.WriteSingle(state.velocity.z);
                    }
                }
            }
            // Write angular velocity.
            if (sendAngularVelocity)
            {
                if (smoothSync.isAngularVelocityCompressed)
                {
                    // Convert to radians for more accurate Half numbers
                    if (smoothSync.isSyncingXAngularVelocity)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.angularVelocity.x * Mathf.Deg2Rad));
                    }
                    if (smoothSync.isSyncingYAngularVelocity)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.angularVelocity.y * Mathf.Deg2Rad));
                    }
                    if (smoothSync.isSyncingZAngularVelocity)
                    {
                        writer.WriteUInt16(HalfHelper.Compress(state.angularVelocity.z * Mathf.Deg2Rad));
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXAngularVelocity)
                    {
                        writer.WriteSingle(state.angularVelocity.x);
                    }
                    if (smoothSync.isSyncingYAngularVelocity)
                    {
                        writer.WriteSingle(state.angularVelocity.y);
                    }
                    if (smoothSync.isSyncingZAngularVelocity)
                    {
                        writer.WriteSingle(state.angularVelocity.z);
                    }
                }
            }
            // Only the server sends out owner information.
            if (smoothSync.isSmoothingAuthorityChanges && NetworkServer.active)
            {
                writer.WriteByte((byte)smoothSync.ownerChangeIndicator);
            }

            if (smoothSync.automaticallyResetTime)
            {
                writer.WriteByte((byte)state.localTimeResetIndicator);
            }
        }

        /// <summary>
        /// Deserialize a message from the network.
        /// </summary>
        /// <remarks>
        /// Only receives what it needs and decompresses floats if you chose to.
        /// </remarks>
        public static NetworkStateMirror Deserialize(this NetworkReader reader)
        {
            var msg = new NetworkStateMirror();
            msg.state = new StateMirror();
            var state = msg.state;

            // The first received byte tells us what we need to be syncing.
            byte syncInfoByte = reader.ReadByte();
            bool syncPosition = shouldSyncPosition(syncInfoByte);
            bool syncRotation = shouldSyncRotation(syncInfoByte);
            bool syncScale = shouldSyncScale(syncInfoByte);
            bool syncVelocity = shouldSyncVelocity(syncInfoByte);
            bool syncAngularVelocity = shouldSyncAngularVelocity(syncInfoByte);
            state.atPositionalRest = shouldBeAtPositionalRest(syncInfoByte);
            state.atRotationalRest = shouldBeAtRotationalRest(syncInfoByte);

            NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
            if (networkIdentity == null)
            {
                Debug.LogWarning("Could not find target for network StateMirror message.");
                return new NetworkStateMirror();
            }
            uint netID = networkIdentity.netId;
            int syncIndex = (int)reader.ReadUInt32();
            state.ownerTimestamp = reader.ReadSingle();

            // Find the GameObject
            GameObject ob = NetworkIdentity.spawned[netID].gameObject;

            if (!ob)
            {
                Debug.LogWarning("Could not find target for network StateMirror message.");
                return new NetworkStateMirror();
            }

            // It doesn't matter which SmoothSync is returned since they all have the same list.
            msg.smoothSync = ob.GetComponent<SmoothSyncMirror>();

            if (!msg.smoothSync)
            {
                Debug.LogWarning("Could not find target for network StateMirror message.");
                return new NetworkStateMirror();
            }

            // Find the correct object to sync according to the syncIndex.
            for (int i = 0; i < msg.smoothSync.childObjectSmoothSyncs.Length; i++)
            {
                if (msg.smoothSync.childObjectSmoothSyncs[i].syncIndex == syncIndex)
                {
                    msg.smoothSync = msg.smoothSync.childObjectSmoothSyncs[i];
                }
            }

            var smoothSync = msg.smoothSync;

            state.receivedTimestamp = smoothSync.localTime;

            // If we want the server to relay non-owned object information out to other clients, set these variables so we know what we need to send.
            if (NetworkServer.active && !smoothSync.hasControl)
            {
                state.serverShouldRelayPosition = syncPosition;
                state.serverShouldRelayRotation = syncRotation;
                state.serverShouldRelayScale = syncScale;
                state.serverShouldRelayVelocity = syncVelocity;
                state.serverShouldRelayAngularVelocity = syncAngularVelocity;
            }

            if (smoothSync.receivedStatesCounter < smoothSync.sendRate) smoothSync.receivedStatesCounter++;

            // Read position.
            if (syncPosition)
            {
                if (smoothSync.isPositionCompressed)
                {
                    if (smoothSync.isSyncingXPosition)
                    {
                        state.position.x = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingYPosition)
                    {
                        state.position.y = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingZPosition)
                    {
                        state.position.z = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXPosition)
                    {
                        state.position.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYPosition)
                    {
                        state.position.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZPosition)
                    {
                        state.position.z = reader.ReadSingle();
                    }
                }
            }
            else
            {
                if (smoothSync.stateCount > 0)
                {
                    state.position = smoothSync.stateBuffer[0].position;
                }
                else
                {
                    state.position = smoothSync.getPosition();
                }
            }

            // Read rotation.
            if (syncRotation)
            {
                state.reusableRotationVector = Vector3.zero;
                if (smoothSync.isRotationCompressed)
                {
                    if (smoothSync.isSyncingXRotation)
                    {
                        state.reusableRotationVector.x = HalfHelper.Decompress(reader.ReadUInt16());
                        state.reusableRotationVector.x *= Mathf.Rad2Deg;
                    }
                    if (smoothSync.isSyncingYRotation)
                    {
                        state.reusableRotationVector.y = HalfHelper.Decompress(reader.ReadUInt16());
                        state.reusableRotationVector.y *= Mathf.Rad2Deg;
                    }
                    if (smoothSync.isSyncingZRotation)
                    {
                        state.reusableRotationVector.z = HalfHelper.Decompress(reader.ReadUInt16());
                        state.reusableRotationVector.z *= Mathf.Rad2Deg;
                    }
                    state.rotation = Quaternion.Euler(state.reusableRotationVector);
                }
                else
                {
                    if (smoothSync.isSyncingXRotation)
                    {
                        state.reusableRotationVector.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYRotation)
                    {
                        state.reusableRotationVector.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZRotation)
                    {
                        state.reusableRotationVector.z = reader.ReadSingle();
                    }
                    state.rotation = Quaternion.Euler(state.reusableRotationVector);
                }
            }
            else
            {
                if (smoothSync.stateCount > 0)
                {
                    state.rotation = smoothSync.stateBuffer[0].rotation;
                }
                else
                {
                    state.rotation = smoothSync.getRotation();
                }
            }
            // Read scale.
            if (syncScale)
            {
                if (smoothSync.isScaleCompressed)
                {
                    if (smoothSync.isSyncingXScale)
                    {
                        state.scale.x = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingYScale)
                    {
                        state.scale.y = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingZScale)
                    {
                        state.scale.z = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXScale)
                    {
                        state.scale.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYScale)
                    {
                        state.scale.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZScale)
                    {
                        state.scale.z = reader.ReadSingle();
                    }
                }
            }
            else
            {
                if (smoothSync.stateCount > 0)
                {
                    state.scale = smoothSync.stateBuffer[0].scale;
                }
                else
                {
                    state.scale = smoothSync.getScale();
                }
            }
            // Read velocity.
            if (syncVelocity)
            {
                if (smoothSync.isVelocityCompressed)
                {
                    if (smoothSync.isSyncingXVelocity)
                    {
                        state.velocity.x = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingYVelocity)
                    {
                        state.velocity.y = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                    if (smoothSync.isSyncingZVelocity)
                    {
                        state.velocity.z = HalfHelper.Decompress(reader.ReadUInt16());
                    }
                }
                else
                {
                    if (smoothSync.isSyncingXVelocity)
                    {
                        state.velocity.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYVelocity)
                    {
                        state.velocity.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZVelocity)
                    {
                        state.velocity.z = reader.ReadSingle();
                    }
                }
                smoothSync.latestReceivedVelocity = state.velocity;
            }
            else
            {
                // If we didn't receive an updated velocity, use the latest received velocity.
                state.velocity = smoothSync.latestReceivedVelocity;
            }
            // Read anguluar velocity.
            if (syncAngularVelocity)
            {
                if (smoothSync.isAngularVelocityCompressed)
                {
                    state.reusableRotationVector = Vector3.zero;
                    if (smoothSync.isSyncingXAngularVelocity)
                    {
                        state.reusableRotationVector.x = HalfHelper.Decompress(reader.ReadUInt16());
                        state.reusableRotationVector.x *= Mathf.Rad2Deg;
                    }
                    if (smoothSync.isSyncingYAngularVelocity)
                    {
                        state.reusableRotationVector.y = HalfHelper.Decompress(reader.ReadUInt16());
                        state.reusableRotationVector.y *= Mathf.Rad2Deg;
                    }
                    if (smoothSync.isSyncingZAngularVelocity)
                    {
                        state.reusableRotationVector.z = HalfHelper.Decompress(reader.ReadUInt16());
                        state.reusableRotationVector.z *= Mathf.Rad2Deg;
                    }
                    state.angularVelocity = state.reusableRotationVector;
                }
                else
                {
                    if (smoothSync.isSyncingXAngularVelocity)
                    {
                        state.angularVelocity.x = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingYAngularVelocity)
                    {
                        state.angularVelocity.y = reader.ReadSingle();
                    }
                    if (smoothSync.isSyncingZAngularVelocity)
                    {
                        state.angularVelocity.z = reader.ReadSingle();
                    }
                }
                smoothSync.latestReceivedAngularVelocity = state.angularVelocity;
            }
            else
            {
                // If we didn't receive an updated angular velocity, use the latest received angular velocity.
                state.angularVelocity = smoothSync.latestReceivedAngularVelocity;
            }

            // Update new owner information sent from the Server.
            if (smoothSync.isSmoothingAuthorityChanges && !NetworkServer.active)
            {
                smoothSync.ownerChangeIndicator = (int)reader.ReadByte();
            }

            if (smoothSync.automaticallyResetTime)
            {
                state.localTimeResetIndicator = (int)reader.ReadByte();
            }

            return msg;
        }
        /// <summary>
        /// Hardcoded information to determine position syncing.
        /// </summary>
        const byte positionMask = 1;        // 0000_0001
        /// <summary>
        /// Hardcoded information to determine rotation syncing.
        /// </summary>
        const byte rotationMask = 2;        // 0000_0010
        /// <summary>
        /// Hardcoded information to determine scale syncing.
        /// </summary>
        const byte scaleMask = 4;        // 0000_0100
        /// <summary>
        /// Hardcoded information to determine velocity syncing.
        /// </summary>
        const byte velocityMask = 8;        // 0000_1000
        /// <summary>
        /// Hardcoded information to determine angular velocity syncing.
        /// </summary>
        const byte angularVelocityMask = 16; // 0001_0000
        /// <summary>
        /// Hardcoded information to determine whether the object is at rest and should stop extrapolating.
        /// </summary>
        const byte atPositionalRestMask = 64; // 0100_0000
        /// <summary>
        /// Hardcoded information to determine whether the object is at rest and should stop extrapolating.
        /// </summary>
        const byte atRotationalRestMask = 128; // 1000_0000
        /// <summary>
        /// Encode sync info based on what we want to send.
        /// </summary>
        static byte encodeSyncInformation(bool sendPosition, bool sendRotation, bool sendScale, bool sendVelocity, bool sendAngularVelocity, bool atPositionalRest, bool atRotationalRest)
        {
            byte encoded = 0;

            if (sendPosition)
            {
                encoded = (byte)(encoded | positionMask);
            }
            if (sendRotation)
            {
                encoded = (byte)(encoded | rotationMask);
            }
            if (sendScale)
            {
                encoded = (byte)(encoded | scaleMask);
            }
            if (sendVelocity)
            {
                encoded = (byte)(encoded | velocityMask);
            }
            if (sendAngularVelocity)
            {
                encoded = (byte)(encoded | angularVelocityMask);
            }
            if (atPositionalRest)
            {
                encoded = (byte)(encoded | atPositionalRestMask);
            }
            if (atRotationalRest)
            {
                encoded = (byte)(encoded | atRotationalRestMask);
            }
            return encoded;
        }
        /// <summary>
        /// Decode sync info to see if we want to sync position.
        /// </summary>
        static bool shouldSyncPosition(byte syncInformation)
        {
            if ((syncInformation & positionMask) == positionMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we want to sync rotation.
        /// </summary>
        static bool shouldSyncRotation(byte syncInformation)
        {
            if ((syncInformation & rotationMask) == rotationMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we want to sync scale.
        /// </summary>
        static bool shouldSyncScale(byte syncInformation)
        {
            if ((syncInformation & scaleMask) == scaleMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we want to sync velocity.
        /// </summary>
        static bool shouldSyncVelocity(byte syncInformation)
        {
            if ((syncInformation & velocityMask) == velocityMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we want to sync angular velocity.
        /// </summary>
        static bool shouldSyncAngularVelocity(byte syncInformation)
        {
            if ((syncInformation & angularVelocityMask) == angularVelocityMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we should be at positional rest. (Stop extrapolating)
        /// </summary>
        static bool shouldBeAtPositionalRest(byte syncInformation)
        {
            if ((syncInformation & atPositionalRestMask) == atPositionalRestMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decode sync info to see if we should be at rotational rest. (Stop extrapolating)
        /// </summary>
        static bool shouldBeAtRotationalRest(byte syncInformation)
        {
            if ((syncInformation & atRotationalRestMask) == atRotationalRestMask)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}