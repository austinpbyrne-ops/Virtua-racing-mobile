using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using VRacer.Core;

namespace VRacer.Audio
{
    /// <summary>
    /// Audio manager handling engine sounds, SFX, music, and arcade-style audio.
    /// Designed for late-80s/early-90s Sega arcade synth vibe.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup engineGroup;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float engineVolume = 0.7f;

        [Header("Engine Sounds")]
        [SerializeField] private AudioClip engineIdleClip;
        [SerializeField] private AudioClip engineLowClip;
        [SerializeField] private AudioClip engineMidClip;
        [SerializeField] private AudioClip engineHighClip;
        [SerializeField] private float enginePitchMin = 0.8f;
        [SerializeField] private float enginePitchMax = 2.5f;
        private AudioSource engineSource;

        [Header("SFX")]
        [SerializeField] private AudioClip collisionClip;
        [SerializeField] private AudioClip tireScreechClip;
        [SerializeField] private AudioClip checkpointClip;
        [SerializeField] private AudioClip countdownBeepClip;
        [SerializeField] private AudioClip countdownGoClip;
        [SerializeField] private AudioClip timerWarningClip;
        [SerializeField] private AudioClip menuSelectClip;
        [SerializeField] private AudioClip menuConfirmClip;
        [SerializeField] private AudioClip coinInsertClip;

        [Header("Music")]
        [SerializeField] private AudioClip bigForestMusic;
        [SerializeField] private AudioClip bayBridgeMusic;
        [SerializeField] private AudioClip acropolisMusic;
        [SerializeField] private AudioClip titleMusic;
        [SerializeField] private AudioClip resultsMusic;
        private AudioSource musicSource;

        // SFX pool
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private const int SFX_POOL_SIZE = 8;
        private int sfxPoolIndex = 0;

        // Music references
        private AudioClip currentTrackMusic;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            // Engine source
            engineSource = gameObject.AddComponent<AudioSource>();
            engineSource.loop = true;
            engineSource.playOnAwake = false;
            engineSource.outputAudioMixerGroup = engineGroup;
            engineSource.spatialBlend = 0f; // 2D sound

            // Music source
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.spatialBlend = 0f;

            // SFX pool
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = sfxGroup;
                source.spatialBlend = 0f;
                sfxPool.Add(source);
            }
        }

        private void Update()
        {
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (audioMixer == null) return;

            audioMixer.SetFloat("MasterVolume", LinearToDecibel(masterVolume));
            audioMixer.SetFloat("SFXVolume", LinearToDecibel(sfxVolume));
            audioMixer.SetFloat("MusicVolume", LinearToDecibel(musicVolume));
            audioMixer.SetFloat("EngineVolume", LinearToDecibel(engineVolume));
        }

        private float LinearToDecibel(float linear)
        {
            return linear > 0.001f ? 20f * Mathf.Log10(linear) : -80f;
        }

        // ============================================================
        // ENGINE SOUNDS
        // ============================================================

        public void UpdateEngineSound(float speed, float maxSpeed, float rpm)
        {
            if (engineSource == null) return;

            float speedRatio = Mathf.Clamp01(speed / maxSpeed);

            // Pitch-shift with speed
            float targetPitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedRatio);
            engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, 5f * Time.deltaTime);

            // Volume based on throttle
            engineSource.volume = engineVolume * (0.5f + 0.5f * speedRatio);

            // Ensure playing
            if (!engineSource.isPlaying && engineIdleClip != null)
            {
                engineSource.clip = engineIdleClip;
                engineSource.Play();
            }

            // Cross-fade between engine clips based on speed
            if (speedRatio < 0.25f && engineSource.clip != engineIdleClip && engineIdleClip != null)
            {
                engineSource.clip = engineIdleClip;
            }
            else if (speedRatio < 0.5f && engineSource.clip != engineLowClip && engineLowClip != null)
            {
                engineSource.clip = engineLowClip;
            }
            else if (speedRatio < 0.75f && engineSource.clip != engineMidClip && engineMidClip != null)
            {
                engineSource.clip = engineMidClip;
            }
            else if (speedRatio >= 0.75f && engineSource.clip != engineHighClip && engineHighClip != null)
            {
                engineSource.clip = engineHighClip;
            }
        }

        public void StopEngine()
        {
            if (engineSource != null)
            {
                engineSource.Stop();
            }
        }

        // ============================================================
        // SFX
        // ============================================================

        public void PlayCollision(float intensity = 0.5f)
        {
            PlaySFX(collisionClip, intensity * 0.8f + 0.2f);
        }

        public void PlayTireScreech(float intensity = 0.5f)
        {
            PlaySFX(tireScreechClip, intensity);
        }

        public void PlayCheckpoint()
        {
            PlaySFX(checkpointClip, 1f);
        }

        public void PlayCountdownBeep()
        {
            PlaySFX(countdownBeepClip, 1f);
        }

        public void PlayCountdownGo()
        {
            PlaySFX(countdownGoClip, 1f);
        }

        public void PlayTimerWarning()
        {
            PlaySFX(timerWarningClip, 0.8f);
        }

        public void PlayMenuSelect()
        {
            PlaySFX(menuSelectClip, 0.6f);
        }

        public void PlayMenuConfirm()
        {
            PlaySFX(menuConfirmClip, 0.7f);
        }

        public void PlayCoinInsert()
        {
            PlaySFX(coinInsertClip, 1f);
        }

        private void PlaySFX(AudioClip clip, float volume)
        {
            if (clip == null || sfxPool.Count == 0) return;

            AudioSource source = sfxPool[sfxPoolIndex];
            sfxPoolIndex = (sfxPoolIndex + 1) % SFX_POOL_SIZE;

            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.pitch = Random.Range(0.95f, 1.05f); // Slight variation
            source.Play();
        }

        // ============================================================
        // MUSIC
        // ============================================================

        public void PlayTitleMusic()
        {
            PlayMusic(titleMusic);
        }

        public void PlayTrackMusic(string trackName)
        {
            AudioClip music = trackName switch
            {
                "Big Forest" => bigForestMusic,
                "Bay Bridge" => bayBridgeMusic,
                "Acropolis" => acropolisMusic,
                _ => bigForestMusic
            };

            PlayMusic(music);
        }

        public void PlayResultsMusic()
        {
            PlayMusic(resultsMusic);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void FadeOutMusic(float duration = 1f)
        {
            StartCoroutine(FadeOutMusicCoroutine(duration));
        }

        private System.Collections.IEnumerator FadeOutMusicCoroutine(float duration)
        {
            if (musicSource == null) yield break;

            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = musicVolume;
        }

        // ============================================================
        // SETTINGS
        // ============================================================

        public void SetMasterVolume(float vol) => masterVolume = Mathf.Clamp01(vol);
        public void SetSFXVolume(float vol) => sfxVolume = Mathf.Clamp01(vol);
        public void SetMusicVolume(float vol) => musicVolume = Mathf.Clamp01(vol);
        public void SetEngineVolume(float vol) => engineVolume = Mathf.Clamp01(vol);
    }
}
