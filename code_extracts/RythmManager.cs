using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using AK.Wwise;
using Unity.VisualScripting;


/** WWISE CALLBACKS

AK_MusicSyncBeat                = 0x0100,   // Enable notifications on Music Beat.
AK_MusicSyncBar                 = 0x0200,   // Enable notifications on Music Bar.
AK_MusicSyncEntry               = 0x0400,   // Enable notifications on Music Entry Point.
AK_MusicSyncExit                = 0x0800,   // Enable notifications on Music Exit Point.
AK_MusicSyncGrid                = 0x1000,   // Enable notifications on Music Grid.
AK_MusicSyncUserCue             = 0x2000,   // Enable notifications on Music User Cue.
AK_MusicSyncPoint               = 0x4000,   // Enable notifications on Music synchronisation point.
AK_MusicSyncAll                 = 0xff00,   // Use this flag if you want to receive all notifications concerning AK_MusicSync registration. 

/// Callback information structure corresponding to Ak_MusicSync
struct AkMusicSyncCallbackInfo : public AkCallbackInfo
{
    AkPlayingID playingID;          ///< Playing ID of Event, returned by PostEvent()
    AkCallbackType musicSyncType;   ///< Would be either AK_MusicSyncEntry, AK_MusicSyncBeat, AK_MusicSyncBar, AK_MusicSyncExit, AK_MusicSyncGrid, AK_MusicSyncPoint or AK_MusicSyncUserCue.
    AkReal32 fBeatDuration;         ///< Beat Duration in seconds.
    AkReal32 fBarDuration;          ///< Bar Duration in seconds.
    AkReal32 fGridDuration;         ///< Grid duration in seconds.
    AkReal32 fGridOffset;           ///< Grid offset in seconds.
};

AK_MusicPlaylistSelect          = 0x0040    // Callback triggered when music playlist container must select the next item to play.

Documentation : https://www.audiokinetic.com/fr/library/edge/?source=SDK&id=soundengine_music_callbacks.html&fbclid=IwAR1rwfNKiJYsa3UPjopfYoCoYxf1lJ_yb5uXiC_vyaYqLSs7n88Y738s7bA

**/

public class RythmManager : MonoBehaviour
{

    /* WWISE VARIABLES */


    public AK.Wwise.Event musicPlayer;
    public AK.Wwise.Event chordProgression;
    public AK.Wwise.Event stopMusic;

    //Song beats per minute : This is determined by the song you're trying to sync up to
    public float songBpm;
    //The offset to the first beat of the song in seconds
    [SerializeField] private float _firstBeatOffset = 0.0f;
    //the number of beats in each loop
    public float beatsPerLoop;

    /* RYTHM MANAGER VARIABLES */


    //Position in the current song in seconds
    private float _songPosition;
    //If true then we can start our game logic
    [HideInEditorMode] public bool musicStarted;
    //The number of seconds for each song beat
    public float secPerBeat;
    //Current song position, in beats
    public float songPositionInBeats;
    //Calculate the offset of when to play the animation
    public float animationOffset;

    //Instance
    public static RythmManager instance;

    /* INPUT IN RYTHM */

    [SerializeField] private Material _materialRenderer;
    //Timing window before the beat arrives
    [SerializeField] public float timingWindowBefore = 0.0f;
    //Timing window after the beat arrives
    [SerializeField] private float timingWindowAfter = 0.3f;
    //Tells to the player and the enemy if their input is in rythm
    private bool _inputInRythm;
    public bool inputInRythm { get { return _inputInRythm;}}
    //Variable used to preshot when the next beat is going to drop
    private float _nextBeatTime;

    //Variable used to preshot the cooldown for everything.
    private float _nextCooldownBeforeOffset = 0.0f;

    /* BEAT DELEGATES */

    public delegate void BeatDelegate();
    public event BeatDelegate BeatEvent;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        instance = this;

        AkSoundEngine.RegisterGameObj(gameObject);

        stopMusic.Post(gameObject);
        musicPlayer.Post(gameObject, (uint)(AkCallbackType.AK_MusicSyncBar | AkCallbackType.AK_MusicSyncBeat | AkCallbackType.AK_MusicSyncEntry), OnMusicCallback);
        chordProgression.Post(gameObject);
    }

    void Update()
    {
        if (!musicStarted) return;

        //determine how many seconds since the song started
        _songPosition += Time.smoothDeltaTime;

        // Check if it's time for the next beat
        if (_songPosition >= _nextBeatTime + timingWindowAfter)
        {
            // Update _nextBeatTime for the next beat
            
            _nextBeatTime += secPerBeat;
            _nextCooldownBeforeOffset = _nextBeatTime + timingWindowBefore;
        } 

        //determine how many beats since the song started
        songPositionInBeats = _songPosition / secPerBeat;

        if (_songPosition >= _nextBeatTime + timingWindowBefore && _songPosition <= _nextBeatTime + timingWindowAfter)
        {
            _inputInRythm = true;

            CooldownTimingOffset();
            HoldTimingCalibration();
        }
        else
        {
            _inputInRythm = false;
        }

        animationOffset = _songPosition / secPerBeat;
    }

    public float CooldownTimingOffset()
    {
        float cooldownOffset = _songPosition - _nextCooldownBeforeOffset;

        return cooldownOffset;
    }

    public bool StartedInputAfterBeat()
    {
        if (_songPosition >= _nextBeatTime + 0.1) return true;
        return false;
    }

    public float HoldTimingCalibration()
    {
        float cooldownOffset = _nextBeatTime - _songPosition;

        return cooldownOffset;
    }

    void OnMusicCallback(object in_cookie, AkCallbackType in_type, object in_info)
    {
        //Barre
        if (in_type == AkCallbackType.AK_MusicSyncBeat)
        {
            if (BeatEvent != null)
            {
                BeatEvent();
            }
        }

        //If the music just started we initialize all of our infos and our BPM
        if (in_type == AkCallbackType.AK_MusicSyncEntry && musicStarted == false)
        {
            AkMusicSyncCallbackInfo info = (AkMusicSyncCallbackInfo)in_info;
            songBpm = 60f/info.segmentInfo_fBeatDuration;
            secPerBeat = 60f / songBpm;

            _nextBeatTime = 0.0f;
            _songPosition = 0.0f;
            musicStarted = true;
        }
    }
}