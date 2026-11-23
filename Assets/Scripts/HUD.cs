using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// HUD durante el juego: corazones, número de horda, arma actual, escudo y barra del jefe.
// Se arma solo con el menú Kakariko > Crear HUD (Assets/Editor/KakarikoMenu.cs).
public class HUD : MonoBehaviour
{
    [Header("Corazones")]
    public RectTransform contenedorCorazones;   // con HorizontalLayoutGroup
    public Sprite spriteCorazon;
    public Color colorLleno = new Color(0.9f, 0.1f, 0.15f);
    public Color colorVacio = new Color(0.25f, 0.25f, 0.25f, 0.8f);

    [Header("Textos")]
    public TextMeshProUGUI textoRonda;
    public TextMeshProUGUI textoEnemigos;       // "Enemigos: X" debajo de la horda
    public TextMeshProUGUI textoArma;
    public TextMeshProUGUI textoEscudo;
    [Header("Icono del arma")]
    public Image iconoArma;                     // muestra la espada o el arco
    public Sprite spriteEspada;
    public Sprite spriteArco;
    public GameObject mira;                     // crosshair al centro, solo con el arco
    public TextMeshProUGUI textoAviso;          // "¡Oleada superada!" al centro
    public float duracionAviso = 2.5f;

    [Header("Barra del jefe")]
    public GameObject barraJefe;               // se oculta si no hay Goblin
    public Image rellenoJefe;                  // Image tipo Filled

    private Vida vidaLink;
    private GestorOleadas oleadas;
    private int enemigosMostrados = -1;
    private LinkCombate combate;
    private LinkEscudo escudo;
    private readonly List<Image> rellenos = new List<Image>();
    private readonly List<Goblin> goblins = new List<Goblin>();

    void OnEnable()
    {
        GestorOleadas.OnRondaInicia += MostrarRonda;
        GestorOleadas.OnRondaTerminada += MostrarAviso;
        Goblin.OnGoblinAparece += AgregarGoblin;
    }

    void OnDisable()
    {
        GestorOleadas.OnRondaInicia -= MostrarRonda;
        GestorOleadas.OnRondaTerminada -= MostrarAviso;
        Goblin.OnGoblinAparece -= AgregarGoblin;
        if (vidaLink != null) vidaLink.OnCambio -= MostrarVida;
        if (combate != null) combate.OnArmaCambio -= MostrarArma;
        if (escudo != null) escudo.OnEscudoCambio -= MostrarEscudo;
    }

    void Update()
    {
        // Enemigos restantes (solo se reescribe el texto cuando cambia)
        if (textoEnemigos == null || oleadas == null) return;
        if (oleadas.Vivos != enemigosMostrados)
        {
            enemigosMostrados = oleadas.Vivos;
            textoEnemigos.text = "Enemigos: " + enemigosMostrados;
        }
    }

    void Start()
    {
        oleadas = FindFirstObjectByType<GestorOleadas>();
        LinkMovimiento link = FindFirstObjectByType<LinkMovimiento>();
        if (link != null)
        {
            vidaLink = link.GetComponent<Vida>();
            combate = link.GetComponent<LinkCombate>();
            escudo = link.GetComponent<LinkEscudo>();
        }

        // Suscribirse y pintar el estado actual (por si los eventos ya se dispararon)
        if (vidaLink != null)
        {
            CrearCorazones(Mathf.CeilToInt(vidaLink.vidaMaxima));
            vidaLink.OnCambio += MostrarVida;
            MostrarVida(vidaLink.Actual, vidaLink.vidaMaxima);
        }
        if (combate != null)
        {
            combate.OnArmaCambio += MostrarArma;
            MostrarArma(combate.armaActual);
        }
        if (escudo != null)
        {
            escudo.OnEscudoCambio += MostrarEscudo;
            MostrarEscudo(escudo.golpesMaximos, false);
        }
        else if (textoEscudo != null) textoEscudo.gameObject.SetActive(false);

        if (barraJefe != null) barraJefe.SetActive(false);
        if (textoRonda != null) textoRonda.text = "";
        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
    }

