using UnityEngine;

// Sword pirate: walks back and forth on its plank, turns at edges and walls. Never chases.
public class EnemyPatrol : MonoBehaviour
{
    public float speed = 1.8f;
    float dir = 1; bool alive = true; Rigidbody2D rb; SpriteRenderer sr;
    void Awake() { rb = GetComponent<Rigidbody2D>(); sr = GetComponent<SpriteRenderer>(); }
    void Start()
    {
        var walk = new Sprite[4];
        for (int i = 0; i < 4; i++) walk[i] = Resources.Load<Sprite>("Sprites/en_walk_" + i);
        GetComponent<SpriteAnimator>().Play(walk, 8);
    }
    void FixedUpdate()
    {
        if (!alive || !GameManager.I.Playing) return;
        Vector2 p = rb.position;
        bool ground = Solid(Physics2D.Raycast(p + new Vector2(dir * .45f, 0), Vector2.down, 1f));
        bool wall = Solid(Physics2D.Raycast(p, new Vector2(dir, 0), .55f));
        if (!ground || wall) dir = -dir;
        rb.MovePosition(p + new Vector2(dir * speed * Time.fixedDeltaTime, 0));
        sr.flipX = dir < 0;
    }
    static bool Solid(RaycastHit2D h) { return h.collider != null && !h.collider.GetComponent<PlayerController>(); }
    public void Die() { if (!alive) return; alive = false; AudioManager.Play("hit"); Destroy(gameObject); }
}
