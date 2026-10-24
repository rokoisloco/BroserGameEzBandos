using UnityEngine;

// Walk into the cannon, press E, aim (mouse or A/D), fire (Space / click) to fly across big gaps.
public class CannonLauncher : MonoBehaviour
{
    public Transform barrel;
    const float Power = 30f;
    PlayerController pl; bool inside, aiming; float angle = 60 * Mathf.Deg2Rad, cd; Vector3 lastMouse; LineRenderer line;

    void Start()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = new Color(1, 1, 1, .85f);
        line.startWidth = line.endWidth = .12f; line.sortingOrder = 8; line.enabled = false;
        Apply();
    }
    void OnTriggerEnter2D(Collider2D o) { var p = o.GetComponent<PlayerController>(); if (p) { pl = p; inside = true; } }
    void OnTriggerExit2D(Collider2D o) { if (o.GetComponent<PlayerController>()) inside = false; }
    static bool Key(KeyCode a, KeyCode b) { return Input.GetKey(a) || Input.GetKey(b); }
    Vector2 Dir { get { return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); } }
    void Apply() { barrel.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg); }

    void Update()
    {
        cd -= Time.deltaTime;
        if (!GameManager.I.Playing) return;
        bool use = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        if (!aiming)
        {
            if (inside && pl && !pl.Busy)
            {
                GameManager.I.Prompt = "Press E to enter the cannon";
                if (use && cd <= 0) { aiming = true; pl.EnterCannon(); AudioManager.Play("enter"); lastMouse = Input.mousePosition; }
            }
            return;
        }
        GameManager.I.Prompt = "Aim: mouse or A/D    Fire: Space / click    Exit: E";
        if (Key(KeyCode.A, KeyCode.LeftArrow)) angle += 2f * Time.deltaTime;
        if (Key(KeyCode.D, KeyCode.RightArrow)) angle -= 2f * Time.deltaTime;
        if ((Input.mousePosition - lastMouse).sqrMagnitude > 4f)
        {
            lastMouse = Input.mousePosition;
            var m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            angle = Mathf.Atan2(m.y - barrel.position.y, m.x - barrel.position.x);
        }
        angle = Mathf.Clamp(angle, 5 * Mathf.Deg2Rad, 175 * Mathf.Deg2Rad); Apply(); Preview();
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            pl.Launch(Dir * Power, barrel.position + (Vector3)(Dir * 1.1f));
            aiming = false; line.enabled = false; cd = .7f; AudioManager.Play("cannon");
        }
        else if (use) { aiming = false; line.enabled = false; pl.ExitCannon(); cd = .3f; }
    }

    void Preview()
    {
        line.enabled = true;
        Vector2 pos = (Vector2)barrel.position + Dir * 1.1f, v = Dir * Power;
        float g = Physics2D.gravity.y * pl.GravityScale; int n;
        var pts = new Vector3[40];
        for (n = 0; n < 40; n++)
        {
            pts[n] = pos; v.y += g * .04f; pos += v * .04f;
            if (Physics2D.OverlapPoint(pos) != null || pos.y < 2f) { n++; break; }
        }
        line.positionCount = n;
        for (int i = 0; i < n; i++) line.SetPosition(i, pts[i]);
    }
}
