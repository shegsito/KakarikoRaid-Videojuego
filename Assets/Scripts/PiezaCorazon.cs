using UnityEngine;

// Pieza de corazón que sueltan los enemigos (5%). Link la recoge al tocarla y se cura.
[RequireComponent(typeof(Collider))]
public class PiezaCorazon : MonoBehaviour
{
    public float curacion = 1f;          // corazones que recupera
    public float duracion = 15f;         // desaparece si no se recoge
    public float velocidadGiro = 90f;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        // Bajar al suelo (la Dragona muere en el aire)
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50f))
            transform.position = hit.point + Vector3.up * 0.5f;

        Destroy(gameObject, duracion);
    }

    void Update()
    {
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        LinkMovimiento link = other.GetComponentInParent<LinkMovimiento>();
        if (link == null) return;

        link.GetComponent<Vida>().Curar(curacion);
        Debug.Log("Pieza de corazón: +" + curacion);
        Destroy(gameObject);
    }
}
