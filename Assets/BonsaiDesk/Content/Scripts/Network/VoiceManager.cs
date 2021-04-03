using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;
using VivoxUnity;

public class VoiceManager : MonoBehaviour
{
    public delegate void LoginStatusChangedHandler();

    public delegate void ParticipantStatusChangedHandler(string username, ChannelId channel, IParticipant participant);

    public delegate void ParticipantValueChangedHandler(string username, ChannelId channel, bool value);

    public delegate void ParticipantValueUpdatedHandler(string username, ChannelId channel, double value);

    public static VoiceManager Singleton;

    [FormerlySerializedAs("headPosition")] public Transform headTransform;

    [SerializeField] private string _server = "https://GETFROMPORTAL.www.vivox.com/api2";
    [SerializeField] private string _domain = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
    [SerializeField] private string _tokenIssuer = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
    [SerializeField] private string _tokenKey = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);
    private AccountId _accountId;
    private Client _client = new Client();
    private bool _hasFocus = true;
    private Coroutine _joinChannelRoutine;

    private float _nextPosUpdate;
    private IChannelSession _positionalChannelSession;

    private ILoginSession LoginSession;
    private IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannels => LoginSession?.ChannelSessions;
    public LoginState LoginState { get; private set; }

    private Uri _serverUri
    {
        get => new Uri(_server);

        set => _server = value.ToString();
    }

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (!_client.Initialized)
        {
            _client.Uninitialize();
            _client.Initialize();
            Login("displayName");
        }
    }

    // Update is called once per frame
    private void Update()
    {
        HandleMuteUpdate();

        if (Time.realtimeSinceStartup > _nextPosUpdate)
        {
            _nextPosUpdate += 0.3f;
            if (!(_positionalChannelSession is null) && _positionalChannelSession.AudioState == ConnectionState.Connected)
            {
                var position = headTransform.position;
                var forward = headTransform.forward;
                var up = headTransform.up;
                _positionalChannelSession?.Set3DPosition(position, position, forward, up);
            }
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        _hasFocus = focus;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        BonsaiLog($"OnApplicationPause {pauseStatus}");
        if (!pauseStatus)
        {
            BonsaiLog("Unpause");
            _client.Uninitialize();
            _client = new Client();
            _client.Initialize();
            Login("displayName");
        }
        else
        {
            BonsaiLog("Pause");
            if (LoginState == LoginState.LoggedIn)
            {
                DisconnectAllChannels();
            }

            Logout();
            LoginState = LoginState.LoggedOut;
        }
    }

    public void OnApplicationQuit()
    {
        if (_client != null)
        {
            BonsaiLog("Uninitializing Vivox client.");
            _client.Uninitialize();
            _client = null;
        }
    }

    public void DisconnectAllChannels()
    {
        if (_client is null)
        {
            BonsaiLog("Client is null so no attempt to disconnect from channels will be made");
            return;
        }

        BonsaiLog("Disconnect all channels");
        // stop any routines to join a channel
        if (!(_joinChannelRoutine is null))
        {
            BonsaiLog("Stopping join channel coroutine in progress");
            StopCoroutine(_joinChannelRoutine);
            _joinChannelRoutine = null;
        }

        if (!(_positionalChannelSession is null))
        {
            BonsaiLog($"Removing positional channel handler for ({_positionalChannelSession.Channel.Name})");
            _positionalChannelSession = null;
        }

        if (ActiveChannels?.Count > 0)
        {
            foreach (var channelSession in ActiveChannels)
            {
                BonsaiLog($"Disconnect ({channelSession.Channel.Name})");
                channelSession.Disconnect();
            }
        }
    }

    private void DisconnectFromChannel(string oldName)
    {
        if (oldName != "") // time to disconnect from old channel
        {
            // stop any routines to join a channel
            if (!(_joinChannelRoutine is null))
            {
                BonsaiLog("Stopping join channel coroutine in progress");
                StopCoroutine(_joinChannelRoutine);
                _joinChannelRoutine = null;
            }

            // disconnect from oldName channel
            foreach (var channelSession in ActiveChannels)
            {
                if (channelSession.Channel.Name == oldName)
                {
                    if (_positionalChannelSession.Channel.Name == channelSession.Channel.Name)
                    {
                        BonsaiLog($"Removing positional channel handler for ({_positionalChannelSession.Channel.Name})");
                        _positionalChannelSession = null;
                    }

                    BonsaiLog($"Disconnecting from ({oldName})");
                    channelSession?.Disconnect();
                }
            }
        }
    }

    public void StartJoinChannel(string newName)
    {
        if (newName != "") // time to join a new voice channel
        {
            if (!(_joinChannelRoutine is null))
            {
                BonsaiLog("Stopping join channel coroutine before joining a new channel");
                StopCoroutine(_joinChannelRoutine);
            }

            _joinChannelRoutine = StartCoroutine(JoinChannelWhenReady(newName));
        }
    }

    private IEnumerator JoinChannelWhenReady(string lobbyName)
    {
        // todo start logging in if not in the process now 

        while (LoginState != LoginState.LoggedIn)
        {
            BonsaiLog($"Wait before voice join voice channel ({LoginState})");
            yield return new WaitForSeconds(0.25f);
        }

        _joinChannelRoutine = null;
        JoinChannel(lobbyName, ChannelType.Positional, properties: new Channel3DProperties());
    }

    public event LoginStatusChangedHandler OnUserLoggedOutEvent;
    public event ParticipantValueUpdatedHandler OnAudioEnergyChangedEvent;
    public event ParticipantStatusChangedHandler OnParticipantAddedEvent;
    public event ParticipantStatusChangedHandler OnParticipantRemovedEvent;
    public event ParticipantValueChangedHandler OnSpeechDetectedEvent;
    public event LoginStatusChangedHandler OnUserLoggedInEvent;

    private void OnParticipantRemoved(object sender, KeyEventArg<string> e)
    {
        ValidateArgs(new[] {sender, e});

        // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
        var source = (IReadOnlyDictionary<string, IParticipant>) sender;
        // Look up the participant via the key.
        var participant = source[e.Key];
        var username = participant.Account.Name;
        var channel = participant.ParentChannelSession.Key;
        var channelSession = participant.ParentChannelSession;

        if (participant.IsSelf)
        {
            BonsaiLog($"Unsubscribing from: {channelSession.Key.Name}");

            // Now that we are disconnected, unsubscribe.
            channelSession.PropertyChanged -= OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;

            var user = _client.GetLoginSession(_accountId);
            user.DeleteChannelSession(channelSession.Channel);
        }

        OnParticipantRemovedEvent?.Invoke(username, channel, participant);
    }

    private void OnParticipantAdded(object sender, KeyEventArg<string> e)
    {
        ValidateArgs(new[] {sender, e});
        // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
        var source = (IReadOnlyDictionary<string, IParticipant>) sender;
        // Look up the participant via the key.
        var participant = source[e.Key];
        var username = participant.Account.Name;
        var channel = participant.ParentChannelSession.Key;
        var channelSession = participant.ParentChannelSession;

        OnParticipantAddedEvent?.Invoke(username, channel, participant);
    }

    private void Login(string displayName)
    {
        BonsaiLog("Begin login");
        var uniqueId = Guid.NewGuid().ToString();
        _accountId = new AccountId(_tokenIssuer, uniqueId, _domain, displayName);
        LoginSession = _client.GetLoginSession(_accountId);
        LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;
        LoginSession.BeginLogin(_serverUri, LoginSession.GetLoginToken(_tokenKey, _tokenExpiration), SubscriptionMode.Accept, null, null, null, ar =>
        {
            try
            {
                BonsaiLog("End login");
                LoginSession.EndLogin(ar);
            }
            catch (Exception e)
            {
                // todo handle error
                BonsaiLogError(nameof(e));
                LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
            }
        });
    }

    private void Logout()
    {
        BonsaiLog("Maybe Logout");
        if (LoginSession != null && LoginState != LoginState.LoggedOut && LoginState != LoginState.LoggingOut)
        {
            BonsaiLog("Logout");
            OnUserLoggedOutEvent?.Invoke();
            LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
            LoginSession.Logout();
        }
    }

    private void JoinChannel(string channelName, ChannelType channelType, bool switchTransmission = true, Channel3DProperties properties = null)
    {
        BonsaiLog($"JoinChannel: {channelName}");

        if (LoginState == LoginState.LoggedIn)
        {
            var channelId = new ChannelId(_tokenIssuer, channelName, _domain, channelType, properties);
            var channelSession = LoginSession.GetChannelSession(channelId);
            channelSession.PropertyChanged += OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
            channelSession.BeginConnect(true, false, switchTransmission, channelSession.GetConnectToken(_tokenKey, _tokenExpiration), ar =>
            {
                try
                {
                    // todo this is not getting called on android
                    BonsaiLog("End JoinChannel");
                    channelSession.EndConnect(ar);
                    if (!(properties is null))
                    {
                        _positionalChannelSession = channelSession;
                    }
                }
                catch (Exception e)
                {
                    // todo handle error
                    BonsaiLogError($"Could not connect to voice channel: {e.Message}");
                }
            });
        }
        else
        {
            // todo handle this
            BonsaiLogWarning("Tried to join a channel when not logged in");
        }
    }

    private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        ValidateArgs(new[] {sender, e});

        var channelSession = (IChannelSession) sender;

        // IF the channel has removed audio, make sure all the VAD indicators aren't showing speaking.
        if (e.PropertyName == "AudioState" && channelSession.AudioState == ConnectionState.Disconnected)
        {
            BonsaiLog($"Audio disconnected from: {channelSession.Key.Name}");

            foreach (var participant in channelSession.Participants)
            {
                OnSpeechDetectedEvent?.Invoke(participant.Account.Name, channelSession.Channel, false);
            }
        }

        // IF the channel has fully disconnected, unsubscribe and remove.
        if (e.PropertyName == "AudioState" && channelSession.AudioState == ConnectionState.Disconnected)
        {
            BonsaiLog($"Unsubscribing from: {channelSession.Key.Name}");

            channelSession.PropertyChanged -= OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;

            var user = _client.GetLoginSession(_accountId);
            user.DeleteChannelSession(channelSession.Channel);
        }
    }

    private void OnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "State")
        {
            return;
        }

        var loginSession = (ILoginSession) sender;
        LoginState = loginSession.State;
        BonsaiLog("Detecting login session change");
        switch (LoginState)
        {
            case LoginState.LoggingIn:
            {
                BonsaiLog("Logging in");
                break;
            }
            case LoginState.LoggedIn:
            {
                OnUserLoggedInEvent?.Invoke();
                break;
            }
            case LoginState.LoggingOut:
            {
                BonsaiLog("Logging out");
                break;
            }
            case LoginState.LoggedOut:
            {
                BonsaiLog("Logged out");
                LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                break;
            }
        }
    }

    private static void ValidateArgs(object[] objs)
    {
        foreach (var obj in objs)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
            }
        }
    }

    private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> e)
    {
        ValidateArgs(new[] {sender, e});

        var source = (IReadOnlyDictionary<string, IParticipant>) sender;
        // Look up the participant via the key.
        var participant = source[e.Key];

        var username = e.Value.Account.Name;
        var channel = e.Value.ParentChannelSession.Key;
        var property = e.PropertyName;

        switch (property)
        {
            case "SpeechDetected":
            {
                BonsaiLog($"OnSpeechDetectedEvent: {username} in {channel}.");
                OnSpeechDetectedEvent?.Invoke(username, channel, e.Value.SpeechDetected);
                break;
            }
            case "AudioEnergy":
                OnAudioEnergyChangedEvent?.Invoke(username, channel, e.Value.AudioEnergy);
                break;
        }
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=green>BonsaiVoice: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=green>BonsaiVoice: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=green>BonsaiVoice: </color>: " + msg);
    }

    private void LogMicrophoneDevices()
    {
        BonsaiLog("Connected to voice server and logged in.");
        BonsaiLog($"SystemDevice: {_client.AudioInputDevices.SystemDevice.Name}");
        BonsaiLog($"ActiveDevice: {_client.AudioInputDevices.ActiveDevice.Name}");
        BonsaiLog($"EffectiveDevice: {_client.AudioInputDevices.EffectiveDevice.Name}");
        BonsaiLog($"Logging ({Microphone.devices.Length}) Microphone.devices now...");
        foreach (var device in Microphone.devices)
        {
            BonsaiLog($"Microphone Name: {device}");
        }

        BonsaiLog($"Logging ({_client.AudioInputDevices.AvailableDevices.Count}) Vivox client available devices now...");
        var devices = _client.AudioInputDevices.AvailableDevices;
        foreach (var entry in devices)
        {
            BonsaiLog($"Device {entry.Key} {devices[entry.Key].Name}");
        }
    }

    private void HandleMuteUpdate()
    {
        var oriented = MoveToDesk.Singleton.oriented;
        var outputActive = !_client.AudioOutputDevices.Muted;
        var inputActive = !_client.AudioInputDevices.Muted;

        if (_hasFocus && oriented)
        {
            if (!outputActive)
            {
                _client.AudioOutputDevices.Muted = false;
            }

            if (!inputActive)
            {
                _client.AudioInputDevices.Muted = false;
            }
        }

        if (!_hasFocus || !oriented)
        {
            if (outputActive)
            {
                _client.AudioOutputDevices.Muted = true;
            }

            if (inputActive)
            {
                _client.AudioInputDevices.Muted = true;
            }
        }
    }
}