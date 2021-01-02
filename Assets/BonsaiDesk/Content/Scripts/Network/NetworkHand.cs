﻿using Mirror;
using UnityEngine;

public class NetworkHand : NetworkBehaviour
{
    public OVRSkeleton.SkeletonType _skeletonType;
    private OVRHandTransformMapper mapper;

    [SyncVar(hook = nameof(FingerRotationsHook))]
    private ulong fingerRotations = 0;

    [SyncVar] private ulong thumbRotations = 0;

    private float lastSetTime = 0;

    private float[] renderedFingerRotations = new float[18];
    private Quaternion renderedThumbRotation = Quaternion.identity;

    private float[] oldRenderedFingerRotations = new float[18];
    private Quaternion oldRenderedThumbRotation = Quaternion.identity;

    public float updateInterval = 1f / 10f;

    private float lastRotationsUpdateTime = 0;

    public SkinnedMeshRenderer meshRenderer;

    public Texture[] handTextures;

    [SyncVar(hook = nameof(ColorHook))] private byte colorIndex = 0;

    public LineRenderer lineRenderer;

    private void Start()
    {
        mapper = GetComponent<OVRHandTransformMapper>();
        for (int i = 0; i < renderedFingerRotations.Length; i++)
            renderedFingerRotations[i] = 0;

        UpdateColor();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!hasAuthority)
            return;

        CmdSetColor(NetworkManagerGame.AssignedColorIndex);

        if (_skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            PlayerHands.hands.left.networkHand = this;
        if (_skeletonType == OVRSkeleton.SkeletonType.HandRight)
            PlayerHands.hands.right.networkHand = this;
    }

