using System.Collections;
using UnityEngine;

// Builds the whole game from code. Drop the Assets folder into any Unity project and press Play in an empty scene.
public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public enum State { Title, Select, Play, Win }
    public State state = State.Title;
    public bool Playing { get { return state == State.Play && !busy; } }
    public string Prompt = "";
    const int Rows = 17;
    int level = 1, unlocked = 1, levelW = 40;
    float fade, hintTimer, camVel;
    bool busy;
    string hint = "";
    GameObject world; PlayerController player; Camera cam;
    GUIStyle label, button, title;

    static Sprite Spr(string n) { return Resources.Load<Sprite>("Sprites/" + n); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { new GameObject("Game").AddComponent<GameManager>(); }

    void Awake()
    {
        I = this;
        Physics2D.queriesHitTriggers = false;
        cam = Camera.main;
        if (!cam) { var c = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); c.tag = "MainCamera"; cam = c.GetComponent<Camera>(); }
        cam.orthographic = true; cam.orthographicSize = Rows / 2f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.05f, .3f, .43f);
        cam.transform.position = new Vector3(15, Rows / 2f, -10);
        var sea = new GameObject("SeaBackground"); sea.transform.SetParent(cam.transform, false);
        sea.transform.localPosition = new Vector3(0, 0, 20); sea.transform.localScale = new Vector3(400, 2.2f, 1);
        var sr = sea.AddComponent<SpriteRenderer>(); sr.sprite = Spr("sea_bg"); sr.sortingOrder = -100;
        gameObject.AddComponent<AudioManager>();
        QualitySettings.vSyncCount = 1;
    }

    GameObject Tile(Vector3 p, string spr, int order, bool solid)
    {
        var g = new GameObject(spr); g.transform.SetParent(world.transform); g.transform.position = p;
        var s = g.AddComponent<SpriteRenderer>(); s.sprite = Spr(spr); s.sortingOrder = order;
        if (solid) g.AddComponent<BoxCollider2D>();
        return g;
    }

    void Build(int n)
    {
        level = n;
        if (world) Destroy(world);
        world = new GameObject("World");
        var lines = Resources.Load<TextAsset>("Levels/level" + n.ToString("00")).text.Replace("\r", "").Split('\n');
        hint = lines[0].Substring(1); hintTimer = 8f; levelW = lines[1].Length;
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < lines[r + 1].Length; c++)
            {
                var p = new Vector3(c + .5f, Rows - r - .5f, 0);
                switch (lines[r + 1][c])
                {
                    case '#': Tile(p, "plank_" + ((c * 7 + r * 13) % 3), 0, true); break;
                    case 'P': SpawnPlayer(p); break;
                    case 'E': SpawnEnemy(p); break;
                    case 'A': SpawnAmmo(p); break;
                    case 'C': SpawnCannon(p); break;
                    case 'G': SpawnGoal(p); break;
                }
            }
        for (int c = -20; c < levelW + 20; c++) { Tile(new Vector3(c + .5f, 1.5f, 0), "water_top", 10, false); Tile(new Vector3(c + .5f, .5f, 0), "water", 10, false); }
        var sea = new GameObject("Sea"); sea.transform.SetParent(world.transform); sea.transform.position = new Vector3(levelW / 2f, 1.5f, 0);
        var bc = sea.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(levelW + 80, 1f);
        sea.AddComponent<Pickup>().kind = Pickup.Kind.Kill;
        state = State.Play;
    }

    void SpawnPlayer(Vector3 p)
    {
        var g = Tile(p + Vector3.up * .15f, "pl_idle", 5, false); g.name = "Player";
        var rb = g.AddComponent<Rigidbody2D>(); rb.freezeRotation = true; rb.interpolation = RigidbodyInterpolation2D.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = g.AddComponent<CapsuleCollider2D>(); col.size = new Vector2(.6f, 1.1f); col.offset = new Vector2(0, -.05f);
        col.sharedMaterial = new PhysicsMaterial2D("Slick") { friction = 0, bounciness = 0 };
        g.AddComponent<SpriteAnimator>(); player = g.AddComponent<PlayerController>();
    }
    void SpawnEnemy(Vector3 p)
    {
        var g = Tile(p + Vector3.up * .125f, "en_walk_0", 4, false); g.name = "Pirate";
        var rb = g.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var c = g.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(.6f, 1.1f); c.offset = new Vector2(0, -.05f);
        g.AddComponent<Pickup>().kind = Pickup.Kind.Kill; g.AddComponent<SpriteAnimator>(); g.AddComponent<EnemyPatrol>();
    }
    void SpawnAmmo(Vector3 p)
    {
        var g = Tile(p + Vector3.down * .25f, "ammo", 3, false); g.name = "Powder";
        var c = g.AddComponent<CircleCollider2D>(); c.isTrigger = true; c.radius = .4f; g.AddComponent<Pickup>().kind = Pickup.Kind.Ammo;
    }
    void SpawnCannon(Vector3 p)
    {
        var g = Tile(p, "cannon_base", 3, false); g.name = "Cannon";
        var b = Tile(p + new Vector3(0, .1f, 0), "cannon_barrel", 4, false); b.transform.SetParent(g.transform);
        var c = g.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(1.6f, 1.2f);
        g.AddComponent<CannonLauncher>().barrel = b.transform;
    }
    void SpawnGoal(Vector3 p)
    {
        var g = Tile(p + Vector3.up * .5f, "flag_0", 3, false); g.name = "Flag";
        var c = g.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(1, 2);
        g.AddComponent<Pickup>().kind = Pickup.Kind.Goal;
        g.AddComponent<SpriteAnimator>().Play(new[] { Spr("flag_0"), Spr("flag_1") }, 4);
    }

    // ---- flow: death = fade to black, rebuild the level, fade back in ----
    public void PlayerDied(bool fx) { if (!busy) StartCoroutine(Restart()); }
    IEnumerator Restart() { busy = true; yield return Fade(1, .45f); yield return new WaitForSeconds(.35f); Build(level); yield return Fade(0, .45f); busy = false; }
    public void LevelComplete() { if (!busy) StartCoroutine(Next()); }
    IEnumerator Next()
    {
        busy = true; AudioManager.Play("win"); yield return new WaitForSeconds(.4f); yield return Fade(1, .5f);
        if (level >= 30) { Destroy(world); state = State.Win; }
        else { unlocked = Mathf.Max(unlocked, level + 1); Build(level + 1); }
        yield return Fade(0, .5f); busy = false;
    }
    IEnumerator Fade(float to, float t) { float f = fade, e = 0; while (e < t) { e += Time.deltaTime; fade = Mathf.Lerp(f, to, e / t); yield return null; } fade = to; }

    void Update()
    {
        if (hintTimer > 0) hintTimer -= Time.deltaTime;
        if (state == State.Play && Input.GetKeyDown(KeyCode.Escape)) { StopAllCoroutines(); busy = false; fade = 0; if (world) Destroy(world); state = State.Select; }
    }

    void LateUpdate()
    {
        var p = cam.transform.position; float half = cam.orthographicSize * cam.aspect; float tx = 15;
        if (state == State.Play && player) { tx = Mathf.Clamp(player.transform.position.x, half, Mathf.Max(half, levelW - half)); p.x = Mathf.SmoothDamp(p.x, tx, ref camVel, .12f); }
        else p.x = tx;
        p.y = Rows / 2f; cam.transform.position = p;
    }

    // ---- UI: every text block has its own rect, so nothing overlaps ----
    void Box(Rect r, string t)
    {
        GUI.color = new Color(.07f, .04f, .02f, .75f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 8, r.y + 4, r.width - 16, r.height - 8), t, label);
    }
    void OnGUI()
    {
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            label.normal.textColor = Color.white;
            button = new GUIStyle(GUI.skin.button) { fontSize = 26, fontStyle = FontStyle.Bold };
            title = new GUIStyle(label) { fontSize = 64 }; title.normal.textColor = new Color(.96f, .84f, .54f);
        }
        float s = Screen.height / 540f; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1)); float W = Screen.width / s;
        if (state == State.Title)
        {
            GUI.Label(new Rect(0, 100, W, 90), "PLANK & POWDER", title);
            GUI.Label(new Rect(0, 190, W, 40), "A pirate puzzle adventure - 30 levels", label);
            if (GUI.Button(new Rect(W / 2 - 110, 300, 220, 60), "Set Sail", button)) { AudioManager.Play("click"); state = State.Select; }
            GUI.Label(new Rect(0, 490, W, 30), "Created by the crew of the Salty Pixel   |   M = mute", label);
        }
        else if (state == State.Select)
        {
            GUI.Label(new Rect(0, 40, W, 80), "Choose Your Voyage", title);
            for (int i = 0; i < 30; i++)
            {
                GUI.enabled = i < unlocked;
                if (GUI.Button(new Rect(W / 2 - 390 + i % 6 * 130, 140 + i / 6 * 80, 120, 70), i < unlocked ? (i + 1).ToString() : "X", button)) { AudioManager.Play("click"); Build(i + 1); }
            }
            GUI.enabled = true;
        }
        else if (state == State.Win)
        {
            GUI.Label(new Rect(0, 150, W, 90), "You are the Pirate King!", title);
            if (GUI.Button(new Rect(W / 2 - 110, 300, 220, 60), "Level Select", button)) state = State.Select;
        }
        else
        {
            int a = player ? player.ammo : 0;
            Box(new Rect(14, 10, 200, 62), "Level " + level + " / 30\nPowder: " + (a > 0 ? new string('o', a) : "-"));
            Box(new Rect(W - 324, 10, 310, 34), "R restart | Esc menu | M mute");
            if (hintTimer > 0 && hint.Length > 0) { float h = label.CalcHeight(new GUIContent(hint), 700) + 16; Box(new Rect((W - 720) / 2, 82, 720, h), hint); }
            if (Prompt != "") Box(new Rect((W - 600) / 2, 490, 600, 36), Prompt);
        }
        if (Event.current.type == EventType.Repaint) Prompt = "";
        if (fade > 0) { GUI.color = new Color(0, 0, 0, fade); GUI.DrawTexture(new Rect(0, 0, W, 540), Texture2D.whiteTexture); GUI.color = Color.white; }
    }
}
