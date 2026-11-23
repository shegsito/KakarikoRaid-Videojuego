using UnityEngine;

// Proyectil genérico: flechas de Link, bolas de fuego de la Dragona y rocas del Goblin.
// Avanza en línea recta y daña al primer objetivo del bando contrario.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Proyectil : MonoBehaviour
{
    public enum Bando { Link, Enemigos }

    public Bando dañaA = Bando.Link;     // a quién le hace daño
    public float velocidad = 15f;
    public float daño = 1f;
    public float vidaUtil = 5f;          // segundos antes de desaparecer solo
    public float knockback = 1f;         // metros (solo aplica a enemigos)

    private Vector3 origen;

    void Awake()
    {
        // Configuración automática para que no haya que acordarse en el Inspector
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        origen = transform.position;
        Destroy(gameObject, vidaUtil);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;   // ignorar otros proyectiles y zonas

        if (dañaA == Bando.Link)
        {
            LinkMovimiento link = other.GetComponentInParent<LinkMovimiento>();
            if (link != null)
            {
                link.GetComponent<Vida>().RecibirDaño(daño, origen);
                Destroy(gameObject);
                return;
            }
            if (other.GetComponentInParent<Enemigo>() != null) return;   // atraviesa a sus aliados
        }
        else
        {
            Enemigo e = other.GetComponentInParent<Enemigo>();
            if (e != null)
            {
                e.GetComponent<Vida>().RecibirDaño(daño, origen);
                e.Knockback(origen, knockback);
                Destroy(gameObject);
                return;
            }
            if (other.GetComponentInParent<LinkMovimiento>() != null) return;   // no chocar con Link
        }

        // Pegó en el suelo, una pared o una casa
        Destroy(gameObject);
    }
}
