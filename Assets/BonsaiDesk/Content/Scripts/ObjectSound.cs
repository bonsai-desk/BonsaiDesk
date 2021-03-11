using UnityEngine;

public class ObjectSound : MonoBehaviour
{
    private Rigidbody body;

    public OVR.SoundFXRef audioClipsNormal;
    public OVR.SoundFXRef audioClipsQuiet;
    public OVR.SoundFXRef settle;
    public OVR.SoundFXRef roll;
    private int emitterId;
    private AudioSource rollSource;

    private float lastHitTime;
    private bool onGround = false;
    private bool playedStaySound = false;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        emitterId = -1;
    }

    private void Update()
    {
        if (onGround && Time.time - lastHitTime > 0.1f)
        {
            if (!playedStaySound)
            {
                playedStaySound = true;
                settle.PlaySoundAt(transform.position, 0, 0.7f, 1);

                emitterId = roll.PlaySoundAt(transform.position);
                if (emitterId > -1)
                {
                    OVR.AudioManager.AttachSoundToParent(emitterId, transform);
                    rollSource = GameObject.Find("SoundEmitter_" + emitterId.ToString()).GetComponent<AudioSource>();
                }
            }
            if (emitterId > -1)
            {
                Vector2 velocity = new Vector2(body.velocity.x, body.velocity.z);
                if (rollSource != null)
                    rollSource.volume = Mathf.Clamp(velocity.magnitude, 0, 0.75f);
            }
        }
        if (transform.position.y < -10f)
        {
            StopSound();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Table"))
        {
            StopSound();
            float volume = Mathf.Clamp(collision.relativeVelocity.magnitude, 0.05f, 1f);
            if (collision.relativeVelocity.magnitude > 1f)
                audioClipsNormal.PlaySoundAt(transform.position, 0, volume, 1);
            else
                audioClipsQuiet.PlaySoundAt(transform.position, 0, volume, 1);
            lastHitTime = Time.time;
            onGround = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Table"))
        {
            onGround = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Table"))
        {
            playedStaySound = false;
            onGround = false;
            StopSound();
        }
    }

    private void StopSound()
    {
        if (emitterId > -1)
        {
            if (rollSource != null)
                rollSource.volume = 0;
            OVR.AudioManager.DetachSoundFromParent(emitterId);
            OVR.AudioManager.StopSound(emitterId);
            emitterId = -1;
        }
    }
}