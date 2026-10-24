using System.Collections.Generic;
using UnityEngine;

// All audio is synthesised at start-up: no audio files needed. M toggles mute.
public class AudioManager : MonoBehaviour
{
    public static AudioManager I; public static bool Muted;
    const int SR = 22050;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    AudioSource sfx, music;

    public static void Play(string n) { AudioClip c; if (I && !Muted && I.clips.TryGetValue(n, out c)) I.sfx.PlayOneShot(c); }

    void Awake()
    {
        I = this;
        sfx = gameObject.AddComponent<AudioSource>();
        music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = .45f;
        clips["jump"] = Tone(.15f, 300, 300, 0, .3f);
        clips["dash"] = Tone(.15f, 0, 0, 1, .3f);
        clips["shot"] = Tone(.25f, 0, 0, 1, .6f);
        clips["cannon"] = Tone(.6f, 0, 0, 1, .9f);
        clips["pickup"] = Seq(new float[] { 660, 880 }, .08f, .3f);
        clips["die"] = Tone(.6f, 400, -350, 2, .3f);
        clips["splash"] = Tone(.45f, 0, 0, 1, .5f);
        clips["win"] = Seq(new float[] { 523, 659, 784, 1047 }, .14f, .35f);
        clips["hit"] = Tone(.12f, 200, -100, 0, .3f);
        clips["enter"] = Tone(.1f, 150, 0, 4, .4f);
        clips["click"] = Tone(.04f, 900, 0, 0, .15f);
        float[] m = { 392,0,392,440,494,0,494,440,392,330,0,330,392,0,0,0, 294,0,330,392,440,0,440,392,330,294,330,0,0,0,0,0 };
        music.clip = Seq(m, .23f, .25f); music.Play();
    }
    void Update() { if (Input.GetKeyDown(KeyCode.M)) { Muted = !Muted; music.mute = Muted; } }

    AudioClip Make(float dur, System.Func<float, float> f)
    {
        int len = (int)(SR * dur); var d = new float[len];
        for (int i = 0; i < len; i++) d[i] = f(i / (float)SR);
        var a = AudioClip.Create("gen", len, 1, SR, false); a.SetData(d, 0); return a;
    }
    // wave: 0 square, 1 noise, 2 saw, 3 sine, 4 triangle
    AudioClip Tone(float dur, float f0, float slide, int wave, float vol)
    {
        return Make(dur, t =>
        {
            float ph = f0 * t + slide * t * t / (2 * dur), s;
            switch (wave) { case 1: s = Random.value * 2 - 1; break; case 2: s = 2 * (ph % 1) - 1; break; case 3: s = Mathf.Sin(ph * 6.2832f); break; case 4: s = Mathf.Abs(ph % 1 * 4 - 2) - 1; break; default: s = (ph % 1) < .5f ? 1 : -1; break; }
            return s * vol * Mathf.Pow(1 - t / dur, 2);
        });
    }
    AudioClip Seq(float[] f, float note, float vol)
    {
        int n = f.Length;
        return Make(note * n, t =>
        {
            int i = Mathf.Min(n - 1, (int)(t / note)); float lt = t - i * note;
            if (f[i] <= 0) return 0; float ph = f[i] * t;
            return (Mathf.Abs(ph % 1 * 4 - 2) - 1) * vol * Mathf.Clamp01(1 - lt / note);
        });
    }
}
