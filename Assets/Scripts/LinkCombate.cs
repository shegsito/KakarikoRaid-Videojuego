using UnityEngine;
using System;
using System.Collections;

// Combate de Link: espada (click izq / mantener = ataque circular cargado), arco (click izq dispara
// hacia la mira) y cambio de arma con F.
public class LinkCombate : MonoBehaviour
{
    public enum Arma { Espada, Arco }
    public Arma armaActual = Arma.Espada;

    [Header("Espada")]
    public float daño = 1f;                 // 1 corazón por espadazo
    public float alcance = 1.2f;            // qué tan adelante pega
    public float radioGolpe = 1f;
    public float radioGiro = 2.5f;          // ataque circular
    public float tiempoCarga = 0.6f;        // segundos manteniendo click para el giro
    public float cooldown = 0.35f;
    public float knockback = 1.5f;          // metros que retrocede el enemigo
    public Transform espadaVisual;          // modelo de la espada (opcional, solo para animarla)
    public bool atacarHaciaCamara = true;   // el espadazo sale hacia donde mira la cámara
    public bool mostrarSlash = true;        // dibuja el área del golpe por un instante
    public Color colorSlash = new Color(0.6f, 0.9f, 1f);   // borde del corte normal
    public Material materialEfectos;        // opcional: si está vacío se crea uno brillante (aditivo)
    public Color colorGiro = new Color(1f, 0.9f, 0.3f);

    [Header("Giro cargado (como en el Zelda original)")]
    public float segundosDañoMin = 1f;      // mantener 1 s  -> daño mínimo
    public float segundosDañoMax = 2f;      // mantener 2 s  -> daño máximo
    public float dañoGiroMin = 1f;          // 1 corazón
    public float dañoGiroMax = 4f;          // 4 corazones
    public float radioGiroMax = 3.5f;       // el giro cargado al máximo alcanza más lejos
    public bool mostrarCarga = true;        // círculo en el piso que crece mientras se carga
    public Color colorCargaMax = new Color(1f, 0.2f, 0.1f);

    // (0 a 1) cuánto lleva cargado el giro -> por si el HUD lo quiere mostrar
    public event Action<float> OnCarga;

    [Header("Arco")]
    public GameObject prefabFlecha;
    public float dañoFlecha = 1f;           // 1 corazón por disparo
    public float cadenciaArco = 0.5f;       // segundos entre flechas
    public Transform arcoVisual;            // modelo del arco (se ve solo con el arco equipado)
    // La mira (crosshair) la muestra el HUD cuando el arma es el Arco

    // El HUD se suscribe para mostrar "Espada" o "Arco"
    public event Action<Arma> OnArmaCambio;

    private float inicioClick;
    private bool cargando;
    private GameObject indicadorCarga;
    private float siguienteAtaque;
    private Rigidbody rb;
    private LinkEscudo escudo;
    private AnimadorModelo animador;
    private bool ladoCorte;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        escudo = GetComponent<LinkEscudo>();
        animador = GetComponent<AnimadorModelo>();
        if (animador == null) animador = gameObject.AddComponent<AnimadorModelo>();
        CambiarArma(armaActual);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;   // en pausa no se ataca
        if (escudo != null && escudo.Bloqueando) { CancelarCarga(); return; }   // cubriéndose no se ataca

        if (Input.GetKeyDown(KeyCode.F))
            CambiarArma(armaActual == Arma.Espada ? Arma.Arco : Arma.Espada);

