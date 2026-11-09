using System;
using UnityEngine;

namespace Zulvyre
{
    public class AudioBank : MonoBehaviour
    {
        const int SampleRate = 44100;

        AudioSource sfx;
        AudioSource music;
        AudioSource ambience;

        AudioClip click;
        AudioClip swing;
        AudioClip heavySwing;
        AudioClip hit;
        AudioClip heavyHit;
        AudioClip dodge;
        AudioClip step;
        AudioClip heal;
        AudioClip shield;
        AudioClip bolt;
        AudioClip victory;
        AudioClip defeat;
        AudioClip menuMusic;
        AudioClip duelMusic;
        AudioClip wind;

        float nextStepTime;
        public bool Muted { get; private set; }

        public void Initialize()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            sfx.volume = 0.85f;

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.volume = 0.52f;

            ambience = gameObject.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.loop = true;
            ambience.spatialBlend = 0f;
            ambience.volume = 0.16f;

            BuildClips();
            ambience.clip = wind;
            ambience.Play();
            PlayMenuMusic();
        }

        void BuildClips()
        {
            click = MakeSfx("click", 0.09f, (t, d, rng) =>
                Mathf.Sin(2f * Mathf.PI * (940f - 300f * (t / d)) * t) * Mathf.Exp(-t * 48f) * 0.42f);

            swing = MakeSfx("swing", 0.23f, (t, d, rng) =>
            {
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / d));
                env *= env;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                return (noise * 0.42f + Mathf.Sin(2f * Mathf.PI * (260f + 1300f * (t / d)) * t) * 0.08f) * env;
            });

            heavySwing = MakeSfx("heavy-swing", 0.36f, (t, d, rng) =>
            {
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / d));
                env *= env;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                return (noise * 0.5f + Mathf.Sin(2f * Mathf.PI * (150f + 650f * (t / d)) * t) * 0.12f) * env;
            });

            hit = MakeSfx("hit", 0.2f, (t, d, rng) =>
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float thump = Mathf.Sin(2f * Mathf.PI * 92f * t) * Mathf.Exp(-t * 26f);
                return thump * 0.6f + noise * Mathf.Exp(-t * 55f) * 0.38f;
            });

            heavyHit = MakeSfx("heavy-hit", 0.32f, (t, d, rng) =>
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float thump = Mathf.Sin(2f * Mathf.PI * (58f - 18f * (t / d)) * t) * Mathf.Exp(-t * 11f);
                return thump * 0.72f + noise * Mathf.Exp(-t * 30f) * 0.34f;
            });

            dodge = MakeSfx("dodge", 0.22f, (t, d, rng) =>
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / d));
                return (noise * 0.34f + Mathf.Sin(2f * Mathf.PI * (240f + 1800f * (t / d)) * t) * 0.1f) * env;
            });

            step = MakeSfx("step", 0.08f, (t, d, rng) =>
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                return noise * Mathf.Exp(-t * 52f) * 0.22f + Mathf.Sin(2f * Mathf.PI * 84f * t) * Mathf.Exp(-t * 38f) * 0.12f;
            });

            heal = MakeSfx("heal", 0.8f, (t, d, rng) =>
            {
                float a = Mathf.Sin(2f * Mathf.PI * 523.25f * t) * Bell(t, 0f);
                float b = Mathf.Sin(2f * Mathf.PI * 659.25f * t) * Bell(t, 0.11f);
                float c = Mathf.Sin(2f * Mathf.PI * 783.99f * t) * Bell(t, 0.22f);
                return (a + b + c) * 0.16f;
            });

            shield = MakeSfx("shield", 0.65f, (t, d, rng) =>
            {
                float env = Mathf.Exp(-t * 5.5f);
                float ring = Mathf.Sin(2f * Mathf.PI * 392f * t) * 0.24f + Mathf.Sin(2f * Mathf.PI * 587f * t) * 0.16f;
                return ring * env;
            });

            bolt = MakeSfx("bolt", 0.45f, (t, d, rng) =>
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float env = Mathf.Exp(-t * 7f);
                return (noise * 0.22f + Mathf.Sin(2f * Mathf.PI * (700f - 520f * (t / d)) * t) * 0.24f) * env;
            });

            victory = MakeArpeggio("victory", new[] { 293.66f, 349.23f, 440f, 587.33f }, 0.16f, 1.5f, 0.22f);
            defeat = MakeArpeggio("defeat", new[] { 293.66f, 233.08f, 174.61f, 146.83f }, 0.22f, 1.7f, 0.2f);

            menuMusic = MakeMusic(false);
            duelMusic = MakeMusic(true);
            wind = MakeWind();
        }

        static float Bell(float t, float delay)
        {
            float local = t - delay;
            if (local < 0f) return 0f;
            return Mathf.Clamp01(local / 0.01f) * Mathf.Exp(-local * 3.4f);
        }

        static AudioClip MakeSfx(string name, float duration, Func<float, float, System.Random, float> generator)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[count];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(generator(t, duration, rng), -1f, 1f);
            }
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeArpeggio(string name, float[] notes, float spacing, float total, float amp)
        {
            int count = Mathf.CeilToInt(total * SampleRate);
            float[] data = new float[count];
            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.RoundToInt(n * spacing * SampleRate);
                int length = count - start;
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)SampleRate;
                    float env = Mathf.Clamp01(t / 0.008f) * Mathf.Exp(-t * 2.6f);
                    float wave = Mathf.Sin(2f * Mathf.PI * notes[n] * t);
                    wave += Mathf.Sin(4f * Mathf.PI * notes[n] * t) * 0.35f;
                    data[start + i] += wave * env * amp;
                }
            }
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeMusic(bool combat)
        {
            float bpm = combat ? 132f : 76f;
            float beat = 60f / bpm;
            const int bars = 8;
            int count = Mathf.CeilToInt(beat * 4f * bars * SampleRate);
            float[] data = new float[count];

            int[][] chords = combat
                ? new[] { new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 } }
                : new[] { new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 } };

            float barLength = beat * 4f;
            int[] arpPattern = { 0, 1, 2, 1, 2, 0, 1, 2 };
            var rng = new System.Random(combat ? 271 : 913);

            for (int bar = 0; bar < bars; bar++)
            {
                int[] chord = chords[(bar / 2) % chords.Length];
                float barStart = bar * barLength;

                AddNote(data, barStart, 1.6f, Midi(chord[0] - 12), combat ? 0.05f : 0.035f, false);
                AddNote(data, barStart, 2.6f, Midi(chord[0] - 5), combat ? 0.032f : 0.024f, false);

                for (int step = 0; step < 8; step++)
                {
                    float noteStart = barStart + step * (beat * 0.5f);
                    int note = chord[arpPattern[step]] + (step >= 4 ? 12 : 0);
                    float amp = combat ? 0.052f : 0.06f;
                    AddPluck(data, noteStart, 0.55f, Midi(note), amp);
                }

                if (!combat && bar % 2 == 0)
                {
                    int melody = chord[(bar / 2) % chord.Length] + 12;
                    AddPluck(data, barStart + beat, 1.2f, Midi(melody), 0.055f);
                    AddPluck(data, barStart + beat * 3f, 1.4f, Midi(melody + 3), 0.04f);
                }

                if (combat)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        float at = barStart + b * beat;
                        AddKick(data, at, 0.5f);
                        if (b == 1 || b == 3) AddSnare(data, at, 0.28f, rng);
                        AddHat(data, at, 0.12f, rng);
                        AddHat(data, at + beat * 0.5f, 0.07f, rng);
                    }
                    for (int s = 0; s < 8; s++)
                    {
                        AddNote(data, barStart + s * (beat * 0.5f), 0.3f, Midi(chord[0] - 12), 0.045f, false);
                    }
                }
            }

            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(data[i], -0.92f, 0.92f);
            FadeEdges(data, SampleRate / 40);
            var clip = AudioClip.Create(combat ? "duel-music" : "village-music", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void AddPluck(float[] buffer, float startSeconds, float duration, float frequency, float amplitude)
        {
            int start = Mathf.RoundToInt(startSeconds * SampleRate);
            int length = Mathf.RoundToInt(duration * SampleRate);
            for (int i = 0; i < length; i++)
            {
                int index = start + i;
                if (index < 0 || index >= buffer.Length) continue;
                float t = i / (float)SampleRate;
                float env = Mathf.Clamp01(t / 0.006f) * Mathf.Exp(-t * 3.4f);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
                wave += Mathf.Sin(4f * Mathf.PI * frequency * t + 0.4f) * 0.4f;
                wave += Mathf.Sin(6f * Mathf.PI * frequency * t + 0.8f) * 0.16f;
                buffer[index] += wave * env * amplitude;
            }
        }

        static void AddNote(float[] buffer, float startSeconds, float duration, float frequency, float amplitude, bool pluck)
        {
            int start = Mathf.RoundToInt(startSeconds * SampleRate);
            int length = Mathf.RoundToInt(duration * SampleRate);
            for (int i = 0; i < length; i++)
            {
                int index = start + i;
                if (index < 0 || index >= buffer.Length) continue;
                float t = i / (float)SampleRate;
                float env = pluck
                    ? Mathf.Exp(-t * 3f)
                    : Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((duration - t) / 0.9f);
                buffer[index] += Mathf.Sin(2f * Mathf.PI * frequency * t) * env * amplitude;
            }
        }

        static void AddKick(float[] buffer, float startSeconds, float amplitude)
        {
            int start = Mathf.RoundToInt(startSeconds * SampleRate);
            int length = Mathf.RoundToInt(0.3f * SampleRate);
            for (int i = 0; i < length; i++)
            {
                int index = start + i;
                if (index < 0 || index >= buffer.Length) continue;
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 13f);
                float freq = 110f - 65f * Mathf.Clamp01(t / 0.15f);
                buffer[index] += Mathf.Sin(2f * Mathf.PI * freq * t) * env * amplitude;
            }
        }

        static void AddSnare(float[] buffer, float startSeconds, float amplitude, System.Random rng)
        {
            int start = Mathf.RoundToInt(startSeconds * SampleRate);
            int length = Mathf.RoundToInt(0.24f * SampleRate);
            for (int i = 0; i < length; i++)
            {
                int index = start + i;
                if (index < 0 || index >= buffer.Length) continue;
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                buffer[index] += (noise * 0.17f + Mathf.Sin(2f * Mathf.PI * 185f * t) * 0.08f) * Mathf.Exp(-t * 18f) * (amplitude / 0.28f);
            }
        }

        static void AddHat(float[] buffer, float startSeconds, float amplitude, System.Random rng)
        {
            int start = Mathf.RoundToInt(startSeconds * SampleRate);
            int length = Mathf.RoundToInt(0.07f * SampleRate);
            float previous = 0f;
            for (int i = 0; i < length; i++)
            {
                int index = start + i;
                if (index < 0 || index >= buffer.Length) continue;
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float bright = noise - previous * 0.85f;
                previous = noise;
                buffer[index] += bright * Mathf.Exp(-t * 48f) * amplitude * 0.5f;
            }
        }

        static AudioClip MakeWind()
        {
            const float duration = 12f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] data = new float[count];
            var rng = new System.Random(4461);
            float low = 0f;
            float low2 = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.012f;
                low2 += (low - low2) * 0.05f;
                float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.055f * t + 1.4f);
                float swell2 = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.021f * t);
                data[i] = low2 * (0.5f + swell * 0.5f) * 0.6f + Mathf.Sin(2f * Mathf.PI * 118f * t) * 0.008f * swell2;
            }
            FadeEdges(data, SampleRate / 12);
            var clip = AudioClip.Create("wind", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void FadeEdges(float[] data, int fadeSamples)
        {
            fadeSamples = Mathf.Min(fadeSamples, data.Length / 4);
            for (int i = 0; i < fadeSamples; i++)
            {
                float fade = i / (float)fadeSamples;
                data[i] *= fade;
                data[data.Length - 1 - i] *= fade;
            }
        }

        static float Midi(int note)
        {
            return 440f * Mathf.Pow(2f, (note - 69) / 12f);
        }

        public void PlayMenuMusic()
        {
            if (music == null) return;
            if (music.clip != menuMusic)
            {
                music.clip = menuMusic;
                music.volume = 0.5f;
                music.Play();
            }
            else if (!music.isPlaying)
            {
                music.Play();
            }
            if (ambience != null) ambience.volume = 0.16f;
        }

        public void PlayDuelMusic()
        {
            if (music == null) return;
            if (music.clip != duelMusic)
            {
                music.clip = duelMusic;
                music.volume = 0.42f;
                music.Play();
            }
            else if (!music.isPlaying)
            {
                music.Play();
            }
            if (ambience != null) ambience.volume = 0.1f;
        }

        public void PlayAmbientVillage()
        {
            if (ambience != null) ambience.volume = 0.16f;
        }

        public void PlayClick(float volume = 0.5f) => Play(click, volume);
        public void PlaySwing(bool heavy = false) => Play(heavy ? heavySwing : swing, heavy ? 0.55f : 0.42f);
        public void PlayHit(bool heavy = false) => Play(heavy ? heavyHit : hit, heavy ? 0.78f : 0.6f);
        public void PlayDodge() => Play(dodge, 0.4f);
        public void PlayHeal() => Play(heal, 0.55f);
        public void PlayShield() => Play(shield, 0.5f);
        public void PlayBolt() => Play(bolt, 0.5f);
        public void PlayVictory() => Play(victory, 0.7f);
        public void PlayDefeat() => Play(defeat, 0.68f);

        public void PlayStep()
        {
            if (Time.unscaledTime < nextStepTime) return;
            nextStepTime = Time.unscaledTime + 0.27f;
            Play(step, 0.16f);
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip == null || sfx == null || Muted) return;
            sfx.PlayOneShot(clip, volume);
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0f : 1f;
        }
    }
}
