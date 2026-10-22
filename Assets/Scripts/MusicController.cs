using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class MusicController : MonoBehaviour
{
    public static MusicController Instance;

    [Header("Track")]
    public AudioClip musicTrack;
    public float originalBpm;
    public float firstBeatOffset = 0f;
    public bool playOnStart = true;
    public bool loop = true;

    [Header("Speed")]
    private float musicSpeed = 1f;
    public float minSpeed = 0.1f;
    public float maxSpeed = 3f;

    [Header("Events")]
    public UnityEvent<int> OnBeat;

    private AudioSource source;
    private Coroutine speedRoutine;
    private int lastBeatIndex = int.MinValue;

    public float SecondsPerBeat => 60f / originalBpm;

    public float SongPositionInBeats
    {
        get
        {
            if (source == null || source.clip == null)
                return 0f;
            float seconds = (float)source.timeSamples / source.clip.frequency;
            return (seconds - firstBeatOffset) / SecondsPerBeat;
        }
    }

    public int CurrentBeatIndex => Mathf.FloorToInt(SongPositionInBeats);

    private float ClipLengthInBeats =>
        source != null && source.clip != null ? source.clip.length / SecondsPerBeat : 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        if (musicTrack != null)
            source.clip = musicTrack;
        ApplyPitch();
    }

    private void Start()
    {
        if (playOnStart && source.clip != null)
            Play();
    }

    private void Update()
    {
        TrackBeats();
    }

    private void OnValidate()
    {
        if (maxSpeed < minSpeed)
            maxSpeed = minSpeed;
        musicSpeed = Mathf.Clamp(musicSpeed, minSpeed, maxSpeed);
        if (source != null)
        {
            source.loop = loop;
            ApplyPitch();
        }
    }

    public void Play()
    {
        if (source.clip == null)
            return;
        lastBeatIndex = int.MinValue;
        source.Play();
    }

    public void Pause() => source.Pause();

    public void Resume() => source.UnPause();

    public void Stop()
    {
        source.Stop();
        lastBeatIndex = int.MinValue;
    }

    /// <summary>Swap to a new track with its own BPM.</summary>
    public void SetTrack(
        AudioClip clip,
        float newBpm,
        float newFirstBeatOffset = 0f,
        bool playImmediately = true
    )
    {
        Stop();
        musicTrack = clip;
        originalBpm = Mathf.Max(1f, newBpm);
        firstBeatOffset = Mathf.Max(0f, newFirstBeatOffset);
        source.clip = clip;
        if (playImmediately)
            Play();
    }

    /// <summary>Instantly set the base music speed (1 = normal).</summary>
    public void SetSpeed(float speed)
    {
        if (speedRoutine != null)
        {
            StopCoroutine(speedRoutine);
            speedRoutine = null;
        }
        musicSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        ApplyPitch();
    }

    /// <summary>Smoothly change the base music speed over a duration in real seconds.</summary>
    public void SetSpeed(float speed, float duration)
    {
        if (duration <= 0f)
        {
            SetSpeed(speed);
            return;
        }
        if (speedRoutine != null)
            StopCoroutine(speedRoutine);
        speedRoutine = StartCoroutine(
            SpeedRoutine(Mathf.Clamp(speed, minSpeed, maxSpeed), duration)
        );
    }

    /// <summary>Add (or subtract) from the current base speed.</summary>
    public void ChangeSpeedBy(float delta) => SetSpeed(musicSpeed + delta);

    public void ResetSpeed() => SetSpeed(1f);

    private IEnumerator SpeedRoutine(float target, float duration)
    {
        float start = musicSpeed;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            musicSpeed = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
            ApplyPitch();
            yield return null;
        }
        musicSpeed = target;
        ApplyPitch();
        speedRoutine = null;
    }

    private void ApplyPitch()
    {
        if (source == null)
            return;
        source.pitch = Mathf.Clamp(musicSpeed, minSpeed, maxSpeed);
    }

    private void TrackBeats()
    {
        if (source == null || source.clip == null || !source.isPlaying)
            return;

        int beat = CurrentBeatIndex;
        if (beat < lastBeatIndex)
            lastBeatIndex = beat - 1; // track looped back to start
        if (beat != lastBeatIndex && beat >= 0)
        {
            lastBeatIndex = beat;
            OnBeat?.Invoke(beat);
        }
    }
}
