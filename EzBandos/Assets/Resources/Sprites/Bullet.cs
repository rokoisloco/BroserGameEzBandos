using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float dir = 1; float life = 1.2f;
    void Update()
    {
        transform.position += Vector3.right * dir * 18f * Time.deltaTime;
        life -= Time.deltaTime;
        if (life <= 0) Destroy(gameObject);
    }
    void OnTriggerEnter2D(Collider2D o)
    {
        if (o.GetComponent<PlayerController>()) return;
        var e = o.GetComponent<EnemyPatrol>();
        if (e) { e.Die(); Destroy(gameObject); return; }
        if (!o.isTrigger) Destroy(gameObject);
    }
}