    // --- Aviso de ronda superada ---
    void MostrarAviso(int ronda)
    {
        if (textoAviso == null) return;
        textoAviso.text = "¡Oleada superada!\n<size=60%>Ronda " + ronda + "</size>";
        textoAviso.gameObject.SetActive(true);
        CancelInvoke(nameof(OcultarAviso));
        Invoke(nameof(OcultarAviso), duracionAviso);
    }

    void OcultarAviso()
    {
        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
    }

    // --- Corazones ---
    void CrearCorazones(int cantidad)
    {
        foreach (Transform hijo in contenedorCorazones) Destroy(hijo.gameObject);
        rellenos.Clear();

        for (int i = 0; i < cantidad; i++)
        {
            // Fondo (corazón vacío)
            // (el tamaño lo controla el GridLayoutGroup del contenedor)
            GameObject fondo = new GameObject("Corazon" + (i + 1), typeof(RectTransform), typeof(Image));
            fondo.transform.SetParent(contenedorCorazones, false);
            Image imgFondo = fondo.GetComponent<Image>();
            imgFondo.sprite = spriteCorazon;
            imgFondo.color = colorVacio;

            // Relleno (se llena de izquierda a derecha: 0, 0.5 o 1)
            GameObject relleno = new GameObject("Relleno", typeof(RectTransform), typeof(Image));
            relleno.transform.SetParent(fondo.transform, false);
            RectTransform rt = relleno.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            Image img = relleno.GetComponent<Image>();
            img.sprite = spriteCorazon;
            img.color = colorLleno;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            rellenos.Add(img);
        }
    }

    void MostrarVida(float actual, float maxima)
    {
        if (rellenos.Count != Mathf.CeilToInt(maxima)) CrearCorazones(Mathf.CeilToInt(maxima));
        for (int i = 0; i < rellenos.Count; i++)
            rellenos[i].fillAmount = Mathf.Clamp01(actual - i);   // 1 = lleno, 0.5 = medio, 0 = vacío
    }

    // --- Textos ---
    void MostrarRonda(int ronda)
    {
        if (textoRonda != null) textoRonda.text = "Horda " + ronda;
    }

    void MostrarArma(LinkCombate.Arma arma)
    {
        if (textoArma != null) textoArma.text = arma == LinkCombate.Arma.Espada ? "Espada" : "Arco";
        if (iconoArma != null)
        {
            Sprite s = arma == LinkCombate.Arma.Espada ? spriteEspada : spriteArco;
            if (s != null) iconoArma.sprite = s;
            iconoArma.preserveAspect = true;
        }
        if (mira != null) mira.SetActive(arma == LinkCombate.Arma.Arco);
    }

    void MostrarEscudo(int restantes, bool roto)
    {
        if (textoEscudo == null) return;
        int max = escudo != null ? escudo.golpesMaximos : restantes;
        textoEscudo.text = roto ? "Escudo roto" : "Escudo " + restantes + "/" + max;
        textoEscudo.color = roto ? new Color(1f, 0.4f, 0.4f) : Color.white;
    }

    // --- Barra del jefe (suma la vida de todos los Goblins vivos) ---
    void AgregarGoblin(Goblin g)
    {
        goblins.Add(g);
        g.VidaJefe.OnCambio += (a, m) => ActualizarJefe();
        g.VidaJefe.OnMuerte += () => { goblins.Remove(g); ActualizarJefe(); };
        ActualizarJefe();
    }

    void ActualizarJefe()
    {
        goblins.RemoveAll(g => g == null);
        if (barraJefe == null) return;

        float actual = 0f, maxima = 0f;
        foreach (Goblin g in goblins)
        {
            if (g.VidaJefe.Muerto) continue;
            actual += g.VidaJefe.Actual;
            maxima += g.VidaJefe.vidaMaxima;
        }

        barraJefe.SetActive(maxima > 0f);
        if (rellenoJefe != null && maxima > 0f) rellenoJefe.fillAmount = actual / maxima;
    }
}
