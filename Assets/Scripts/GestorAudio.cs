using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Música del juego (y más adelante los efectos). Vive entre escenas (DontDestroyOnLoad).
// Se crea solo desde Resources/GestorAudio (lo arma el menú Kakariko > Configurar música),
// así que no hay que ponerlo en ninguna escena.
//   MainMenu          -> música de menú
//   Game (ronda)      -> música de ronda
//   Aparece un Goblin -> música de jefe (regresa a ronda cuando mueren todos)
//   Perder / Ganar    -> música de perder / ganar
public class GestorAudio : MonoBehaviour
{
    public static GestorAudio Instancia { get; private set; }

    [Header("Música")]
    public AudioClip musicaMenu;
    public AudioClip musicaRonda;
    public AudioClip musicaJefe;
    public AudioClip musicaPerder;
    public AudioClip musicaGanar;
    [Range(0f, 1f)] public float volumenMusica = 0.5f;
    public float tiempoCambio = 1f;          // segundos de transición entre canciones

    // ---------- Efectos de sonido ----------
    public enum Sfx { Boton, Espadazo, EspadazoCircular, DisparoArco, DisparoDragona, GoblinDisparo, GolpeRecibido, GolpeEscudo, RugidoGoblin, VozDanio, VozMorir, VozSaltar, VozGiro }

    [Header("Efectos de sonido")]
    public AudioClip boton;
    public AudioClip espadazo;
    public AudioClip espadazoCircular;
    public AudioClip disparoArco;
    public AudioClip disparoDragona;
    public AudioClip goblinDisparo;
    public AudioClip golpeRecibido;
    public AudioClip golpeEscudo;
    public AudioClip rugidoGoblin;

    [Header("Voz de Link")]
    public AudioClip vozDanio;              // al recibir daño
    public AudioClip vozMorir;              // al morir
    public AudioClip vozSaltar;             // al saltar
    public AudioClip vozGiro;               // grito del ataque circular (SpinAtack)
    [Header("Voz de Link al atacar (en orden: 1, 2, ... 7, 1, 2 ...)")]
    public AudioClip[] vozAtaque;
    [Range(0f, 1f)] public float volumenVoz = 1f;
    private int siguienteVoz;

    [Range(0f, 1f)] public float volumenEfectos = 0.8f;
    public float variacionTono = 0.08f;      // cada golpe suena un poquito distinto

    private AudioSource[] fuentesSfx;        // varias para que no se corten entre sí
    private int siguienteSfx;

    [Header("Escenas")]
    public string escenaMenu = "MainMenu";
    public string escenaJuego = "Game";

    private AudioSource[] fuentes;           // dos para hacer transición (crossfade)
    private int activa;
    private Coroutine transicion;
    private int goblinsVivos;
    private bool partidaTerminada;

    // Se crea automáticamente al arrancar el juego (en cualquier escena)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CrearAlIniciar()
    {
        if (Instancia != null) return;
        GestorAudio prefab = Resources.Load<GestorAudio>("GestorAudio");
        if (prefab == null) return;   // todavía no se configuró la música
        Instantiate(prefab).name = "GestorAudio";
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        DontDestroyOnLoad(gameObject);

        fuentes = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            fuentes[i] = gameObject.AddComponent<AudioSource>();
            fuentes[i].playOnAwake = false;
            fuentes[i].loop = true;
            fuentes[i].volume = 0f;
            fuentes[i].ignoreListenerPause = true;
        }

