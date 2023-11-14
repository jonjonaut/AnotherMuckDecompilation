using UnityEngine;

public class MusicController : MonoBehaviour
{
    private void Awake()
    {
        MusicController.Instance = this;
        this.audio = base.GetComponent<AudioSource>();
        this.audioSource = base.GetComponent<AudioSource>();
    }

    private void Start()
    {
        MusicController.Instance.audio.volume = SaveManager.Instance.state.music / 10f;
		this.targetVolume = CurrentSettings.Instance.music;
    }

    public void SetVolume(float f)
    {
        this.targetVolume = f;
        this.StartFade(this.audio, 0.1f, f);
    }

    private void Update()
    {
        this.currentTime += Time.deltaTime;
        this.audioSource.volume = this.audio.volume;

    }

    public void PlaySong(MusicController.SongType s, bool chanceToSkip = true)
    {
		if (GameManager.instance && GameManager.instance.boatLeft)
		{
			return;
		}
        AudioClip audioClip = null;
        if (this.currentSong == MusicController.SongType.Boss && BossUI.Instance.currentBoss != null)
        {
            return;
        }
        this.currentSong = s;
        switch (s)
        {
            case SongType.Day:
                if (!chanceToSkip || Random.Range(0f, 1f) <= 0.5f)
                {
                    audioClip = this.day[Random.Range(0, this.day.Length)];
                }
                break;
            case SongType.Night:
                audioClip = this.night[Random.Range(0, this.night.Length)];
                break;
            case SongType.Boss:
                audioClip = this.boss[Random.Range(0, this.boss.Length)];
                break;
        }
        if (audioClip == null)
        {
            this.StartFade(this.audio, this.fadeTime, 0f);
            return;
        }
        if (this.audio.isPlaying)
        {
            this.queuedSong = audioClip;
            this.StartFade(this.audio, this.fadeTime, 0f);
            Invoke(nameof(NextSong), this.fadeTime);
            return;
        }
        this.NextSong(audioClip);
    }

    private void NextSong()
    {
        this.StartFade(this.audio, this.fadeTime, this.targetVolume);
        this.audio.clip = this.queuedSong;
        this.audio.Play();
    }

    private void NextSong(AudioClip song)
    {
        this.StartFade(this.audio, this.fadeTime, this.targetVolume);
        this.audio.clip = song;
        this.audio.Play();
    }

	public void StopSong(float fade = -1f)
    {
		float duration = this.fadeTime;
		if (fade >= 0f)
		{
			duration = fade;
		}
		this.StartFade(this.audio, duration, 0f);
        currentSong = default;
    }

    private void StartFade(AudioSource audioSource, float duration, float targetVolume)
    {
        this.currentTime = 0f;
        this.newFadeTime = duration;
        this.desiredVolume = targetVolume;
        this.startVolume = audioSource.volume;
    }

    public AudioClip[] day;

    public AudioClip[] night;

    public AudioClip[] boss;

    private AudioSource audio;

    public static MusicController Instance;

    private AudioClip queuedSong;

    private float fadeTime = 6f;

    private float targetVolume = 0.2f;

    private MusicController.SongType currentSong;

    private float currentTime;

    private float newFadeTime;

    [System.NonSerialized] public AudioSource audioSource;

    private float desiredVolume;

    private float startVolume;

    public enum SongType
    {
        Day,
        Night,
        Boss
        
    }
}