        if (armaActual == Arma.Espada)
            ActualizarEspada();
        else
            ActualizarArco();
    }

    void CambiarArma(Arma nueva)
    {
        armaActual = nueva;
        CancelarCarga();
        if (espadaVisual != null)
            espadaVisual.gameObject.SetActive(armaActual == Arma.Espada);
        if (arcoVisual != null)
            arcoVisual.gameObject.SetActive(armaActual == Arma.Arco);
        OnArmaCambio?.Invoke(armaActual);
    }

    void ActualizarArco()
    {
        if (!Input.GetMouseButtonDown(0) || Time.time < siguienteAtaque) return;
        if (prefabFlecha == null)
        {
            Debug.LogWarning("LinkCombate: falta asignar el Prefab Flecha");
            return;
        }
        siguienteAtaque = Time.time + cadenciaArco;
        GestorAudio.Reproducir(GestorAudio.Sfx.DisparoArco);   // solo el arco, sin voz de Link

        // Rayo desde el centro de la pantalla: ¿a qué está apuntando la mira?
        Transform cam = Camera.main.transform;
        Vector3 puntoMira = cam.position + cam.forward * 100f;
        RaycastHit[] hits = Physics.RaycastAll(cam.position, cam.forward, 100f);
        float masCerca = Mathf.Infinity;
        // Con la cámara al hombro, ignorar lo que esté entre la cámara y Link
        float distanciaALink = Vector3.Dot(transform.position - cam.position, cam.forward);
        foreach (RaycastHit h in hits)
        {
            if (h.collider.isTrigger || h.transform.IsChildOf(transform)) continue;   // ignorar a Link
            if (h.distance < distanciaALink) continue;
            if (h.distance < masCerca) { masCerca = h.distance; puntoMira = h.point; }
        }

        // La flecha sale del pecho de Link y va hacia el punto de la mira
        Vector3 salida = transform.position + Vector3.up * 0.5f + transform.forward * 0.6f;
        GameObject flecha = Instantiate(prefabFlecha, salida, Quaternion.LookRotation(puntoMira - salida));
        Proyectil p = flecha.GetComponent<Proyectil>();
        if (p == null)
        {
            Debug.LogWarning("El prefab '" + prefabFlecha.name + "' no tiene el script Proyectil. Se lo agrego, pero agrégalo al prefab.");
            p = flecha.AddComponent<Proyectil>();
        }
        p.dañaA = Proyectil.Bando.Enemigos;
        p.daño = dañoFlecha;
    }

    void ActualizarEspada()
    {
        if (Input.GetMouseButtonDown(0))
        {
            inicioClick = Time.time;
            cargando = true;
        }

        // Mientras se mantiene el click: mostrar la carga
        if (cargando && Input.GetMouseButton(0))
        {
            float mantenido = Time.time - inicioClick;
            if (mantenido >= tiempoCarga)
            {
                float f = FraccionCarga(mantenido);
                OnCarga?.Invoke(f);
                if (mostrarCarga) ActualizarIndicador(f);
            }
        }

        if (Input.GetMouseButtonUp(0) && cargando)
        {
            float mantenido = Time.time - inicioClick;
            CancelarCarga();
            if (Time.time < siguienteAtaque) return;
            siguienteAtaque = Time.time + cooldown;

            bool giro = mantenido >= tiempoCarga;
            if (giro)
            {
                float f = FraccionCarga(mantenido);
                float dañoGiro = DañoGiro(mantenido);
                float radio = Mathf.Lerp(radioGiro, radioGiroMax, f);
                Golpear(transform.position, radio, dañoGiro, knockback * (1f + f));
                if (mostrarSlash) MostrarSlash(true, radio, f);
                GestorAudio.Reproducir(GestorAudio.Sfx.EspadazoCircular, Mathf.Lerp(0.8f, 1.2f, f));
                GestorAudio.Reproducir(GestorAudio.Sfx.VozGiro);   // el giro tiene su propio grito
                Debug.Log("Giro cargado " + mantenido.ToString("0.0") + " s -> " + dañoGiro + " de daño");
            }
            else
            {
                if (atacarHaciaCamara) VoltearHaciaCamara();
                Golpear(transform.position + transform.forward * alcance, radioGolpe, daño, knockback);
                if (mostrarSlash) MostrarSlash(false, radioGolpe, 0f);
                GestorAudio.Reproducir(GestorAudio.Sfx.Espadazo);
                GestorAudio.VozAtaque();
                if (animador != null) animador.Atacar();
            }

            if (giro && animador != null) animador.Girar(0.35f);              // Link gira con todo y espada
            else if (espadaVisual != null) StartCoroutine(AnimarEspada(giro));
        }
    }

    // 0 = recién empieza el giro (1 s o menos), 1 = carga máxima (2 s o más)
    float FraccionCarga(float mantenido)
    {
        return Mathf.Clamp01((mantenido - segundosDañoMin) / (segundosDañoMax - segundosDañoMin));
    }

    // 1 s -> 1 corazón ... 2 s -> 4 corazones (redondeado a medios corazones)
    public float DañoGiro(float mantenido)
    {
        float d = Mathf.Lerp(dañoGiroMin, dañoGiroMax, FraccionCarga(mantenido));
        return Mathf.Round(d * 2f) / 2f;
    }

    // Círculo plano en el piso: crece y se pone rojo mientras se carga; parpadea al máximo
    void ActualizarIndicador(float f)
    {
        if (indicadorCarga == null)
        {
            indicadorCarga = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicadorCarga.name = "CargaGiro";
            Destroy(indicadorCarga.GetComponent<Collider>());
        }
        Collider col = GetComponent<Collider>();
        float piesY = col != null ? col.bounds.min.y + 0.03f : transform.position.y - 0.95f;
        indicadorCarga.transform.position = new Vector3(transform.position.x, piesY, transform.position.z);

        float radio = Mathf.Lerp(radioGiro, radioGiroMax, f);
        indicadorCarga.transform.localScale = new Vector3(radio * 2f, 0.01f, radio * 2f);

        Color c = Color.Lerp(colorGiro, colorCargaMax, f);
        if (f >= 1f && Mathf.Repeat(Time.time * 8f, 1f) > 0.5f) c = Color.white;   // carga completa
        indicadorCarga.GetComponent<Renderer>().material.color = c;
    }

    void CancelarCarga()
    {
        cargando = false;
        if (indicadorCarga != null) Destroy(indicadorCarga);
        OnCarga?.Invoke(0f);
    }

    // Link voltea al instante hacia donde mira la cámara (solo horizontal)
    void VoltearHaciaCamara()
    {
        Vector3 adelante = Camera.main.transform.forward;
        adelante.y = 0f;
        if (adelante == Vector3.zero) return;

        Quaternion rot = Quaternion.LookRotation(adelante);
        transform.rotation = rot;
        if (rb != null) rb.rotation = rot;
    }

    // Efecto simple: una figura plana del tamaño del golpe que desaparece en 0.12 s
    // Corte normal: media luna blanca que barre enfrente de Link (alterna diagonal).
    // Giro: anillo de fuego alrededor de Link; más intenso entre más carga (f de 0 a 1).
    void MostrarSlash(bool giro, float radio, float f)
    {
        if (!giro)
        {
            ladoCorte = !ladoCorte;
            float inclinacion = ladoCorte ? 25f : -25f;
            Vector3 centro = transform.position + Vector3.up * 0.25f;
            Quaternion rot = transform.rotation * Quaternion.Euler(0f, 0f, inclinacion);
            float desde = ladoCorte ? 80f : -80f;                 // barre de un lado al otro
            float r = alcance + radio * 0.6f;
            EfectoCorte.Crear(centro, rot, r, 0.55f, desde, ladoCorte ? -160f : 160f, 0.2f,
                              Color.white, colorSlash, true, materialEfectos);
        }
        else
        {
            Collider col = GetComponent<Collider>();
            float cintura = col != null ? Mathf.Lerp(col.bounds.min.y, col.bounds.center.y, 0.6f) : transform.position.y - 0.3f;
            Vector3 centro = new Vector3(transform.position.x, cintura, transform.position.z);
            Quaternion rot = Quaternion.Euler(6f, transform.eulerAngles.y, 0f);
            float duracion = Mathf.Lerp(0.3f, 0.4f, f);

            Color fuegoNucleo = Color.Lerp(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.8f, 0.3f), f);
            Color fuegoBorde = Color.Lerp(colorGiro, colorCargaMax, f);
            // Anillo exterior (llama) + anillo interior (brillo), giran en el mismo sentido que Link
            EfectoCorte.Crear(centro, rot, radio, Mathf.Lerp(0.7f, 1.2f, f), 0f, 400f, duracion,
                              fuegoNucleo, fuegoBorde, false, materialEfectos);
            EfectoCorte.Crear(centro + Vector3.up * 0.05f, rot * Quaternion.Euler(0f, 40f, 0f), radio * 0.75f, 0.35f, 0f, 380f, duracion * 0.9f,
                              Color.white, fuegoNucleo, false, materialEfectos);
            if (f >= 1f)   // carga completa: un tercer anillo más grande
                EfectoCorte.Crear(centro + Vector3.up * 0.1f, rot * Quaternion.Euler(0f, 90f, 0f), radio * 1.1f, 0.5f, 0f, 360f, duracion * 1.1f,
                                  new Color(1f, 0.6f, 0.2f), colorCargaMax, false, materialEfectos);
        }
    }

    // Daña a todos los enemigos dentro de una esfera
    void Golpear(Vector3 centro, float radio, float dañoGolpe, float empuje)
    {
        Collider[] golpeados = Physics.OverlapSphere(centro, radio);
        foreach (Collider c in golpeados)
        {
            Enemigo e = c.GetComponentInParent<Enemigo>();
            if (e == null) continue;

            e.GetComponent<Vida>().RecibirDaño(dañoGolpe, transform.position);
            e.Knockback(transform.position, empuje);
        }
    }

    // Sin animaciones: solo gira la espada un momento
    IEnumerator AnimarEspada(bool giro)
    {
        float duracion = giro ? 0.3f : 0.15f;
        float angulo = giro ? 360f : 90f;
        float t = 0f;
        Quaternion inicial = espadaVisual.localRotation;
        while (t < duracion)
        {
            Quaternion extra = Quaternion.Euler(0f, angulo * (t / duracion), 0f);
            if (animador != null) animador.giroExtraEspada = extra;   // el animador la aplica junto con el cuerpo
            else espadaVisual.localRotation = inicial * extra;
            t += Time.deltaTime;
            yield return null;
        }
        if (animador != null) animador.giroExtraEspada = Quaternion.identity;
        else espadaVisual.localRotation = inicial;
    }

    // Dibuja las zonas de golpe en la vista Scene (para ajustar valores)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * alcance, radioGolpe);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioGiro);
    }
}
