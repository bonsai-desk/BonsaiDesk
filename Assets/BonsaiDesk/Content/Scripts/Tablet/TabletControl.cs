﻿using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Networking;

public class TabletControl : NetworkBehaviour
{
    [SyncVar(hook = nameof(VideoIdHook))] public string videoId;
    [SyncVar(hook = nameof(ServerLerpingHook))] private bool _serverLerping = false;

    public PhysicMaterial lowFrictionPhysicMaterial;
    public BoxCollider worldBox;
    public BoxCollider fingerBox;
    public TabletCollider tabletCollider;
    public Rigidbody tabletBody;
    public MeshRenderer thumbnailRenderer;

    private PhysicMaterial _defaultPhysicMaterial;
    private Vector2 _bounds;

    private void Start()
    {
        _defaultPhysicMaterial = worldBox.sharedMaterial;
        _bounds = thumbnailRenderer.transform.localScale.xy();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(LoadThumbnail(videoId));
        SetInteractable(!_serverLerping);
    }

    private void Update()
    {
        worldBox.sharedMaterial =
            tabletCollider.NumFingersTouching >= 3 ? lowFrictionPhysicMaterial : _defaultPhysicMaterial;
        tabletBody.mass = tabletCollider.NumFingersTouching >= 3 ? 0.050f : 0.300f;
    }

    [Server]
    public void SetServerLerping(bool serverLerping)
    {
        if (serverLerping == _serverLerping)
            return;
        
        _serverLerping = serverLerping;
        SetInteractable(!serverLerping);
    }

    private void SetInteractable(bool interactable)
    {
        worldBox.enabled = interactable;
        fingerBox.enabled = interactable;
    }

    public void TabletPlay()
    {
        TabletSpot.Instance.CmdSetNewVideo(netIdentity);
    }

    private void VideoIdHook(string oldValue, string newValue)
    {
        StartCoroutine(LoadThumbnail(newValue));
    }
    
    private void ServerLerpingHook(bool oldValue, bool newValue)
    {
        SetInteractable(!newValue);
    }

    private IEnumerator LoadThumbnail(string newVideoId, bool maxRes = true)
    {
        if (string.IsNullOrEmpty(newVideoId))
            yield break;

        string url = $"https://img.youtube.com/vi/{newVideoId}/";
        url += maxRes ? "maxresdefault.jpg" : "0.jpg";

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                if (maxRes)
                {
                    print("Could not get max res thumbnail. Retrying with 0.jpg");
                    yield return LoadThumbnail(newVideoId, false);
                }
                else
                {
                    Debug.LogError("Could not get thumbnail: " + uwr.error);
                }
            }
            else
            {
                var texture = DownloadHandlerTexture.GetContent(uwr);
                
                //generate new texture with mipmaps
                var newTexture = new Texture2D(texture.width, texture.height);
                newTexture.SetPixels(texture.GetPixels(0));
                newTexture.Apply();

                var aspectRatio = (float) newTexture.width / newTexture.height;
                var localScale = new Vector3(_bounds.y * aspectRatio, _bounds.y, 1);
                if (localScale.x > _bounds.x)
                    localScale = new Vector3(_bounds.x, _bounds.x * (1f / aspectRatio), 1);

                thumbnailRenderer.transform.localScale = localScale;
                thumbnailRenderer.material.mainTexture = newTexture;
                
                Destroy(texture);
            }
        }
    }
}