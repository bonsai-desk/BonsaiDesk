using System.Collections;
using System.Collections.Generic;
using OVR;
using UnityEngine;

public class MessageStack : MonoBehaviour
{
    private const float StepSize = 0.25f;
    private const float Duration = 5f;
    public static MessageStack Singleton;
    public MessageCanvas messageObject;

    public Transform spawnLocation;
    public Transform firstLocation;

    public SoundFXRef messageSound;
    private List<Message> _messages = new List<Message>();

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    private void Update()
    {
        PruneMessages();
        UpdateAllMessageTransforms();
    }

    private IEnumerator DelayAddMessage(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        AddMessage("Dummy Message");
    }

    public void AddMessage(string text)
    {
        BumpAll();
        var msg = Instantiate(messageObject, spawnLocation);
        msg.SetText(text);
        msg.SetEnabled(false);
        _messages.Add(new Message(msg, firstLocation));
    }

    private void BumpAll()
    {
        foreach (var message in _messages)
        {
            message.BumpMessage();
        }
    }

    private void UpdateAllMessageTransforms()
    {
        foreach (var message in _messages)
        {
            UpdateMessageTransform(message);
        }

        for (var i = 0; i < _messages.Count; i++)
        {
            if (i == 0 && !_messages[i].IsEnabled())
            {
                _messages[i].SetEnabled(true);
                messageSound.PlaySoundAt(_messages[i].MessageCanvas.transform.position);
            }
            else if (!_messages[i].IsEnabled() && i != 0)
            {
                var messageLoc = _messages[i].MessageCanvas.transform.position;
                var nextMessageLoc = _messages[i - 1].MessageCanvas.transform.position;
                var distance = Vector3.Distance(messageLoc, nextMessageLoc);
                var farEnoughAway = distance / StepSize > 0.99;
                if (farEnoughAway)
                {
                    _messages[i].SetEnabled(true);
                    messageSound.PlaySoundAt(_messages[i].MessageCanvas.transform.position);
                }
            }
        }
    }

    private static void UpdateMessageTransform(Message message)
    {
        var target = message.TargetPosition;
        var transform1 = message.MessageCanvas.transform;
        var location = transform1.position;
        var atTarget = target - location;
        var newLoc = location + 0.15f * atTarget;
        transform1.position = newLoc;
    }

    private void PruneMessages()
    {
        var newMessages = new List<Message>();
        foreach (var message in _messages)
        {
            if (Time.time - message.TimeAdded < Duration)
            {
                newMessages.Add(message);
            }
            else
            {
                Destroy(message.MessageCanvas.gameObject);
            }
        }

        _messages = newMessages;
    }

    private class Message
    {
        public readonly MessageCanvas MessageCanvas;
        public readonly double TimeAdded;
        public Vector3 TargetPosition;

        public Message(MessageCanvas messageCanvas, Transform firstLocation)
        {
            TimeAdded = Time.time;
            MessageCanvas = messageCanvas;
            TargetPosition = firstLocation.position;
        }

        public void BumpMessage()
        {
            TargetPosition.y += StepSize;
        }

        public void SetEnabled(bool enabled)
        {
            MessageCanvas.SetEnabled(enabled);
        }

        public bool IsEnabled()
        {
            return MessageCanvas.IsEnabled();
        }
    }
}