using UnityEngine;
using System.Collections;

// Movimiento de Link: WASD relativo a la cámara, salto con SPACE y rodar (dash) con SHIFT
[RequireComponent(typeof(Rigidbody))]
public class LinkMovimiento : MonoBehaviour
{
    public float velocidad = 8f;          // media-rápida
    public float fuerzaSalto = 6f;
    public float velocidadGiro = 12f;     // qué tan rápido voltea hacia donde camina
    public float distanciaSuelo = 1.1f;   // largo del rayo hacia abajo (mitad de la cápsula + margen)
    public Transform camara;              // cámara principal (se asigna en el Inspector)
    public bool mirarHaciaCamara = true;  // true = estilo Fortnite, false = voltea hacia donde camina

    [Header("Rodar (dash)")]
    public float distanciaDash = 5f;      // metros que avanza al rodar
    public float duracionDash = 0.3f;
    public float cooldownDash = 0.8f;
    public bool invencibleAlRodar = true; // no recibe daño mientras rueda
    public Transform modelo;              // parte visual que gira (si está vacío se crea sola)

    // Otros scripts (escudo, knockback) pueden bloquear el movimiento
    [HideInInspector] public bool puedeMoverse = true;
    [HideInInspector] public float factorVelocidad = 1f;   // el escudo lo baja a 0.5

    public bool Rodando { get; private set; }

    private Rigidbody rb;
    private Vector3 movimiento;
    private bool quiereSaltar;
    private float siguienteDash;
    private Vida vida;
    private LinkEscudo escudo;

    void Awake()
    {
        if (modelo == null) modelo = CrearContenedorModelo();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;   // que no se voltee con la física
        vida = GetComponent<Vida>();
        escudo = GetComponent<LinkEscudo>();
        if (GetComponent<ParpadeoDanio>() == null) gameObject.AddComponent<ParpadeoDanio>();   // se pone rojo al recibir daño
        if (camara == null) camara = Camera.main.transform;
    }

    // Pasa la malla de Link y sus hijos visuales a un hijo "Modelo",
    // para poder girarlo al rodar sin girar el Rigidbody ni la cámara.
    Transform CrearContenedorModelo()
    {
        GameObject contenedor = new GameObject("Modelo");
        contenedor.transform.SetParent(transform, false);

        // Mover los hijos actuales (Nariz, modelo 3D, etc.)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo != contenedor.transform) hijo.SetParent(contenedor.transform, true);
        }

        // Si Link tiene la malla en sí mismo (cápsula del graybox), copiarla al contenedor
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mf != null && mr != null && mr.enabled)
        {
            contenedor.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            contenedor.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
            mr.enabled = false;
        }
        return contenedor.transform;
    }

    void Update()   // cada frame: leer el teclado
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // Adelante/derecha de la cámara, pero planos (sin inclinación)
        Vector3 adelante = camara.forward; adelante.y = 0f; adelante.Normalize();
        Vector3 derecha  = camara.right;   derecha.y  = 0f; derecha.Normalize();

        movimiento = (adelante * z + derecha * x).normalized;

        if (Input.GetKeyDown(KeyCode.Space) && EnSuelo())
        {
            quiereSaltar = true;
            GestorAudio.Reproducir(GestorAudio.Sfx.VozSaltar);   // solo una vez, al presionar
        }

        bool cubriendose = escudo != null && escudo.Bloqueando;
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= siguienteDash && !Rodando
            && puedeMoverse && !cubriendose && Time.timeScale > 0f)
        {
            // Rueda hacia donde camina; si está quieto, hacia adelante
            Vector3 dir = movimiento != Vector3.zero ? movimiento : transform.forward;
            StartCoroutine(Rodar(dir));
        }
    }

    IEnumerator Rodar(Vector3 direccion)
    {
        Rodando = true;
        siguienteDash = Time.time + cooldownDash;
        if (invencibleAlRodar && vida != null) vida.invencible = true;

        // Eje de giro: perpendicular a la dirección (rueda hacia adelante en esa dirección)
        Vector3 eje = Vector3.Cross(Vector3.up, direccion).normalized;
        Quaternion rotInicial = modelo != null ? modelo.localRotation : Quaternion.identity;

        float t = 0f;
        while (t < duracionDash)
        {
            Vector3 v = direccion * (distanciaDash / duracionDash);
            v.y = rb.linearVelocity.y;
            rb.linearVelocity = v;

            if (modelo != null)
            {
                float angulo = 360f * (t / duracionDash);
                // El eje está en el mundo; se convierte al espacio local de Link
                modelo.localRotation = Quaternion.AngleAxis(angulo, transform.InverseTransformDirection(eje)) * rotInicial;
            }

            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (modelo != null) modelo.localRotation = rotInicial;
        if (vida != null) vida.invencible = false;
        Rodando = false;
    }

    void FixedUpdate()   // física: mover con el Rigidbody
    {
        if (!puedeMoverse || Rodando) return;

        // Mantener la velocidad vertical (gravedad/salto) y reemplazar la horizontal
        Vector3 v = movimiento * velocidad * factorVelocidad;
        v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;

        if (mirarHaciaCamara)
        {
            // Estilo Fortnite: Link siempre mira hacia donde apunta la cámara
            Vector3 adelante = camara.forward; adelante.y = 0f;
            if (adelante != Vector3.zero)
                rb.MoveRotation(Quaternion.LookRotation(adelante));
        }
        else if (movimiento != Vector3.zero)
        {
            // Estilo clásico: voltear a Link hacia donde camina
            Quaternion objetivo = Quaternion.LookRotation(movimiento);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, objetivo, velocidadGiro * Time.fixedDeltaTime));
        }

        if (quiereSaltar)
        {
            rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.VelocityChange);
            quiereSaltar = false;
        }
    }

    // Rayo hacia abajo: ¿hay algo justo debajo de Link?
    public bool EnSuelo()
    {
        return Physics.Raycast(transform.position, Vector3.down, distanciaSuelo);
    }
}
