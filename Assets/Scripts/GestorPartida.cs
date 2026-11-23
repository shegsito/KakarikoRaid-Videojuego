using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

// Estado general de la partida: pausa (ESC), ganar cada 10 rondas (continuar o salir) y perder.
// Basado en el GestorPartida de FlashPoint_Graybox.
public class GestorPartida : MonoBehaviour
{
    [Header("Paneles (Canvas)")]
    public GameObject panelPausa;
    public GameObject panelGanar;
    public GameObject panelPerder;
    public TextMeshProUGUI textoGanar;     // subtítulo: "Sobreviviste 10 rondas"
    public TextMeshProUGUI textoPerder;    // subtítulo: "Llegaste a la ronda X"

    [Header("Reglas")]
    public int rondasParaGanar = 10;       // cada 10 rondas pregunta si seguir
    public string escenaMenu = "MainMenu";

    // Avisos para la música (GestorAudio) y quien los necesite
    public static event Action OnGanar;
    public static event Action OnPerder;
    public static event Action OnContinuar;

    private bool pausado;
    private bool terminada;                // ganó (esperando decisión) o perdió
    private int rondaActual;

    void Awake()
    {
        Time.timeScale = 1f;   // por si la escena se recarga después de congelarla
    }

    void OnEnable()
    {
        GestorOleadas.OnRondaInicia += AlIniciarRonda;
        GestorOleadas.OnRondaTerminada += AlTerminarRonda;
    }

    void OnDisable()
    {
        GestorOleadas.OnRondaInicia -= AlIniciarRonda;
        GestorOleadas.OnRondaTerminada -= AlTerminarRonda;
    }

    void Start()
    {
        AsegurarEventSystem.Revisar();
        ConectarBotones();
        LinkMovimiento link = FindFirstObjectByType<LinkMovimiento>();
        if (link != null) link.GetComponent<Vida>().OnMuerte += Perder;

        MostrarPanel(null);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !terminada)
        {
            if (pausado) Reanudar();
            else Pausar();
        }
    }

    void AlIniciarRonda(int ronda) { rondaActual = ronda; }

    void AlTerminarRonda(int ronda)
    {
        if (terminada || ronda % rondasParaGanar != 0) return;

        terminada = true;
        if (textoGanar != null) textoGanar.text = "Sobreviviste " + ronda + " rondas";
        Congelar(panelGanar);
        OnGanar?.Invoke();
    }

    void Perder()
    {
        if (terminada) return;
        terminada = true;
        if (textoPerder != null) textoPerder.text = "Llegaste a la ronda " + rondaActual;
        Congelar(panelPerder);
        OnPerder?.Invoke();
    }

    // --- Botones (se conectan en el OnClick de cada Button) ---

    public void Pausar()
    {
        GestorAudio.Reproducir(GestorAudio.Sfx.Boton);
        pausado = true;
        Congelar(panelPausa);
    }

    public void Reanudar()
    {
        GestorAudio.Reproducir(GestorAudio.Sfx.Boton);
        pausado = false;
        terminada = false;
        MostrarPanel(null);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Botón "Continuar" del panel de ganar: sigue con las mismas reglas
    public void Continuar()
    {
        Reanudar();
        OnContinuar?.Invoke();
    }

    // Botón "Retry": empieza desde cero
    public void Reintentar()
    {
        GestorAudio.Reproducir(GestorAudio.Sfx.Boton);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Botón "Salir": regresa al menú principal
    public void IrAlMenu()
    {
        GestorAudio.Reproducir(GestorAudio.Sfx.Boton);
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }

    // Conecta cada botón de los paneles por su nombre (BotonReanudar, BotonContinuar, BotonRetry, BotonSalir).
    // Si un botón ya tiene algo en su On Click (hecho a mano), se respeta.
    void ConectarBotones()
    {
        foreach (GameObject panel in new[] { panelPausa, panelGanar, panelPerder })
        {
            if (panel == null) continue;
            foreach (UnityEngine.UI.Button b in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (b.onClick.GetPersistentEventCount() > 0) continue;
                string n = b.name.ToLower();
                if (n.Contains("reanudar")) b.onClick.AddListener(Reanudar);
                else if (n.Contains("continuar")) b.onClick.AddListener(Continuar);
                else if (n.Contains("retry") || n.Contains("reintentar") || n.Contains("reiniciar")) b.onClick.AddListener(Reintentar);
                else if (n.Contains("salir") || n.Contains("menu")) b.onClick.AddListener(IrAlMenu);
            }
        }
    }

    // --- Utilidades ---

    void Congelar(GameObject panel)
    {
        Time.timeScale = 0f;
        MostrarPanel(panel);
        Cursor.lockState = CursorLockMode.None;   // para poder dar click a los botones
        Cursor.visible = true;
    }

    void MostrarPanel(GameObject panel)
    {
        if (panelPausa != null)  panelPausa.SetActive(panel == panelPausa);
        if (panelGanar != null)  panelGanar.SetActive(panel == panelGanar);
        if (panelPerder != null) panelPerder.SetActive(panel == panelPerder);
    }
}
