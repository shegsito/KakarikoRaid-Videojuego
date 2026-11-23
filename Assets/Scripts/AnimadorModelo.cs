using UnityEngine;

// Anima el modelo 3D (hijo "Modelo3D") de Link o de un enemigo.
// 1) Si el modelo trae clips con movimiento (dragón esqueleto): idle / walk / fly / attack.
// 2) Si tiene huesos de humanoide (esqueleto Mixamo): mueve piernas, brazos y columna por código.
// 3) Si no tiene nada (Link, Goblin): anima todo el cuerpo por código (pasos, respiración, ataques).
// Se agrega solo desde Enemigo y LinkCombate; no hace falta ponerlo a mano.
public class AnimadorModelo : MonoBehaviour
{
    public enum TipoAtaque { Golpe, Lanzar }

    public Transform modelo;                 // si está vacío busca "Modelo3D"
    public bool volador = false;             // Dragona: usa "fly" y flota
    public bool pesado = false;              // Goblin: pasos lentos y fuertes, ataques con impulso
    public float velocidadParaCaminar = 0.3f;

    [Header("Animación por código")]
    public float alturaPaso = 0.06f;         // fracción de la altura del modelo
    public float pasosPorSegundo = 2.2f;
    public float inclinacionPaso = 7f;
    public float embestida = 0.35f;
    public float duracionAtaque = 0.3f;
    public float giroPiernas = 35f;          // grados que se balancean las piernas (humanoide)
    public float giroBrazos = 30f;

    [HideInInspector] public bool cubriendose;   // Link con el escudo arriba (lo pone LinkEscudo)
    [HideInInspector] public Quaternion giroExtraEspada = Quaternion.identity;   // lo usa LinkCombate al tajar

    // Espada / escudo / arco de Link: se mueven junto con el cuerpo
    private Transform[] armas;
    private Vector3[] armasPos;
    private Quaternion[] armasRot;

    // Clips
    private Animation anim;
    private string clipIdle, clipCaminar, clipVolar, clipAtacar;

    // Huesos (humanoide)
    private Transform piernaI, piernaD, rodillaI, rodillaD, brazoI, brazoD, antebrazoI, antebrazoD, columna, cabeza;
    private Quaternion rPiernaI, rPiernaD, rRodillaI, rRodillaD, rBrazoI, rBrazoD, rAntebrazoD, rAntebrazoI, rColumna, rCabeza;
    private bool humanoide;

    private Rigidbody rb;
    private Transform pivoteGiro;
    private Vector3 basePos;
    private Quaternion baseRot, baseRotGiro;
    private float altura = 1f;
    private float fase;
    private float inicioAtaque = -10f;
    private TipoAtaque tipoAtaque;
    private float inicioGiro = -10f, duracionGiro;
    private float inicioRugido = -10f;
    private float agacharse;                 // 0..1 suavizado

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (modelo == null) modelo = BuscarHijo(transform, "Modelo3D");
        if (modelo == null) { enabled = false; return; }

        basePos = modelo.localPosition;
        baseRot = modelo.localRotation;
        pivoteGiro = (modelo.parent != null && modelo.parent != transform) ? modelo.parent : modelo;
        baseRotGiro = pivoteGiro.localRotation;

        Bounds b = new Bounds(modelo.position, Vector3.zero);
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        altura = Mathf.Max(0.2f, b.size.y);