    private void Update()
    {
        if (!hasAuthority)
        {
            if (Time.time - lastRotationsUpdateTime < updateInterval)
                SetFingerRotations( /*hand*/);
            return;
        }

        PlayerHand hand;
        if (_skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            hand = PlayerHands.hands.left;
        else
            hand = PlayerHands.hands.right;

        if (!hand.Tracking())
            return;

        if (hand.oVRSkeleton.IsInitialized && Time.time - lastSetTime > updateInterval)
        {
            lastSetTime = Time.time;
            var rotations = GetFingerRotations(hand);
            CmdSetFingerRotations(rotations.rotations, rotations.tRotations);
        }
    }

    [Command]
    private void CmdSetColor(int color)
    {
        colorIndex = (byte) color;
    }

    private void ColorHook(byte oldColor, byte newColor)
    {
        UpdateColor();
    }

    private void UpdateColor()
    {
        meshRenderer.material.SetTexture("_MainTex", handTextures[colorIndex]);
        if (hasAuthority)
        {
            PlayerHands.hands.left.material.SetTexture("_MainTex", handTextures[colorIndex]);
            PlayerHands.hands.right.material.SetTexture("_MainTex", handTextures[colorIndex]);
        }
    }

    private void FingerRotationsHook(ulong oldRotations, ulong newRotations)
    {
        SetOldRotations();
    }

    [Command]
    private void CmdSetFingerRotations(ulong rotations, ulong tRotations)
    {
        fingerRotations = rotations;
        thumbRotations = tRotations;

        SetOldRotations();
    }

    private void SetOldRotations()
    {
        for (int i = 0; i < renderedFingerRotations.Length; i++)
            oldRenderedFingerRotations[i] = renderedFingerRotations[i];
        oldRenderedThumbRotation = renderedThumbRotation;
        lastRotationsUpdateTime = Time.time;
    }

    private (ulong rotations, ulong tRotations) GetFingerRotations(PlayerHand hand)
    {
        ulong rotations = 0;
        ulong tRotations = 0;

        //index middle ring pinky
        for (int i = 0; i < 4; i++)
        {
            int fingerIndex = 6 + (i * 3);
            if (i == 3)
                fingerIndex++;

            Vector3 localRight1 = hand.mapper.CustomBones[fingerIndex].localRotation * Vector3.right;
            localRight1.z = 0;
            float rotation1 = Vector3.Angle(Vector3.right, localRight1);
            rotation1 = Mathf.Clamp(rotation1, 0f, 90f);
            byte rotation1Byte = (byte) Mathf.FloorToInt((rotation1 / 90f) * 255f);
            rotation1Byte = (byte) Mathf.Clamp(rotation1Byte, 0, 255);
            rotations <<= 8;
            rotations |= rotation1Byte;

            Vector3 localRight2 = hand.mapper.CustomBones[fingerIndex + 1].localRotation * Vector3.right;
            localRight2.z = 0;
            float rotation2 = Vector3.Angle(Vector3.right, localRight2);
            rotation2 = Mathf.Clamp(rotation2, 0f, 90f);
            byte rotation2Byte = (byte) Mathf.FloorToInt((rotation2 / 90f) * 255f);
            rotation2Byte = (byte) Mathf.Clamp(rotation2Byte, 0, 255);
            rotations <<= 8;
            rotations |= rotation2Byte;
        }

        //thumb
        int thumbIndex = 3;

        Quaternion localRotation = hand.mapper.CustomBones[thumbIndex].localRotation;
        for (int i = 0; i < 4; i++)
        {
            byte qPart = (byte) ((localRotation[i] + 1f) / 2f * 255f);
            tRotations <<= 8;
            tRotations |= qPart;
        }

        Vector3 localRightThumb1 = hand.mapper.CustomBones[thumbIndex + 1].localRotation * Vector3.right;
        localRightThumb1.z = 0;
        float rotationThumb1 = Vector3.Angle(Vector3.right, localRightThumb1);
        rotationThumb1 = Mathf.Clamp(rotationThumb1, 0f, 90f);
        byte rotationThumb1Byte = (byte) Mathf.FloorToInt((rotationThumb1 / 90f) * 255f);
        rotationThumb1Byte = (byte) Mathf.Clamp(rotationThumb1Byte, 0, 255);
        tRotations <<= 8;
        tRotations |= rotationThumb1Byte;

        Vector3 localRightThumb2 = hand.mapper.CustomBones[thumbIndex + 2].localRotation * Vector3.right;
        localRightThumb2.z = 0;
        float rotationThumb2 = Vector3.Angle(Vector3.right, localRightThumb2);
        rotationThumb2 = Mathf.Clamp(rotationThumb2, 0f, 90f);
        byte rotationThumb2Byte = (byte) Mathf.FloorToInt((rotationThumb2 / 90f) * 255f);
        rotationThumb2Byte = (byte) Mathf.Clamp(rotationThumb2Byte, 0, 255);
        tRotations <<= 8;
        tRotations |= rotationThumb2Byte;

        return (rotations, tRotations);
    }

    private void SetFingerRotations( /*PlayerHand hand*/)
    {
        ulong rotations = fingerRotations;
        ulong tRotations = thumbRotations;

        //index middle ring pinky
        for (int i = 3; i >= 0; i--)
        {
            int fingerIndex = 6 + (i * 3);
            if (i == 3)
                fingerIndex++;

            byte rotation2Byte = (byte) (rotations & 0b_1111_1111);
            rotations >>= 8;
            float rotation2 = (rotation2Byte / 255f) * 90f;

            byte rotation1Byte = (byte) (rotations & 0b_1111_1111);
            rotations >>= 8;
            float rotation1 = (rotation1Byte / 255f) * 90f;

            renderedFingerRotations[fingerIndex] += (rotation1 - oldRenderedFingerRotations[fingerIndex]) *
                Time.deltaTime / updateInterval;
            renderedFingerRotations[fingerIndex + 1] += (rotation2 - oldRenderedFingerRotations[fingerIndex + 1]) *
                Time.deltaTime / updateInterval;

            // mapper.CustomBones[fingerIndex].localRotation = hand.oVRSkeleton.BindPoses[fingerIndex].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[fingerIndex]);
            // mapper.CustomBones[fingerIndex + 1].localRotation = hand.oVRSkeleton.BindPoses[fingerIndex + 1].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[fingerIndex + 1]);
            // mapper.CustomBones[fingerIndex + 2].localRotation = hand.oVRSkeleton.BindPoses[fingerIndex + 2].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[fingerIndex + 1]);

            mapper.CustomBones[fingerIndex].localEulerAngles = new Vector3(
                mapper.CustomBones[fingerIndex].localRotation.x, mapper.CustomBones[fingerIndex].localRotation.y,
                -renderedFingerRotations[fingerIndex]);
            mapper.CustomBones[fingerIndex + 1].localEulerAngles = new Vector3(
                mapper.CustomBones[fingerIndex + 1].localRotation.x,
                mapper.CustomBones[fingerIndex + 1].localRotation.y, -renderedFingerRotations[fingerIndex + 1]);
            mapper.CustomBones[fingerIndex + 2].localEulerAngles = new Vector3(
                mapper.CustomBones[fingerIndex + 2].localRotation.x,
                mapper.CustomBones[fingerIndex + 2].localRotation.y, -renderedFingerRotations[fingerIndex + 1]);
        }

        //thumb
        int thumbIndex = 3;

        byte rotationThumb2Byte = (byte) (tRotations & 0b_1111_1111);
        tRotations >>= 8;
        float rotationThumb2 = (rotationThumb2Byte / 255f) * 90f;

        //byte rotationThumb1Byte = (byte)(tRotations & 0b_1111_1111);
        tRotations >>= 8;
        //float rotationThumb1 = (rotationThumb1Byte / 255f) * 90f;

        renderedFingerRotations[thumbIndex + 1] += (rotationThumb2 - oldRenderedFingerRotations[thumbIndex + 1]) *
            Time.deltaTime / updateInterval;
        renderedFingerRotations[thumbIndex + 2] += (rotationThumb2 - oldRenderedFingerRotations[thumbIndex + 2]) *
            Time.deltaTime / updateInterval;

        Quaternion localRotation = Quaternion.identity;
        for (int i = 3; i >= 0; i--)
        {
            byte qPart = (byte) (tRotations & 0b_1111_1111);
            localRotation[i] = (qPart / 255f) * 2f - 1f;
            tRotations >>= 8;
        }

        renderedThumbRotation = Quaternion.RotateTowards(renderedThumbRotation, localRotation,
            Vector3.Angle(oldRenderedThumbRotation * Vector3.right, localRotation * Vector3.right) * Time.deltaTime /
            updateInterval);

        mapper.CustomBones[thumbIndex].localRotation = renderedThumbRotation;

        // mapper.CustomBones[thumbIndex + 1].localRotation = hand.oVRSkeleton.BindPoses[thumbIndex + 1].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[thumbIndex + 1]);
        // mapper.CustomBones[thumbIndex + 2].localRotation = hand.oVRSkeleton.BindPoses[thumbIndex + 2].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[thumbIndex + 2]);

        mapper.CustomBones[thumbIndex + 1].localEulerAngles = new Vector3(
            mapper.CustomBones[thumbIndex + 1].localEulerAngles.x,
            mapper.CustomBones[thumbIndex + 1].localEulerAngles.y, -renderedFingerRotations[thumbIndex + 1]);
        mapper.CustomBones[thumbIndex + 2].localEulerAngles = new Vector3(
            mapper.CustomBones[thumbIndex + 2].localEulerAngles.x,
            mapper.CustomBones[thumbIndex + 2].localEulerAngles.y, -renderedFingerRotations[thumbIndex + 2]);
    }
}