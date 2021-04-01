using System;
using System.ComponentModel;
using UnityEngine;
using VivoxUnity;

public class VoiceManager : MonoBehaviour
{
    public delegate void LoginStatusChangedHandler();

    public delegate void ParticipantStatusChangedHandler(string username, ChannelId channel, IParticipant participant);

    public delegate void ParticipantValueChangedHandler(string username, ChannelId channel, bool value);

    public delegate void ParticipantValueUpdatedHandler(string username, ChannelId channel, double value);

    [SerializeField] private string _server = "https://GETFROMPORTAL.www.vivox.com/api2";
    [SerializeField] private string _domain = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
    [SerializeField] private string _tokenIssuer = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
    [SerializeField] private string _tokenKey = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);
    private AccountId _accountId;
    private Client _client = new Client();

    private ILoginSession LoginSession;
    private IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannels => LoginSession?.ChannelSessions;
    private LoginState LoginState { get; set; }

    private Uri _serverUri
    {
        get => new Uri(_server);

        set => _server = value.ToString();
    }

    // Start is called before the first frame update
    private void Start()
    {
        _client.Uninitialize();
        _client.Initialize();
    }

    // Update is called once per frame
    private void Update() { }

    public void OnApplicationQuit()
    {
        if (_client != null)
        {
            BonsaiLog("Uninitializing Vivox client.");
            _client.Uninitialize();
            _client = null;
        }
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

    private void LoginVoice(string displayName)
    {
        var uniqueId = Guid.NewGuid().ToString();
        _accountId = new AccountId(_tokenIssuer, uniqueId, _domain, displayName);
        LoginSession = _client.GetLoginSession(_accountId);
        LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;
        LoginSession.BeginLogin(_serverUri, LoginSession.GetLoginToken(_tokenKey, _tokenExpiration),
                                SubscriptionMode.Accept, null, null, null,
                                ar =>
                                {
                                    try
                                    {
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

    private void LogoutVoice()
    {
        if (LoginSession != null && LoginState != LoginState.LoggedOut && LoginState != LoginState.LoggingOut)
        {
            OnUserLoggedOutEvent?.Invoke();
            LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
            LoginSession.Logout();
        }
    }

    public void DisconnectAllChannels()
    {
        if (ActiveChannels?.Count > 0)
        {
            foreach (var channelSession in ActiveChannels)
            {
                channelSession?.Disconnect();
            }
        }
    }

    private void JoinVoiceChannel(string channelName, ChannelType channelType, bool switchTransmission = true,
                                  Channel3DProperties properties = null)
    {
        if (LoginState == LoginState.LoggedIn)
        {
            var channelId = new ChannelId(_tokenIssuer, channelName, _domain, channelType, properties);
            var channelSession = LoginSession.GetChannelSession(channelId);
            channelSession.PropertyChanged += OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
            channelSession.BeginConnect(true, false, switchTransmission,
                                        channelSession.GetConnectToken(_tokenKey, _tokenExpiration),
                                        ar =>
                                        {
                                            try
                                            {
                                                channelSession.EndConnect(ar);
                                            }
                                            catch (Exception e)
                                            {
                                                // todo handle error
                                                BonsaiLogError($"Could not connect to voice channel: {e.Message}");
                                            }
                                        });
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
                BonsaiLog("Connected to voice server and logged in.");
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
        Debug.Log("<color=orange>BonsaiComms: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiComms: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiComms: </color>: " + msg);
    }
}