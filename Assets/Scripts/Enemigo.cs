using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

// Base para todos los enemigos: persigue a Link (buscando camino con el NavMesh),
// ataca cada cierto tiempo y recibe knockback.
// Esqueleto, Dragona y Goblin heredan de aquí y solo cambian Atacar() / Mover().
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Vida))]
public class Enemigo : MonoBehaviour
{
    public float velocidad = 4.5f;
    public float rangoAtaque = 1.5f;      // a qué distancia puede atacar
    public float cadenciaAtaque = 1f;     // segundos entre ataques
    public bool recibeKnockback = true;   // el Goblin lo pone en false
    public GameObject prefabCorazon;      // pieza de corazón (opcional)
    [Range(0f, 1f)] public float probCorazon = 0.05f;

    [Header("Buscar camino")]
    public bool usarNavMesh = true;       // la Dragona vuela, no lo usa
    public float cadaCuantoRuta = 0.4f;   // segundos entre cálculos de ruta
    public float tiempoAtorado = 5f;      // si no avanza en este tiempo, reaparece cerca de Link

    // Avisa al GestorOleadas que murió un enemigo (como OnMinuteChanged del Lab3)
    public static event Action<Enemigo> OnEnemigoMuerto;

    protected Transform link;
    protected Rigidbody rb;
    protected Vida vida;
    protected AnimadorModelo animador;
    protected float siguienteAtaque;
    private bool aturdido;
    private Coroutine rutinaKnockback;
    private float vidaAnterior;
    private Renderer[] renderers;

    // Ruta y control de atoramiento
    private NavMeshPath ruta;
    private float siguienteRuta;
    private Vector3 posControl;
    private float tiempoControl;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        vida = GetComponent<Vida>();
        vida.OnMuerte += Morir;
        vida.OnCambio += AlCambiarVida;
        vidaAnterior = vida.vidaMaxima;
        renderers = GetComponentsInChildren<Renderer>();
        ColocarEnSuelo();

        animador = GetComponent<AnimadorModelo>();
        if (animador == null) animador = gameObject.AddComponent<AnimadorModelo>();

        ruta = new NavMeshPath();
        siguienteRuta = Time.time + UnityEngine.Random.Range(0f, cadaCuantoRuta);   // no todos calculan en el mismo frame
        posControl = transform.position;
        tiempoControl = Time.time;

