using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float speed = 6f, jumpSpeed = 19f, dashSpeed = 17f, dashTime = .18f, dashCooldown = .4f, coyoteTime = .1f, bufferTime = .1f;
    public float GravityScale = 5.7f;
    public int ammo;
    public bool Busy { get { return hidden || dead; } }

    Rigidbody2D rb; SpriteRenderer sr; SpriteAnimator anim; Collider2D col;
    Sprite[] idle, run, jumpA, idleG, runG, jumpG;
    float coyote, buffer, dashT, dashCd, shootCd, launchT, launchGrace, face = 1, axis;
    bool grounded, canDash = true, dashReq, jumpHeld, dead, hidden;
    readonly Collider2D[] hits = new Collider2D[8];

    static Sprite[] Frames(string p, int n) { var a = new Sprite[n]; for (int i = 0; i < n; i++) a[i] = Resources.Load<Sprite>("Sprites/" + p + i); return a; }
    static Sprite[] One(string n) { return new[] { Resources.Load<Sprite>("Sprites/" + n) }; }
    static bool K(KeyCode a, KeyCode b) { return Input.GetKey(a) || Input.GetKey(b); }
    static bool Down(KeyCode a, KeyCode b) { return Input.GetKeyDown(a) || Input.GetKeyDown(b); }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); sr = GetComponent<SpriteRenderer>(); anim = GetComponent<SpriteAnimator>(); col = GetComponent<Collider2D>();
        rb.gravityScale = GravityScale;
        idle = One("pl_idle"); jumpA = One("pl_jump"); run = Frames("pl_run_", 4);
        idleG = One("plg_idle"); jumpG = One("plg_jump"); runG = Frames("plg_run_", 4);
    }

    void Update()
    {
        if (!GameManager.I.Playing || Busy) return;
        axis = (K(KeyCode.D, KeyCode.RightArrow) ? 1 : 0) - (K(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
        if (axis != 0) face = axis;
        if (Down(KeyCode.W, KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) buffer = bufferTime;
        jumpHeld = K(KeyCode.W, KeyCode.UpArrow) || Input.GetKey(KeyCode.Space);
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K)) dashReq = true;
        shootCd -= Time.deltaTime;
        if ((Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.X)) && shootCd <= 0) Shoot();
        if (Input.GetKeyDown(KeyCode.R)) GameManager.I.PlayerDied(false); // plain restart: no sound, no effects
        sr.flipX = face < 0;
        bool g = ammo > 0;
        if (!grounded) anim.Play(g ? jumpG : jumpA);
        else if (Mathf.Abs(rb.linearVelocity.x) > .5f) anim.Play(g ? runG : run, 12);
        else anim.Play(g ? idleG : idle);
    }

    void FixedUpdate()
    {
        if (!GameManager.I.Playing || Busy) return;
        float dt = Time.fixedDeltaTime;
        launchGrace -= dt;
        grounded = launchGrace <= 0 && Grounded();
        if (grounded) { coyote = coyoteTime; canDash = true; launchT = 0; } else coyote -= dt;
        buffer -= dt; dashCd -= dt;
        Vector2 v = rb.linearVelocity;
        if (dashReq && canDash && dashCd <= 0) { dashT = dashTime; canDash = false; dashCd = dashCooldown; launchT = 0; AudioManager.Play("dash"); }
        dashReq = false;
        if (dashT > 0) { dashT -= dt; rb.gravityScale = 0; v = new Vector2(face * dashSpeed, 0); }
        else
        {
            rb.gravityScale = GravityScale;
            if (launchT > 0) { launchT -= dt; v.x += axis * 8f * dt; }
            else v.x = Mathf.MoveTowards(v.x, axis * speed, (grounded ? 90f : 45f) * dt);
            if (buffer > 0 && coyote > 0) { v.y = jumpSpeed; buffer = coyote = 0; launchT = 0; AudioManager.Play("jump"); }
            else if (!jumpHeld && v.y > 7.5f && launchT <= 0) v.y = 7.5f;
            v.y = Mathf.Max(v.y, -22.5f);
        }
        rb.linearVelocity = v;
    }

    bool Grounded()
    {
        var f = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.OverlapBox(rb.position + Vector2.down * .6f, new Vector2(.5f, .12f), 0, f, hits);
        for (int i = 0; i < n; i++) if (hits[i] != col) return true;
        return false;
    }

    void Shoot()
    {
        shootCd = .25f;
        if (ammo <= 0) { AudioManager.Play("click"); return; }
        ammo--; AudioManager.Play("shot");
        var go = new GameObject("Bullet"); go.transform.position = transform.position + new Vector3(face * .7f, .1f, 0);
        var s = go.AddComponent<SpriteRenderer>(); s.sprite = Resources.Load<Sprite>("Sprites/bullet"); s.sortingOrder = 6;
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var c = go.AddComponent<CircleCollider2D>(); c.isTrigger = true; c.radius = .12f;
        go.AddComponent<Bullet>().dir = face;
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        var p = o.GetComponent<Pickup>();
        if (!p || dead) return;
        switch (p.kind)
        {
            case Pickup.Kind.Ammo: ammo = Mathf.Min(9, ammo + 2); AudioManager.Play("pickup"); Destroy(o.gameObject); break;
            case Pickup.Kind.Goal: GameManager.I.LevelComplete(); break;
            case Pickup.Kind.Kill: Die(o.name == "Sea" ? "sea" : "enemy"); break;
        }
    }

    public void Die(string why)
    {
        if (dead || !GameManager.I.Playing) return;
        dead = true; AudioManager.Play(why == "sea" ? "splash" : "die");
        rb.linearVelocity = Vector2.zero; GameManager.I.PlayerDied(true);
    }

    // ---- cannon hooks ----
    public void EnterCannon() { hidden = true; rb.linearVelocity = Vector2.zero; rb.simulated = false; sr.enabled = false; }
    public void ExitCannon() { hidden = false; rb.simulated = true; sr.enabled = true; }
    public void Launch(Vector2 vel, Vector3 pos)
    {
        ExitCannon(); transform.position = pos; rb.position = pos; rb.linearVelocity = vel;
        launchT = 1.4f; launchGrace = .25f; canDash = true; dashT = 0; grounded = false;
    }
}
