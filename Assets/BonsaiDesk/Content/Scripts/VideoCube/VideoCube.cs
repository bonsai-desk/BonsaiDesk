using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public class VideoCube : NetworkBehaviour
{
    private static Mesh VideoCubeMesh;

    private List<Vector3> _vertices = new List<Vector3>();
    private List<Vector3> _normals = new List<Vector3>();
    private List<Vector2> _uv = new List<Vector2>();
    private List<Vector3> _lerpDir = new List<Vector3>();
    private List<int> _triangles = new List<int>();

    public SmoothSyncVars smoothSyncVars;
    public Transform quad;
    public Transform triangle;
    public Transform playIcon;
    private Vector3 _targetScale;

    private Material _material;
    private Material _hologramMaterial;

    private float _lerp;
    private const float AnimationTime = 0.25f;
    private const float ActivationRadius = 0.125f;

    private Rigidbody _body;

    private static VideoCube _localActiveCube = null;
    private static float _localActiveCubeDistance = float.PositiveInfinity;

    private void Awake()
    {
        _targetScale = quad.localScale;
        quad.localScale = Vector3.zero;
        triangle.localScale = Vector3.zero;
        playIcon.localScale = Vector3.zero;
    }

    void Start()
    {
        _body = GetComponent<Rigidbody>();

        if (!VideoCubeMesh)
        {
            var mesh = new Mesh();

            Face(new Vector3(0, 0, -0.5f), Quaternion.identity);
            Face(new Vector3(0.5f, 0, 0), Quaternion.AngleAxis(-90, Vector3.up));
            Face(new Vector3(0, 0, 0.5f), Quaternion.AngleAxis(180, Vector3.up));
            Face(new Vector3(-0.5f, 0, 0), Quaternion.AngleAxis(90, Vector3.up));
            Quad(new Vector3(0, 0.5f, 0), Quaternion.AngleAxis(90, Vector3.right), Vector3.one, false);
            Quad(new Vector3(0, -0.5f, 0), Quaternion.AngleAxis(-90, Vector3.right), Vector3.one, false);

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uv);
            mesh.SetUVs(1, _lerpDir);
            mesh.SetTriangles(_triangles, 0);

            VideoCubeMesh = mesh;
        }

        GetComponent<MeshFilter>().sharedMesh = VideoCubeMesh;
    }

    private void Update()
    {
        var inRange = InRange();
        var shouldShowThumbnail = inRange && !_body.isKinematic;
        var clientAuthority = smoothSyncVars.AutoAuthority.isClient && smoothSyncVars.AutoAuthority.ClientHasAuthority();
        var serverAuthority = smoothSyncVars.AutoAuthority.isServer && smoothSyncVars.AutoAuthority.ServerHasAuthority();

        if (shouldShowThumbnail && smoothSyncVars.AutoAuthority.isClient && !clientAuthority)
        {
            smoothSyncVars.RequestAuthority();
        }

        if (clientAuthority)
        {
            smoothSyncVars.Set("showThumbnail", shouldShowThumbnail);
        }

        if (serverAuthority)
        {
            smoothSyncVars.Set("showThumbnail", false);
        }

        _lerp = CubicBezier.EaseOut.MoveTowards01(_lerp, AnimationTime, smoothSyncVars.Get("showThumbnail"));

        var toHead = InputManager.Hands.head.position - transform.position;
        toHead.y = 0;
        var atHead = Quaternion.LookRotation(-toHead);
        quad.rotation = atHead;
        playIcon.rotation = atHead;

        var startPosition = transform.position;
        var targetPosition = transform.position + new Vector3(0, 2.5f * 0.05f, 0);
        quad.position = Vector3.Lerp(startPosition, targetPosition, _lerp);
        quad.localScale = Vector3.Lerp(Vector3.zero, _targetScale, _lerp);

        playIcon.position = Vector3.Lerp(startPosition, targetPosition + atHead * Vector3.forward * -0.01f, _lerp);
        playIcon.localScale = Vector3.Lerp(Vector3.zero, new Vector3(1.25f, 1.25f, 1f), _lerp);

        CalculateTriangle(atHead);
    }

    private bool InRange()
    {
        var inRange = false;
        float closest = float.PositiveInfinity;
        if (Vector3.SqrMagnitude(InputManager.Hands.Left.PlayerHand.palm.position - transform.position) < ActivationRadius * ActivationRadius ||
            Vector3.SqrMagnitude(InputManager.Hands.Right.PlayerHand.palm.position - transform.position) < ActivationRadius * ActivationRadius)
        {
            inRange = true;
        }
        else
        {
            for (int i = 0; i < InputManager.Hands.physicsFingerTipPositions.Length; i++)
            {
                var horizontalDistance = Vector2.SqrMagnitude(InputManager.Hands.physicsFingerTipPositions[i].xz() - transform.position.xz());
                if (horizontalDistance < ActivationRadius * ActivationRadius && InputManager.Hands.physicsFingerTipPositions[i].y > transform.position.y &&
                    InputManager.Hands.physicsFingerTipPositions[i].y < transform.position.y + 0.2f)
                {
                    inRange = true;
                }

                var distance = horizontalDistance + Mathf.Abs(InputManager.Hands.physicsFingerTipPositions[i].y - transform.position.y);
                if (distance < closest)
                {
                    closest = distance;
                }
            }
        }

        if (_localActiveCube == this)
        {
            _localActiveCubeDistance = closest;
        }

        if (!inRange && _localActiveCube == this)
        {
            _localActiveCubeDistance = float.PositiveInfinity;
            _localActiveCube = null;
        }

        if (inRange && (closest < _localActiveCubeDistance || !_localActiveCube))
        {
            _localActiveCubeDistance = closest;
            _localActiveCube = this;
        }

        if (_localActiveCube != this)
        {
            inRange = false;
        }

        return inRange;
    }

    private void CalculateTriangle(Quaternion atHead)
    {
        triangle.position = transform.position + new Vector3(0, 0.5f * 0.05f, 0);
        triangle.rotation = atHead;
        triangle.localScale = new Vector3(quad.localScale.x,
            (quad.transform.position.y - transform.position.y) * (1f / 0.05f) - (quad.localScale.y / 2f) - 0.5f, 1);
    }

    private readonly Vector3[] quadVertices = new[]
    {
        new Vector3(-0.5f, -0.5f, 0),
        new Vector3(0.5f, -0.5f, 0),
        new Vector3(0.5f, 0.5f, 0),
        new Vector3(-0.5f, 0.5f, 0)
    };

    private readonly Vector2[] quadUvs = new[]
    {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1)
    };

    private readonly int[] quadTriangles = new[]
    {
        0, 2, 1,
        0, 3, 2
    };

    private void Quad(Vector3 position, Quaternion rotation, Vector3 scale, bool hasUvs)
    {
        for (int i = 0; i < quadTriangles.Length; i++)
        {
            _triangles.Add(quadTriangles[i] + _vertices.Count);
        }

        for (int i = 0; i < 4; i++)
        {
            var v = new Vector3(quadVertices[i].x * scale.x, quadVertices[i].y * scale.y, quadVertices[i].z * scale.z);
            v = rotation * v;
            v += position;
            _vertices.Add(v);
            _normals.Add(rotation * Vector3.back);
            if (hasUvs)
            {
                _uv.Add(quadUvs[i]);
            }
            else
            {
                _uv.Add(-Vector2.one);
            }

            _lerpDir.Add(Vector2.zero);
        }
    }

    private void Face(Vector3 position, Quaternion rotation)
    {
        var offset = rotation * new Vector3(0, 0.5f, 0);
        var scale = new Vector3(1, 0, 1);
        Quad(position + offset, rotation, scale, false);
        Quad(position, rotation, Vector3.one, true);
        Quad(position - offset, rotation, scale, false);

        var up = rotation * Vector3.up;
        var down = -up;

        const int numVerticesAdded = 12;
        _lerpDir[_lerpDir.Count - numVerticesAdded + 0] = down;
        _lerpDir[_lerpDir.Count - numVerticesAdded + 1] = down;

        _lerpDir[_lerpDir.Count - numVerticesAdded + 0 + 4] = up;
        _lerpDir[_lerpDir.Count - numVerticesAdded + 1 + 4] = up;
        _lerpDir[_lerpDir.Count - numVerticesAdded + 2 + 4] = down;
        _lerpDir[_lerpDir.Count - numVerticesAdded + 3 + 4] = down;

        _lerpDir[_lerpDir.Count - numVerticesAdded + 2 + 8] = up;
        _lerpDir[_lerpDir.Count - numVerticesAdded + 3 + 8] = up;
    }

    public IEnumerator LoadThumbnail(string newVideoId, bool maxRes = true)
    {
        if (string.IsNullOrEmpty(newVideoId))
        {
            yield break;
        }

        var url = $"https://img.youtube.com/vi/{newVideoId}/";
        url += maxRes ? "maxresdefault.jpg" : "0.jpg";

        using (var uwr = UnityWebRequestTexture.GetTexture(url))
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
                var newTexture = new Texture2D(texture.width, texture.height, texture.format, true);

                if (SystemInfo.copyTextureSupport != CopyTextureSupport.None)
                {
                    Graphics.CopyTexture(texture, 0, 0, newTexture, 0, 0);
                }
                else
                {
                    newTexture.LoadImage(uwr.downloadHandler.data);
                }

                newTexture.filterMode = FilterMode.Trilinear;
                newTexture.Apply();

                if (!_material)
                {
                    var autoAuthority = GetComponent<AutoAuthority>();
                    if (autoAuthority.cachedMaterial)
                    {
                        _material = autoAuthority.cachedMaterial;
                    }
                    else
                    {
                        _material = new Material(GetComponent<MeshRenderer>().sharedMaterial);
                        autoAuthority.SetCachedMaterial(_material);
                    }
                }

                _material.mainTexture = newTexture;

                var aspectRatio = (float) newTexture.width / newTexture.height;
                if (aspectRatio < 1f)
                {
                    Debug.LogError("[Bonsai Desk] Portrait thumbnail will be squashed to a square.");
                }

                _material.SetFloat("_AspectRatio", aspectRatio);

                var hologramQuad = quad.GetComponent<MeshRenderer>();
                _hologramMaterial = new Material(hologramQuad.sharedMaterial);
                hologramQuad.sharedMaterial = _hologramMaterial;
                _hologramMaterial.mainTexture = newTexture;

                var bounds = _targetScale.xy();
                var localScale = new Vector3(bounds.y * aspectRatio, bounds.y, 1);
                if (localScale.x > bounds.x)
                {
                    localScale = new Vector3(bounds.x, bounds.x * (1f / aspectRatio), 1);
                }

                _targetScale = localScale;

                quad.gameObject.SetActive(true);
                triangle.gameObject.SetActive(true);
                playIcon.gameObject.SetActive(true);

                Destroy(texture);
            }
        }
    }

    private void OnDestroy()
    {
        Destroy(_material);
        Destroy(_hologramMaterial);
    }
}