        BuscarClips();
        BuscarHuesos();
        BuscarArmas();
    }

    void BuscarArmas()
    {
        Transform padre = modelo.parent;
        var lista = new System.Collections.Generic.List<Transform>();
        foreach (string n in new[] { "Espada3D", "Escudo3D", "Arco3D" })
        {
            Transform a = padre != null ? padre.Find(n) : null;
            if (a != null) lista.Add(a);
        }
        armas = lista.ToArray();
        armasPos = new Vector3[armas.Length];
        armasRot = new Quaternion[armas.Length];
        for (int i = 0; i < armas.Length; i++) { armasPos[i] = armas[i].localPosition; armasRot[i] = armas[i].localRotation; }
    }

    // ---------------- Clips ----------------
    void BuscarClips()
    {
        anim = modelo.GetComponentInChildren<Animation>();
        if (anim == null) return;

        foreach (AnimationState st in anim)
        {
            if (st.length < 0.05f) continue;   // una "animación" de 0 s es solo una pose: no sirve
            string n = st.name.ToLower();
            if (clipAtacar == null && (n.Contains("attack") || n.Contains("bite") || n.Contains("ataque"))) clipAtacar = st.name;
            else if (clipVolar == null && n.Contains("fly")) clipVolar = st.name;
            else if (clipCaminar == null && (n.Contains("walk") || n.Contains("run") || n.Contains("camina"))) clipCaminar = st.name;
            else if (clipIdle == null && (n.Contains("idle") || n.Contains("quieto"))) clipIdle = st.name;
        }

        foreach (string c in new[] { clipIdle, clipCaminar, clipVolar })
            if (c != null) anim[c].wrapMode = WrapMode.Loop;
        if (clipAtacar != null) { anim[clipAtacar].wrapMode = WrapMode.Once; anim[clipAtacar].layer = 1; }

        anim.playAutomatically = false;
        anim.Stop();
        if (clipIdle == null && clipCaminar == null && clipVolar == null && clipAtacar == null) anim.enabled = false;
    }

    // ---------------- Huesos (Mixamo y similares) ----------------
    void BuscarHuesos()
    {
        if (clipCaminar != null) return;   // ya tiene animación real de caminar
        foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLower();
            if (n.Contains("leftupleg")) piernaI = t;
            else if (n.Contains("rightupleg")) piernaD = t;
            else if (n.Contains("leftleg") && rodillaI == null) rodillaI = t;
            else if (n.Contains("rightleg") && rodillaD == null) rodillaD = t;
            else if (n.Contains("leftarm") && !n.Contains("fore")) brazoI = t;
            else if (n.Contains("rightarm") && !n.Contains("fore")) brazoD = t;
            else if (n.Contains("rightforearm")) antebrazoD = t;
            else if (n.Contains("leftforearm")) antebrazoI = t;
            else if (n.Contains("spine") && columna == null) columna = t;
            else if (n.Contains("head") && !n.Contains("top") && cabeza == null) cabeza = t;
        }
        humanoide = piernaI != null && piernaD != null && brazoI != null && brazoD != null;
        if (!humanoide) return;

        rPiernaI = piernaI.localRotation; rPiernaD = piernaD.localRotation;
        if (rodillaI) rRodillaI = rodillaI.localRotation;
        if (rodillaD) rRodillaD = rodillaD.localRotation;
        // Mixamo deja el modelo en pose de "T" (brazos estirados): bajarlos para que cuelguen
        BajarBrazo(brazoI, antebrazoI);
        BajarBrazo(brazoD, antebrazoD);

        rBrazoI = brazoI.localRotation; rBrazoD = brazoD.localRotation;
        if (antebrazoD) rAntebrazoD = antebrazoD.localRotation;
        if (antebrazoI) rAntebrazoI = antebrazoI.localRotation;
        if (columna) rColumna = columna.localRotation;
        if (cabeza) rCabeza = cabeza.localRotation;
    }

    // Si el brazo apunta hacia el lado (pose T), lo gira para que cuelgue hacia abajo y un poco hacia afuera
    void BajarBrazo(Transform brazo, Transform antebrazo)
    {
        if (brazo == null || antebrazo == null) return;
        Vector3 dir = (antebrazo.position - brazo.position).normalized;
        if (Vector3.Angle(dir, Vector3.down) < 45f) return;     // ya cuelga

        float lado = Mathf.Sign(Vector3.Dot(dir, transform.right));
        Vector3 objetivo = (Vector3.down * 0.94f + transform.right * lado * 0.28f + transform.forward * 0.12f).normalized;
        brazo.rotation = Quaternion.FromToRotation(dir, objetivo) * brazo.rotation;
    }

    // Zarpazo: primero echa el brazo hacia atrás (carga) y luego lo lanza hacia adelante y arriba
    static float Zarpazo(float k)
    {
        if (k < 0f || k >= 1f) return 0f;
        float v = k < 0.35f
            ? 45f * Mathf.Sin(k / 0.35f * Mathf.PI * 0.5f)                               // carga: hacia atrás (+)
            : 45f - 175f * Mathf.Sin(Mathf.Clamp01((k - 0.35f) / 0.4f) * Mathf.PI * 0.5f); // golpe: adelante y arriba (-)
        return v * (1f - Mathf.Clamp01((k - 0.8f) / 0.2f));                              // regresa suave al final
    }

    // Gira un hueso 'grados' alrededor de un eje del personaje (no del hueso): sirve sin importar cómo esté armado
    void Girar(Transform hueso, Quaternion reposo, Vector3 ejeMundo, float grados)
    {
        if (hueso == null) return;
        hueso.localRotation = reposo;
        hueso.rotation = Quaternion.AngleAxis(grados, ejeMundo) * hueso.rotation;
    }

    // ---------------- Llamados desde otros scripts ----------------
    public void Atacar() { Atacar(TipoAtaque.Golpe); }

    public void Atacar(TipoAtaque tipo)
    {
        tipoAtaque = tipo;
        inicioAtaque = Time.time;
        if (anim != null && clipAtacar != null) anim.CrossFade(clipAtacar, 0.05f);
    }

    public void Girar(float duracion)
    {
        inicioGiro = Time.time;
        duracionGiro = Mathf.Max(0.05f, duracion);
    }

    public void Rugir() { inicioRugido = Time.time; }

    // ---------------- Cada frame ----------------
    void LateUpdate()
    {
        if (modelo == null) return;

        Vector3 vel = rb != null ? rb.linearVelocity : Vector3.zero;
        vel.y = 0f;
        float rapidez = vel.magnitude;
        bool moviendose = rapidez > velocidadParaCaminar;
        float ritmo = Mathf.Clamp(rapidez / 4f, 0.7f, 2f) * (pesado ? 0.6f : 1f);

        // 1) Clips
        string loop = volador ? (clipVolar ?? clipCaminar ?? clipIdle)
                              : (moviendose ? (clipCaminar ?? clipIdle) : (clipIdle ?? clipCaminar));
        if (anim != null && anim.enabled && loop != null)
        {
            if (!anim.IsPlaying(loop)) anim.CrossFade(loop, 0.2f);
            anim[loop].speed = (volador || !moviendose) ? 1f : ritmo;
        }

        if (moviendose) fase += Time.deltaTime * pasosPorSegundo * Mathf.PI * ritmo;
        else fase = Mathf.MoveTowards(fase, Mathf.Round(fase / Mathf.PI) * Mathf.PI, Time.deltaTime * 6f);

        // Ataque: k de 0 a 1. Pesado = se echa hacia atrás (impulso) y luego golpea fuerte.
        float dur = duracionAtaque * (pesado ? 2.2f : 1f);
        float ka = (Time.time - inicioAtaque) / dur;
        bool atacando = ka >= 0f && ka < 1f && clipAtacar == null;
        float golpe = 0f;   // -1 (atrás, cargando) .. 1 (adelante, impacto)
        if (atacando)
            golpe = pesado ? (ka < 0.45f ? -Mathf.Sin(ka / 0.45f * Mathf.PI * 0.5f) : Mathf.Sin((ka - 0.45f) / 0.55f * Mathf.PI))
                           : Mathf.Sin(ka * Mathf.PI);

        // 2) Huesos (esqueleto)
        if (humanoide)
        {
            Vector3 lado = transform.right, adelante = transform.forward;
            float s = moviendose ? Mathf.Sin(fase) : 0f;
            Girar(piernaI, rPiernaI, lado, s * giroPiernas);
            Girar(piernaD, rPiernaD, lado, -s * giroPiernas);
            Girar(rodillaI, rRodillaI, lado, Mathf.Max(0f, -s) * giroPiernas * 1.2f);
            Girar(rodillaD, rRodillaD, lado, Mathf.Max(0f, s) * giroPiernas * 1.2f);
            // Brazos: se balancean al revés que las piernas (+ = hacia atrás, - = hacia adelante),
            // traquetean un poco (huesos sueltos) y al atacar dan un zarpazo con los dos (el izquierdo un poco después)
            float traqI = (Mathf.PerlinNoise(Time.time * 7f, 1.3f) - 0.5f) * (moviendose ? 10f : 6f);
            float traqD = (Mathf.PerlinNoise(Time.time * 7f, 7.9f) - 0.5f) * (moviendose ? 10f : 6f);
            float zarpazoD = atacando ? Zarpazo(ka) : 0f;
            float zarpazoI = atacando ? Zarpazo(ka - 0.12f) : 0f;
            float balanceo = s * giroBrazos;
            Girar(brazoI, rBrazoI, lado, -balanceo * (atacando ? 0.3f : 1f) + zarpazoI + traqI);
            Girar(brazoD, rBrazoD, lado, balanceo * (atacando ? 0.3f : 1f) + zarpazoD + traqD);

            // Codos: algo doblados al caminar; se doblan más al lanzar el zarpazo
            float codoBase = moviendose ? 20f + Mathf.Abs(s) * 15f : 12f;
            if (antebrazoI) Girar(antebrazoI, rAntebrazoI, lado, -(codoBase + Mathf.Max(0f, -zarpazoI) * 0.35f));
            if (antebrazoD) Girar(antebrazoD, rAntebrazoD, lado, -(codoBase + Mathf.Max(0f, -zarpazoD) * 0.35f));
            // Columna: se inclina al caminar/atacar; la cabeza traquetea (huesos sueltos)
            if (columna) Girar(columna, rColumna, lado, (moviendose ? 6f : 0f) + golpe * 15f);
            if (cabeza) Girar(cabeza, rCabeza, adelante, Mathf.Sin(Time.time * 9f) * (moviendose ? 6f : 2f));
        }

        // 3) Cuerpo completo por código
        Vector3 pos = basePos;
        Quaternion rot = baseRot;

        if (volador && clipVolar == null)
        {
            pos.y += Mathf.Sin(Time.time * 3f) * altura * 0.08f;
        }
        else if (!volador && moviendose && clipCaminar == null)
        {
            float paso = Mathf.Abs(Mathf.Sin(fase));
            if (pesado) paso = Mathf.Pow(paso, 0.5f);                 // pisada fuerte: baja de golpe
            pos.y += paso * altura * alturaPaso * (humanoide ? 0.5f : 1f) * (pesado ? 0.7f : 1f);
            rot = Quaternion.Euler(0f, 0f, Mathf.Sin(fase) * inclinacionPaso * (pesado ? 1.3f : 1f)) * rot;
        }
        else if (!moviendose && clipIdle == null && !volador)
        {
            pos.y += Mathf.Sin(Time.time * (pesado ? 1.3f : 2f)) * altura * 0.012f;   // respiración
        }

        // Ataque sin clip: el cuerpo entero empuja (o se echa atrás y golpea, si es pesado)
        if (atacando && !humanoide)
        {
            float lanzar = tipoAtaque == TipoAtaque.Lanzar ? 1.4f : 1f;
            pos += Vector3.forward * golpe * altura * embestida * (pesado ? 0.5f : 1f);
            rot = Quaternion.Euler(golpe * (pesado ? 20f : 15f) * lanzar, 0f, 0f) * rot;
            if (pesado && tipoAtaque == TipoAtaque.Golpe && ka > 0.6f && ka < 0.75f)
                pos.y -= altura * 0.04f;                                    // impacto contra el suelo
        }

        // Rugido (Goblin al aparecer): se echa atrás y tiembla
        float kr = (Time.time - inicioRugido) / 1.4f;
        if (kr >= 0f && kr < 1f)
        {
            float fuerza = Mathf.Sin(kr * Mathf.PI);
            rot = Quaternion.Euler(-14f * fuerza, Mathf.Sin(Time.time * 40f) * 2.5f * fuerza, 0f) * rot;
            pos.y += fuerza * altura * 0.03f;
        }

        // Cubrirse (Link con el escudo): se agacha un poco y se echa para atrás
        agacharse = Mathf.MoveTowards(agacharse, cubriendose ? 1f : 0f, Time.deltaTime * 8f);
        if (agacharse > 0f)
        {
            pos.y -= agacharse * altura * 0.08f;
            rot = Quaternion.Euler(-8f * agacharse, 0f, 0f) * rot;
        }

        modelo.localPosition = pos;
        modelo.localRotation = rot;

        // Las armas copian el mismo movimiento del cuerpo (como si estuvieran pegadas a él)
        Quaternion delta = rot * Quaternion.Inverse(baseRot);
        for (int i = 0; i < armas.Length; i++)
        {
            if (armas[i] == null) continue;
            Vector3 p = pos + delta * (armasPos[i] - basePos);
            Quaternion r = delta * armasRot[i];
            if (armas[i].name == "Escudo3D") p += Vector3.forward * 0.12f * agacharse;   // al cubrirse lo adelanta
            if (armas[i].name == "Espada3D") r = r * giroExtraEspada;
            armas[i].localPosition = p;
            armas[i].localRotation = r;
        }

        // Giro completo (ataque circular de Link)
        float kg = (Time.time - inicioGiro) / duracionGiro;
        if (kg >= 0f && kg < 1f)
        {
            float suave = 1f - Mathf.Pow(1f - kg, 3f);
            pivoteGiro.localRotation = Quaternion.Euler(0f, 360f * suave, 0f) * baseRotGiro;
        }
        else if (kg >= 1f && kg < 1.5f)
        {
            pivoteGiro.localRotation = baseRotGiro;
        }
    }

    static Transform BuscarHijo(Transform t, string nombre)
    {
        foreach (Transform h in t.GetComponentsInChildren<Transform>(true))
            if (h.name == nombre) return h;
        return null;
    }
}