        fuentesSfx = new AudioSource[8];
        for (int i = 0; i < fuentesSfx.Length; i++)
        {
            fuentesSfx[i] = gameObject.AddComponent<AudioSource>();
            fuentesSfx[i].playOnAwake = false;
            fuentesSfx[i].ignoreListenerPause = true;
        }
    }

    // Uso desde cualquier script:  GestorAudio.Reproducir(GestorAudio.Sfx.Espadazo);
    public static void Reproducir(Sfx sonido, float volumen = 1f)
    {
        if (Instancia == null) return;
        bool esVoz = sonido == Sfx.VozDanio || sonido == Sfx.VozMorir || sonido == Sfx.VozSaltar || sonido == Sfx.VozGiro;
        Instancia.ReproducirClip(Instancia.Clip(sonido), volumen, !esVoz);
    }

    // Cada ataque de Link toca la siguiente voz de la lista y al final vuelve a empezar
    public static void VozAtaque()
    {
        if (Instancia == null || Instancia.vozAtaque == null || Instancia.vozAtaque.Length == 0) return;
        GestorAudio g = Instancia;
        AudioClip clip = g.vozAtaque[g.siguienteVoz % g.vozAtaque.Length];
        g.siguienteVoz = (g.siguienteVoz + 1) % g.vozAtaque.Length;
        if (clip == null) return;
        AudioSource f = g.fuentesSfx[g.siguienteSfx];
        g.siguienteSfx = (g.siguienteSfx + 1) % g.fuentesSfx.Length;
        f.pitch = 1f;   // la voz sin variar el tono
        f.PlayOneShot(clip, g.volumenVoz);
    }

    AudioClip Clip(Sfx s)
    {
        switch (s)
        {
            case Sfx.Boton: return boton;
            case Sfx.Espadazo: return espadazo;
            case Sfx.EspadazoCircular: return espadazoCircular;
            case Sfx.DisparoArco: return disparoArco;
            case Sfx.DisparoDragona: return disparoDragona;
            case Sfx.GoblinDisparo: return goblinDisparo;
            case Sfx.GolpeRecibido: return golpeRecibido;
            case Sfx.GolpeEscudo: return golpeEscudo;
            case Sfx.RugidoGoblin: return rugidoGoblin;
            case Sfx.VozDanio: return vozDanio;
            case Sfx.VozMorir: return vozMorir;
            case Sfx.VozSaltar: return vozSaltar;
            case Sfx.VozGiro: return vozGiro;
        }
        return null;
    }

    void ReproducirClip(AudioClip clip, float volumen, bool variar)
    {
        if (clip == null) return;
        AudioSource f = fuentesSfx[siguienteSfx];
        siguienteSfx = (siguienteSfx + 1) % fuentesSfx.Length;
        f.pitch = variar ? 1f + Random.Range(-variacionTono, variacionTono) : 1f;   // la voz no cambia de tono
        f.PlayOneShot(clip, (variar ? volumenEfectos : volumenVoz) * volumen);
    }

    void Start()
    {
        // Por si la primera escena se cargó antes de suscribirse
        if (!fuentes[activa].isPlaying) AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
        Goblin.OnGoblinAparece += AlAparecerGoblin;
        Enemigo.OnEnemigoMuerto += AlMorirEnemigo;
        GestorPartida.OnGanar += AlGanar;
        GestorPartida.OnPerder += AlPerder;
        GestorPartida.OnContinuar += AlContinuar;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        Goblin.OnGoblinAparece -= AlAparecerGoblin;
        Enemigo.OnEnemigoMuerto -= AlMorirEnemigo;
        GestorPartida.OnGanar -= AlGanar;
        GestorPartida.OnPerder -= AlPerder;
        GestorPartida.OnContinuar -= AlContinuar;
    }

    // --- Eventos del juego ---
    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        goblinsVivos = 0;
        partidaTerminada = false;
        if (escena.name == escenaMenu) Tocar(musicaMenu, true);
        else if (escena.name == escenaJuego) Tocar(musicaRonda, true);
    }

    void AlAparecerGoblin(Goblin g)
    {
        goblinsVivos++;
        if (!partidaTerminada) Tocar(musicaJefe, true);
    }

    void AlMorirEnemigo(Enemigo e)
    {
        if (!(e is Goblin)) return;
        goblinsVivos = Mathf.Max(0, goblinsVivos - 1);
        if (goblinsVivos == 0 && !partidaTerminada) Tocar(musicaRonda, true);
    }

    void AlGanar()     { partidaTerminada = true;  Tocar(musicaGanar, false); }
    void AlPerder()    { partidaTerminada = true;  Tocar(musicaPerder, false); }
    void AlContinuar() { partidaTerminada = false; Tocar(goblinsVivos > 0 ? musicaJefe : musicaRonda, true); }

    // --- Reproducción con transición ---
    public void Tocar(AudioClip clip, bool repetir)
    {
        if (clip == null) return;
        AudioSource actual = fuentes[activa];
        if (actual.clip == clip && actual.isPlaying) return;   // ya está sonando

        activa = 1 - activa;
        AudioSource nueva = fuentes[activa];
        nueva.clip = clip;
        nueva.loop = repetir;
        nueva.volume = 0f;
        nueva.Play();

        if (transicion != null) StopCoroutine(transicion);
        transicion = StartCoroutine(Transicion(actual, nueva));
    }

    IEnumerator Transicion(AudioSource sale, AudioSource entra)
    {
        float t = 0f;
        float volSale = sale.volume;
        while (t < tiempoCambio)
        {
            t += Time.unscaledDeltaTime;   // funciona aunque el juego esté en pausa (Time.timeScale = 0)
            float k = Mathf.Clamp01(t / tiempoCambio);
            sale.volume = Mathf.Lerp(volSale, 0f, k);
            entra.volume = Mathf.Lerp(0f, volumenMusica, k);
            yield return null;
        }
        sale.Stop();
        sale.volume = 0f;
        entra.volume = volumenMusica;
    }
}
