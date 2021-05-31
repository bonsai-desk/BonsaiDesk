using System;
using System.Collections;
using System.Collections.Generic;
using OVR;
using UnityEngine;

public class MessageStack : MonoBehaviour
{
    public enum MessageType
    {
        Bad,
        Neutral,
        Good
    }

    private const float StepSize = 0.25f;
    public static MessageStack Singleton;
    public MessageCanvas messageObject;

    public Transform spawnLocation;
    public Transform firstLocation;

    public SoundFXRef badMessageSound;
    public SoundFXRef neutralMessageSound;
    public SoundFXRef goodMessageSound;
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

    private IEnumerator DemoMessages()
    {
        yield return new WaitForSeconds(5);
        AddMessage("good", MessageType.Good);
        yield return new WaitForSeconds(2);
        AddMessage("neutral", MessageType.Neutral);
        yield return new WaitForSeconds(2);
        AddMessage("bad", MessageType.Bad);
    }

    public int AddMessage(string text, MessageType messageType = MessageType.Neutral, float duration = 5f)
    {
        BumpAll();
        var msg = Instantiate(messageObject, spawnLocation);
        msg.SetText(text);
        msg.SetEnabled(false);
        BonsaiLog($"Message ({text}) ({messageType})");
        switch (messageType)
        {
            case MessageType.Bad:
                msg.SetColor(MessageCanvas.BorderColor.Red);
                break;
            case MessageType.Neutral:
                msg.SetColor(MessageCanvas.BorderColor.Gray);
                break;
            case MessageType.Good:
                msg.SetColor(MessageCanvas.BorderColor.Green);
                break;
        }

        _messages.Add(new Message(msg, firstLocation, messageType, duration));
        return _messages[_messages.Count - 1].GetHashCode();
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiMessage: </color>: " + msg);
    }

    private void BumpAll()
    {
        foreach (var message in _messages)
        {
            message.BumpMessage();
        }
    }

    private void PlaySound(Message message)
    {
        switch (message.MessageType)
        {
            case MessageType.Bad:
                badMessageSound.PlaySoundAt(message.MessageCanvas.transform.position);
                break;
            case MessageType.Neutral:
                neutralMessageSound.PlaySoundAt(message.MessageCanvas.transform.position);
                break;
            case MessageType.Good:
                goodMessageSound.PlaySoundAt(message.MessageCanvas.transform.position);
                break;
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
                PlaySound(_messages[i]);
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
                    PlaySound(_messages[i]);
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
        for (var i = 0; i < _messages.Count; i++)
        {
            var message = _messages[i];
            if (message.MessageCanvas != null)
            {
                newMessages.Add(message);
                if (Time.time - message.TimeAdded > message.Duration && !message.MessageCanvas.IsDestructing())
                {
                    message.MessageCanvas.SelfDestruct();
                }
            }
            else if (i > 0)
            {
                // we don't want holes in the message stack when some messages delete in the middle
                for (var j = 0; j < i; j++)
                {
                    _messages[j].UnbumpMessage();
                }
            }
        }

        _messages = newMessages;
    }

    public void PruneMessageID(int id)
    {
        foreach (var message in _messages)
        {
            if (message.GetHashCode() == id && !message.MessageCanvas.IsDestructing())
            {
                message.MessageCanvas.SelfDestruct();
            }
        }
    }

    private class Message
    {
        public readonly MessageCanvas MessageCanvas;
        public readonly double TimeAdded;
        public readonly float Duration;
        public MessageType MessageType;
        public Vector3 TargetPosition;

        public Message(MessageCanvas messageCanvas, Transform firstLocation, MessageType messageType, float duration)
        {
            TimeAdded = Time.time;
            MessageCanvas = messageCanvas;
            TargetPosition = firstLocation.position;
            MessageType = messageType;
            Duration = duration;
        }

        public void BumpMessage()
        {
            TargetPosition.y += StepSize;
        }

        public void UnbumpMessage()
        {
            TargetPosition.y -= StepSize;
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