        // Busca a Link por su script (no depende del nombre del tag)
        LinkMovimiento jugador = FindFirstObjectByType<LinkMovimiento>();
        if (jugador != null) link = jugador.transform;
        else Debug.LogWarning("Enemigo: no encontré a Link (¿tiene el script LinkMovimiento?)");
    }

    protected virtual void FixedUpdate()
    {
        // Si se cayó del mapa, avisar y eliminarlo (para que la ronda no se trabe)
        if (transform.position.y < -20f)
        {
            Debug.LogWarning(name + " se cayó del mapa. Revisa la altura de los Spawns (deben estar arriba del suelo).");
            OnEnemigoMuerto?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        if (aturdido) return;

        if (link == null)
        {
            // Sin objetivo: quedarse quieto en vez de deslizarse
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 hacia = link.position - transform.position;
        hacia.y = 0f;
        float distancia = hacia.magnitude;

        // Dirección para caminar: siguiente esquina de la ruta (rodea casas), o directo si no hay ruta
        Vector3 direccion = DireccionDeCamino(hacia.normalized, distancia);

        // De cerca mira a Link; de lejos mira hacia donde camina
        Vector3 mirar = distancia <= rangoAtaque * 1.5f ? hacia : direccion;
        if (mirar != Vector3.zero)
            rb.MoveRotation(Quaternion.LookRotation(mirar));

        Mover(direccion, distancia);
        RevisarAtorado(distancia);

        if (distancia <= rangoAtaque && Time.time >= siguienteAtaque)
        {
            siguienteAtaque = Time.time + cadenciaAtaque;
            if (animador != null && AnimarAtaqueAutomatico) animador.Atacar();
            Atacar(distancia);
        }
    }

    // Sube al enemigo para que su collider quede justo encima del suelo
    // (el Goblin mide 8 m y si aparece en y = 1 quedaría medio enterrado)
    void ColocarEnSuelo()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 50f, Vector3.down, 100f);
        float sueloY = float.NegativeInfinity;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.isTrigger || h.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<Enemigo>() != null) continue;
            if (h.collider.GetComponentInParent<LinkMovimiento>() != null) continue;
            sueloY = Mathf.Max(sueloY, h.point.y);
        }
        if (float.IsNegativeInfinity(sueloY)) return;

        float deLaBaseAlCentro = transform.position.y - col.bounds.min.y;
        transform.position = new Vector3(transform.position.x, sueloY + deLaBaseAlCentro + 0.05f, transform.position.z);
        rb.position = transform.position;
    }

    // Pies del enemigo (el NavMesh está a la altura del suelo, no del centro)
    Vector3 Pies()
    {
        Collider col = GetComponent<Collider>();
        Vector3 p = transform.position;
        if (col != null) p.y = col.bounds.min.y;
        return p;
    }

    Vector3 DireccionDeCamino(Vector3 directa, float distancia)
    {
        if (!usarNavMesh || distancia <= rangoAtaque) return directa;

        // Recalcular la ruta cada cierto tiempo
        if (Time.time >= siguienteRuta)
        {
            siguienteRuta = Time.time + cadaCuantoRuta;
            ruta.ClearCorners();
            if (NavMesh.SamplePosition(Pies(), out NavMeshHit desde, 3f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(link.position, out NavMeshHit hasta, 3f, NavMesh.AllAreas))
            {
                NavMesh.CalculatePath(desde.position, hasta.position, NavMesh.AllAreas, ruta);
            }
        }

        Vector3[] esquinas = ruta.corners;
        if (ruta.status == NavMeshPathStatus.PathInvalid || esquinas.Length < 2) return directa;

        // Primera esquina que todavía no alcanzamos
        Vector3 pies = Pies();
        for (int i = 1; i < esquinas.Length; i++)
        {
            Vector3 d = esquinas[i] - pies;
            d.y = 0f;
            if (d.magnitude > 0.6f || i == esquinas.Length - 1)
                return d.sqrMagnitude > 0.0001f ? d.normalized : directa;
        }
        return directa;
    }

    // Si lleva mucho tiempo sin avanzar estando lejos, reaparece en el spawn más cercano a Link
    void RevisarAtorado(float distancia)
    {
        if (Time.time - tiempoControl < tiempoAtorado) return;

        float avanzado = Vector3.Distance(transform.position, posControl);
        if (avanzado < 0.5f && distancia > rangoAtaque * 1.5f)
        {
            GestorOleadas g = FindFirstObjectByType<GestorOleadas>();
            Transform sp = g != null ? g.SpawnMasCercano(link.position) : null;
            if (sp != null)
            {
                transform.position = sp.position;
                rb.position = sp.position;
                rb.linearVelocity = Vector3.zero;
                ColocarEnSuelo();
            }
        }
        posControl = transform.position;
        tiempoControl = Time.time;
    }

    // Por defecto camina hacia Link y se detiene al estar en rango
    protected virtual void Mover(Vector3 direccion, float distancia)
    {
        Vector3 v = distancia > rangoAtaque * 0.8f ? direccion * velocidad : Vector3.zero;
        v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;
    }

    // El Goblin lo pone en false: él decide si la animación es golpe o lanzamiento
    protected virtual bool AnimarAtaqueAutomatico { get { return true; } }

    // Cada enemigo define cómo ataca
    protected virtual void Atacar(float distancia) { }

    // Daño directo a Link (melee)
    protected void GolpearLink(float corazones)
    {
        Vida vidaLink = link.GetComponent<Vida>();
        if (vidaLink != null) vidaLink.RecibirDaño(corazones, transform.position);
    }

    // Empuja al enemigo 'metros' hacia atrás, sin física (siempre la misma distancia)
    public void Knockback(Vector3 origen, float metros)
    {
        if (!recibeKnockback || vida.Muerto) return;
        Vector3 dir = transform.position - origen;
        dir.y = 0f;
        if (rutinaKnockback != null) StopCoroutine(rutinaKnockback);
        rutinaKnockback = StartCoroutine(Aturdir(dir.normalized, metros));
    }

    private IEnumerator Aturdir(Vector3 direccion, float metros)
    {
        aturdido = true;
        float duracion = 0.15f;
        float t = 0f;
        while (t < duracion)
        {
            // Velocidad horizontal exacta para recorrer 'metros' en 'duracion'
            Vector3 v = direccion * (metros / duracion);
            v.y = rb.linearVelocity.y;
            rb.linearVelocity = v;
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        yield return new WaitForSeconds(0.2f);   // breve aturdimiento antes de volver a perseguir
        aturdido = false;
    }

    // Parpadeo rojo al recibir daño
    private void AlCambiarVida(float actual, float maxima)
    {
        if (actual < vidaAnterior) StartCoroutine(Parpadeo());
        vidaAnterior = actual;
    }

    private IEnumerator Parpadeo()
    {
        Color[] originales = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originales[i] = renderers[i].material.color;
            renderers[i].material.color = Color.red;
        }
        yield return new WaitForSeconds(0.15f);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].material.color = originales[i];
    }

    protected virtual void Morir()
    {
        if (prefabCorazon != null && UnityEngine.Random.value < probCorazon)
            Instantiate(prefabCorazon, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        OnEnemigoMuerto?.Invoke(this);
        Destroy(gameObject);
    }
}
