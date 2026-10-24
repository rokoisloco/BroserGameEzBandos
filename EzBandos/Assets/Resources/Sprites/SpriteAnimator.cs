using UnityEngine;

// Frame-by-frame sprite animation. Call Play() every frame with a cached array; it only restarts when the array changes.
public class SpriteAnimator : MonoBehaviour
{
    SpriteRenderer sr; Sprite[] frames; float fps = 10, t;
    void Awake() { sr = GetComponent<SpriteRenderer>(); }
    public void Play(Sprite[] f, float rate = 10f)
    {
        fps = rate;
        if (f == frames) return;
        frames = f; t = 0;
        if (f != null && f.Length > 0) sr.sprite = f[0];
    }
    void Update()
    {
        if (frames == null || frames.Length < 2) return;
        t += Time.deltaTime * fps;
        sr.sprite = frames[(int)t % frames.Length];
    }
}
