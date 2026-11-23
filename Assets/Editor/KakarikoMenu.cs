using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

// Menú "Kakariko" en la barra de Unity: arma automáticamente partes del juego en la escena abierta.
public static class KakarikoMenu
{
    const string CarpetaUI = "Assets/UI";

    // ==================== Protección: no usar en Play ====================
    // Unity borra al detener el juego todo lo que se cree en Play, así que los menús se desactivan.
    [MenuItem("Kakariko/Crear HUD", true)]
    [MenuItem("Kakariko/Rehacer paneles (Pausa, Ganar, Perder)", true)]
    [MenuItem("Kakariko/Crear Menú Principal (escena MainMenu)", true)]
    [MenuItem("Kakariko/Aplicar valores del GDD", true)]
    static bool FueraDePlay() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    static bool EnPlay()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) return false;
        EditorUtility.DisplayDialog("Kakariko", "Detén el juego (botón Play) antes de usar este menú.\nLo que se crea en Play se borra al detenerlo.", "OK");
        return true;
    }

    // ==================== HUD ====================
    [MenuItem("Kakariko/Crear HUD")]
    public static void CrearHUD()
    {
        if (EnPlay()) return;
        Sprite corazon = CrearSpriteCorazon();
        Sprite blanco = CrearSpriteBlanco();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject goCanvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(goCanvas, "Crear Canvas");
            canvas = goCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        ConfigurarEscala(canvas);

        // Reemplazar un HUD anterior hecho por este menú
        Transform viejo = canvas.transform.Find("HUD");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        // Raíz del HUD (ocupa toda la pantalla, va detrás de los paneles)
        RectTransform raiz = CrearUI("HUD", canvas.transform);
        Estirar(raiz);
        raiz.SetAsFirstSibling();
        HUD hud = raiz.gameObject.AddComponent<HUD>();

        // Corazones: esquina superior izquierda, filas de 10 (como en Zelda)
        RectTransform corazones = CrearUI("Corazones", raiz);
        Anclar(corazones, new Vector2(0, 1), new Vector2(40, -40), new Vector2(10 * 50, 2 * 50));
        GridLayoutGroup grid = corazones.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(44, 44);
        grid.spacing = new Vector2(6, 6);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 10;

        // Aviso al terminar una ronda: al centro
        TextMeshProUGUI aviso = CrearTexto("TextoAviso", raiz, "¡Oleada superada!", 64, TextAlignmentOptions.Center);
        Anclar(aviso.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1200, 180));

        // Horda: arriba al centro
        TextMeshProUGUI ronda = CrearTexto("TextoRonda", raiz, "Horda 1", 56, TextAlignmentOptions.Center);
        Anclar(ronda.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(600, 70));

        // Enemigos restantes: debajo de la horda
        TextMeshProUGUI enemigos = CrearTexto("TextoEnemigos", raiz, "Enemigos: 0", 32, TextAlignmentOptions.Center);
        enemigos.fontStyle = FontStyles.Normal;
        Anclar(enemigos.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(600, 45));
        hud.textoEnemigos = enemigos;

        // Arma: icono (espada/arco) en un marco abajo a la derecha; el escudo encima
        RectTransform marco = CrearUI("MarcoArma", raiz);
        Anclar(marco, new Vector2(1, 0), new Vector2(-40, 40), new Vector2(150, 150));
        Image imgMarco = marco.gameObject.AddComponent<Image>();
        imgMarco.sprite = blanco;
        imgMarco.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform icono = CrearUI("IconoArma", marco);
        Estirar(icono, 14);
        Image imgIcono = icono.gameObject.AddComponent<Image>();
        imgIcono.sprite = ImportarSprite(CarpetaUI + "/icono_espada.png");
        imgIcono.preserveAspect = true;
        imgIcono.raycastTarget = false;
        TextMeshProUGUI teclaF = CrearTexto("TeclaF", marco, "F", 28, TextAlignmentOptions.TopLeft);
        Anclar(teclaF.rectTransform, new Vector2(0, 1), new Vector2(8, -4), new Vector2(40, 40));

        TextMeshProUGUI escudo = CrearTexto("TextoEscudo", raiz, "Escudo 5/5", 32, TextAlignmentOptions.Right);
        Anclar(escudo.rectTransform, new Vector2(1, 0), new Vector2(-40, 200), new Vector2(500, 45));

        // Barra del jefe: arriba al centro, debajo de la horda
        RectTransform barra = CrearUI("BarraJefe", raiz);
        Anclar(barra, new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(900, 34));
        Image fondo = barra.gameObject.AddComponent<Image>();
        fondo.sprite = blanco;
        fondo.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        RectTransform relleno = CrearUI("Relleno", barra);
        Estirar(relleno, 4);
        Image imgRelleno = relleno.gameObject.AddComponent<Image>();
        imgRelleno.sprite = blanco;
        imgRelleno.color = new Color(0.35f, 0.75f, 0.2f);   // verde goblin
        imgRelleno.type = Image.Type.Filled;
        imgRelleno.fillMethod = Image.FillMethod.Horizontal;
        imgRelleno.fillOrigin = (int)Image.OriginHorizontal.Left;

        TextMeshProUGUI nombre = CrearTexto("Nombre", barra, "JEFE", 30, TextAlignmentOptions.Center);
        Anclar(nombre.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 40), new Vector2(900, 40));

        // Mira (crosshair) al centro de la pantalla
        RectTransform mira = CrearUI("Mira", raiz);
        Anclar(mira, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
        mira.pivot = new Vector2(0.5f, 0.5f);
        Image imgMira = mira.gameObject.AddComponent<Image>();
        imgMira.sprite = CrearSpriteMira();
        imgMira.raycastTarget = false;
        hud.mira = mira.gameObject;

        // Conectar referencias
        hud.contenedorCorazones = corazones;
        hud.spriteCorazon = corazon;
        hud.textoRonda = ronda;
        hud.textoAviso = aviso;
        hud.textoArma = null;   // ahora es un icono
        hud.iconoArma = imgIcono;
        hud.spriteEspada = ImportarSprite(CarpetaUI + "/icono_espada.png");
        hud.spriteArco = ImportarSprite(CarpetaUI + "/icono_arco.png");
        hud.textoEscudo = escudo;
        hud.barraJefe = barra.gameObject;
        hud.rellenoJefe = imgRelleno;

        // Apagar el texto de depuración del GestorOleadas
        GestorOleadas oleadas = Object.FindFirstObjectByType<GestorOleadas>();
        if (oleadas != null)
        {
            Undo.RecordObject(oleadas, "Apagar debug");
            oleadas.mostrarDebug = false;
        }

        Selection.activeGameObject = raiz.gameObject;
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("HUD creado. Guarda la escena (Ctrl+S) y dale Play.");
    }

    // ==================== Todo junto: HUD + paneles, y guarda la escena ====================
    [MenuItem("Kakariko/Rehacer interfaz (HUD y paneles)", true)]
    static bool ValidarInterfaz() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    [MenuItem("Kakariko/Rehacer interfaz (HUD y paneles)")]
    public static void RehacerInterfaz()
    {
        if (EnPlay()) return;
        if (Object.FindFirstObjectByType<GestorPartida>() == null)
        {
            EditorUtility.DisplayDialog("Kakariko", "Abre la escena Game (no encontré el GestorPartida).", "OK");
            return;
        }
        CrearHUD();
        RehacerPaneles();
        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(escena);   // guardar de una vez para no perder los cambios
        Debug.Log("Interfaz rehecha y escena guardada.");
    }

    // Importa un PNG como Sprite pixel-art (nítido)
    static Sprite ImportarSprite(string ruta)
    {
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogWarning("No encontré la imagen " + ruta); return null; }
        if (imp.textureType != TextureImporterType.Sprite || imp.filterMode != FilterMode.Point)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // ==================== Paneles Pausa / Ganar / Perder ====================
    [MenuItem("Kakariko/Rehacer paneles (Pausa, Ganar, Perder)")]
    public static void RehacerPaneles()
    {
        if (EnPlay()) return;
        GestorPartida gp = Object.FindFirstObjectByType<GestorPartida>();
        if (gp == null)
        {
            EditorUtility.DisplayDialog("Kakariko", "No encontré el GestorPartida en la escena. Abre la escena Game.", "OK");
            return;
        }
        Sprite blanco = CrearSpriteBlanco();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Kakariko", "No hay Canvas. Usa primero Kakariko > Crear HUD.", "OK");
            return;
        }
        ConfigurarEscala(canvas);

        // Borrar los paneles anteriores (los hechos a mano o por este menú)
        foreach (string n in new[] { "PanelPausa", "PanelGanar", "PanelPerder" })
        {
            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == n) { Undo.DestroyObjectImmediate(t.gameObject); break; }
        }

        Undo.RecordObject(gp, "Paneles");

        // Pausa
        Transform pausa = CrearPanel("PanelPausa", canvas.transform, blanco, "Pausa", null, out _);
        CrearBoton("Reanudar", pausa, "Reanudar", blanco, gp.Reanudar);
        CrearBoton("Salir", pausa, "Salir al menú", blanco, gp.IrAlMenu);
        gp.panelPausa = pausa.parent.gameObject;

        // Ganar
        Transform ganar = CrearPanel("PanelGanar", canvas.transform, blanco, "¡Ganaste!", "Sobreviviste 10 rondas", out TextMeshProUGUI subGanar);
        CrearBoton("Continuar", ganar, "Continuar", blanco, gp.Continuar);
        CrearBoton("Retry", ganar, "Retry", blanco, gp.Reintentar);
        CrearBoton("Salir", ganar, "Salir al menú", blanco, gp.IrAlMenu);
        gp.panelGanar = ganar.parent.gameObject;
        gp.textoGanar = subGanar;

        // Perder
        Transform perder = CrearPanel("PanelPerder", canvas.transform, blanco, "Perdiste", "Llegaste a la ronda 1", out TextMeshProUGUI subPerder);
        CrearBoton("Retry", perder, "Retry", blanco, gp.Reintentar);
        CrearBoton("Salir", perder, "Salir al menú", blanco, gp.IrAlMenu);
        gp.panelPerder = perder.parent.gameObject;
        gp.textoPerder = subPerder;

        // Se ven en el editor solo para revisarlos; el GestorPartida los oculta al dar Play
        gp.panelPausa.SetActive(false);
        gp.panelGanar.SetActive(false);
        gp.panelPerder.SetActive(false);

        AsegurarEventSystem();
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Paneles creados y conectados al GestorPartida. Guarda la escena (Ctrl+S).");
    }

    // Fondo oscuro a pantalla completa + columna centrada. Devuelve la columna (para meter botones).
    static Transform CrearPanel(string nombre, Transform padre, Sprite blanco, string titulo, string subtitulo, out TextMeshProUGUI textoSub)
    {
        RectTransform panel = CrearUI(nombre, padre);
        Estirar(panel);
        Image fondo = panel.gameObject.AddComponent<Image>();
        fondo.sprite = blanco;
        fondo.color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform columna = CrearUI("Contenido", panel);
        Anclar(columna, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 700));
        columna.pivot = new Vector2(0.5f, 0.5f);
        VerticalLayoutGroup vl = columna.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 20;
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.childControlWidth = true; vl.childControlHeight = true;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

        TextMeshProUGUI t = CrearTexto("Titulo", columna, titulo, 96, TextAlignmentOptions.Center);
        Alto(t.gameObject, 120);

        textoSub = null;
        if (subtitulo != null)
        {
            textoSub = CrearTexto("Subtitulo", columna, subtitulo, 40, TextAlignmentOptions.Center);
            textoSub.fontStyle = FontStyles.Normal;
            Alto(textoSub.gameObject, 60);
        }

        RectTransform espacio = CrearUI("Espacio", columna);
        Alto(espacio.gameObject, 20);
        return columna;
    }

    static Button CrearBoton(string nombre, Transform padre, string texto, Sprite blanco, UnityEngine.Events.UnityAction accion)
    {
        RectTransform rt = CrearUI("Boton" + nombre, padre);
        Alto(rt.gameObject, 80);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = blanco;
        img.color = new Color(0.85f, 0.7f, 0.25f);   // dorado (Trifuerza)
        Button b = rt.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.95f, 0.75f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        b.colors = cb;

        TextMeshProUGUI t = CrearTexto("Texto", rt, texto, 40, TextAlignmentOptions.Center);
        t.color = new Color(0.12f, 0.1f, 0.05f);
        Estirar(t.rectTransform);

        // La acción se conecta por código al iniciar (GestorPartida / MenuPrincipal buscan el botón por su nombre),
        // así funciona aunque Unity no guarde el On Click.
        return b;
    }

    static void Alto(GameObject go, float alto)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = alto;
        le.minHeight = alto;
    }

    static void AsegurarEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "EventSystem");
    }

    // ==================== Menú principal ====================
    [MenuItem("Kakariko/Crear Menú Principal (escena MainMenu)")]
    public static void CrearMenuPrincipal()
    {
        if (EnPlay()) return;
        const string rutaMenu = "Assets/Scenes/MainMenu.unity";
        const string rutaJuego = "Assets/Scenes/Game.unity";

        if (File.Exists(rutaMenu) &&
            !EditorUtility.DisplayDialog("Kakariko", "Ya existe MainMenu.unity. ¿Reemplazarla?", "Reemplazar", "Cancelar"))
            return;

        // Guardar la escena actual si tiene cambios
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Sprite blanco = CrearSpriteBlanco();

        // Cámara con fondo verde oscuro (Hyrule)
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.16f, 0.1f);
        }

        GameObject goCanvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ConfigurarEscala(canvas);
        AsegurarEventSystem();

        MenuPrincipal menu = new GameObject("MenuPrincipal").AddComponent<MenuPrincipal>();

        // Título y botones al centro
        RectTransform columna = CrearUI("Contenido", goCanvas.transform);
        Anclar(columna, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 600));
        columna.pivot = new Vector2(0.5f, 0.5f);
        VerticalLayoutGroup vl = columna.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 24;
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.childControlWidth = true; vl.childControlHeight = true;
        vl.childForceExpandWidth = false; vl.childForceExpandHeight = false;

        TextMeshProUGUI titulo = CrearTexto("Titulo", columna, "Kakariko Raid", 120, TextAlignmentOptions.Center);
        titulo.color = new Color(0.95f, 0.8f, 0.3f);
        Alto(titulo.gameObject, 150);
        TextMeshProUGUI sub = CrearTexto("Subtitulo", columna, "Defiende la aldea", 40, TextAlignmentOptions.Center);
        sub.fontStyle = FontStyles.Italic;
        Alto(sub.gameObject, 60);
        Alto(CrearUI("Espacio", columna).gameObject, 30);

        Button start = CrearBoton("Start", columna, "START", blanco, menu.Jugar);
        Ancho(start.gameObject, 420);
        Button salir = CrearBoton("Salir", columna, "SALIR", blanco, menu.Salir);
        Ancho(salir.gameObject, 420);

        // Controles en la esquina inferior izquierda
        TextMeshProUGUI controles = CrearTexto("Controles", goCanvas.transform,
            "<b>CONTROLES</b>\n" +
            "WASD  -  Moverse\n" +
            "Mouse  -  Cámara / apuntar\n" +
            "Click izq  -  Atacar / disparar\n" +
            "Mantener click izq  -  Ataque circular\n" +
            "Click der  -  Escudo\n" +
            "Space  -  Saltar\n" +
            "Shift  -  Rodar\n" +
            "F  -  Cambiar arma (Espada / Arco)\n" +
            "ESC  -  Pausa",
            28, TextAlignmentOptions.BottomLeft);
        controles.fontStyle = FontStyles.Normal;
        controles.lineSpacing = 10;
        Anclar(controles.rectTransform, new Vector2(0, 0), new Vector2(40, 40), new Vector2(700, 480));

        EditorSceneManager.SaveScene(escena, rutaMenu);

        // Lista de escenas: MainMenu primero (es la que abre el juego), luego Game
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(rutaMenu, true),
            new EditorBuildSettingsScene(rutaJuego, true),
        };

        Debug.Log("Escena MainMenu creada y agregada a Build Profiles. Dale Play aquí para probar START.");
    }

    static void Ancho(GameObject go, float ancho)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredWidth = ancho;
        le.minWidth = ancho;
    }

    // ==================== Modelos 3D ====================
    const string M = "Assets/Models/";

    // Corrección de orientación por modelo (los .glb de Sketchfab de este proyecto vienen acostados).
    // Si alguno sale mal, cámbialo aquí o usa Kakariko > Modelo seleccionado > Girar.
    static Vector3 Orientacion(string ruta)
    {
        if (ruta.Contains("voxel_link")) return new Vector3(-90f, 0f, 0f);
        if (ruta.Contains("skeleton")) return Vector3.zero;   // este sí viene parado
        if (ruta.Contains("mini_dragon")) return new Vector3(-90f, 0f, 0f);
        if (ruta.Contains("skeleton_drgaon")) return new Vector3(-90f, 0f, 0f);   // Blockbench, igual que el mini dragón
        if (ruta.Contains("goblin_boss")) return new Vector3(-90f, 0f, 0f);
        if (ruta.Contains("heart_voxvr")) return new Vector3(-90f, 0f, 0f);
        if (ruta.Contains("master_sword")) return new Vector3(-90f, 0f, 45f);   // sprite en diagonal: 45° para enderezarla
        return Vector3.zero;
    }

    [MenuItem("Kakariko/Poner modelos 3D (Link y enemigos)", true)]
    static bool ValidarModelos() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    [MenuItem("Kakariko/Poner modelos 3D (Link y enemigos)")]
    public static void PonerModelos()
    {
        if (EnPlay()) return;

        // --- Link (en la escena) ---
        LinkMovimiento link = Object.FindFirstObjectByType<LinkMovimiento>();
        if (link != null)
        {
            Transform t = link.transform;
            Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Modelos Link");
            t.rotation = Quaternion.identity;   // la raíz no se gira (el script la controla); se gira el modelo
            BorrarHijo(t, "Nariz");

            // Si una pieza ya existe con el mismo modelo, se deja como está (respeta tus ajustes a mano)
            if (Existente(t, "Modelo3D", M + "voxel_link_sin_armas.glb") == null)
            {
                float alto = AlturaCapsula(t, out float pies);
                PonerModelo(t, M + "voxel_link_sin_armas.glb", "Modelo3D", alto, true, pies);
            }

            Transform espada = Existente(t, "Espada3D", M + "master_sword.glb");
            if (espada == null)
            {
                espada = PonerModelo(t, M + "master_sword.glb", "Espada3D", 1.1f, false, 0f);
                if (espada != null) espada.localPosition += new Vector3(0.7f, 0f, 0.25f);   // a la derecha, a media altura
            }
            if (espada != null)
            {
                LinkCombate c = link.GetComponent<LinkCombate>();
                if (c != null) { Undo.RecordObject(c, "Espada"); c.espadaVisual = espada; }
            }

            Transform escudo = Existente(t, "Escudo3D", M + "wooden_round_shield.glb");
            if (escudo == null)
            {
                escudo = PonerModelo(t, M + "wooden_round_shield.glb", "Escudo3D", 0.9f, false, 0f);
                if (escudo != null) escudo.localPosition += new Vector3(0f, 0f, 0.6f);      // enfrente, a media altura
            }
            if (escudo != null)
            {
                LinkEscudo e = link.GetComponent<LinkEscudo>();
                if (e != null) { Undo.RecordObject(e, "Escudo"); e.escudoVisual = escudo; }
            }

            OcultarCapsula(t);
            EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        }
        else Debug.LogWarning("No encontré a Link en la escena abierta.");

        // --- Enemigos (prefabs) ---
        ModeloEnPrefab("Assets/Prefabs/Esqueleto.prefab", M + "skeleton.glb");
        ModeloEnPrefab("Assets/Prefabs/Dragona.prefab", M + "skeleton_drgaon.glb");
        ModeloEnPrefab("Assets/Prefabs/Goblin.prefab", M + "goblin_boss.glb");
        PonerArcoYFlecha();
        PonerBolaDeFuego();
        PonerRoca();
        PonerCorazon();

        Debug.Log("Modelos puestos (las piezas que ya existían con el mismo modelo se conservaron). Si alguno se ve acostado o al revés: selecciona su 'Modelo3D' y usa Kakariko > Modelo seleccionado > Girar. Guarda la escena (Ctrl+S).");
    }

    // ---------- Arco (en la mano de Link) y flecha (prefab del proyectil) ----------
    [MenuItem("Kakariko/Poner arco y flecha", true)]
    static bool ValidarArco() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    [MenuItem("Kakariko/Poner arco y flecha")]
    public static void PonerArcoYFlecha()
    {
        if (EnPlay()) return;

        // Arco: a la izquierda de Link, vertical
        LinkMovimiento link = Object.FindFirstObjectByType<LinkMovimiento>();
        if (link != null)
        {
            Transform t = link.transform;
            Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Arco");
            t.rotation = Quaternion.identity;
            Transform arco = Existente(t, "Arco3D", M + "bow_pixel.glb");
            if (arco == null)
            {
                arco = PonerModelo(t, M + "bow_pixel.glb", "Arco3D", 1.2f, false, 0f);
                if (arco != null)
                {
                    AlinearLargo(arco, Vector3.up);               // el lado largo del arco, vertical
                    Ajustar(arco, t, 1.2f, false, 0f);
                    arco.localPosition += new Vector3(-0.55f, 0f, 0.35f);
                }
            }
            if (arco != null)
            {
                LinkCombate c = link.GetComponent<LinkCombate>();
                if (c != null) { Undo.RecordObject(c, "Arco"); c.arcoVisual = arco; }
            }
            EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        }
        else Debug.LogWarning("No encontré a Link en la escena abierta.");

        // Flecha: el modelo va dentro del prefab, apuntando hacia adelante (+Z, hacia donde vuela)
        const string rutaFlecha = "Assets/Prefabs/Flecha.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaFlecha) != null)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(rutaFlecha);
            raiz.transform.localRotation = Quaternion.identity;
            raiz.transform.localScale = Vector3.one;          // antes era un cubo estirado; el modelo no debe deformarse
            BoxCollider bc = raiz.GetComponent<BoxCollider>();
            if (bc != null) { bc.center = Vector3.zero; bc.size = new Vector3(0.15f, 0.15f, 0.8f); }

            // Si ya existe con el mismo modelo se conserva tal cual (respeta ajustes a mano)
            if (Existente(raiz.transform, "Modelo3D", M + "voxel_arrow.glb") == null)
            {
                Transform m = PonerModelo(raiz.transform, M + "voxel_arrow.glb", "Modelo3D", 0.8f, false, 0f);
                if (m != null)
                {
                    AlinearLargo(m, Vector3.forward);
                    Ajustar(m, raiz.transform, 0.8f, false, 0f);
                }
            }
            OcultarCapsula(raiz.transform);                   // esconde el cubo
            PrefabUtility.SaveAsPrefabAsset(raiz, rutaFlecha);
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        else Debug.LogWarning("No existe " + rutaFlecha);

        Debug.Log("Arco y flecha puestos. Si la flecha vuela al revés: abre el prefab Flecha, selecciona Modelo3D y usa Girar 90° en Y dos veces.");
    }

    // Devuelve el hijo 'nombre' si ya existe y es del mismo modelo (para no borrar ajustes a mano).
    // Si existe pero es de otro modelo, lo borra y devuelve null.
    static Transform Existente(Transform padre, string nombre, string rutaModelo)
    {
        Transform h = padre.Find(nombre);
        if (h == null) return null;
        Object fuente = PrefabUtility.GetCorrespondingObjectFromSource(h.gameObject);
        if (fuente != null && AssetDatabase.GetAssetPath(fuente) == rutaModelo) return h;
        Object.DestroyImmediate(h.gameObject);
        return null;
    }

    // Importa el .glb con animación "Legacy" (componente Animation con sus clips)
    static void AnimacionLegacy(string ruta)
    {
        AssetImporter imp = AssetImporter.GetAtPath(ruta);
        if (imp == null) return;
        SerializedObject so = new SerializedObject(imp);
        SerializedProperty p = so.FindProperty("importSettings.animationMethod");
        if (p != null && p.intValue != 1)
        {
            p.intValue = 1;   // 0 = ninguna, 1 = Legacy, 2 = Mecanim
            so.ApplyModifiedPropertiesWithoutUndo();
            imp.SaveAndReimport();
        }
    }

    // ---------- Bola de fuego de la Dragona (modelo animado) ----------
    [MenuItem("Kakariko/Poner bola de fuego", true)]
    static bool ValidarBola() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    [MenuItem("Kakariko/Poner bola de fuego")]
    public static void PonerBolaDeFuego()
    {
        if (EnPlay()) return;
        const string rutaBola = "Assets/Prefabs/BolaDeFuego.prefab";
        const string rutaModelo = M + "fireball.glb";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaBola) == null) { Debug.LogWarning("No existe " + rutaBola); return; }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo) == null) { Debug.LogWarning("No encontré " + rutaModelo); return; }
        AnimacionLegacy(rutaModelo);

        GameObject raiz = PrefabUtility.LoadPrefabContents(rutaBola);
        raiz.transform.localRotation = Quaternion.identity;
        raiz.transform.localScale = Vector3.one;          // antes era una esfera escalada; el modelo no debe deformarse
        SphereCollider sc = raiz.GetComponent<SphereCollider>();
        if (sc != null) { sc.center = Vector3.zero; sc.radius = 0.3f; }

        if (Existente(raiz.transform, "Modelo3D", rutaModelo) == null)
        {
            Transform m = PonerModelo(raiz.transform, rutaModelo, "Modelo3D", 1.1f, false, 0f);
            if (m != null)
            {
                AlinearLargo(m, Vector3.forward);         // la estela queda a lo largo de la dirección de vuelo
                Ajustar(m, raiz.transform, 1.1f, false, 0f);
            }
        }
        OcultarCapsula(raiz.transform);                   // esconde la esfera naranja
        if (raiz.GetComponent<AnimacionEnLoop>() == null) raiz.AddComponent<AnimacionEnLoop>();

        PrefabUtility.SaveAsPrefabAsset(raiz, rutaBola);
        PrefabUtility.UnloadPrefabContents(raiz);
        Debug.Log("Bola de fuego puesta (con animación en loop). Si la estela va hacia adelante: abre el prefab BolaDeFuego, selecciona Modelo3D y usa Girar 90° en Y dos veces.");
    }

    // ---------- Espada y escudo en las manos de Link ----------
    [MenuItem("Kakariko/Acomodar espada y escudo en las manos", true)]
    static bool ValidarManos() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    // Busca las manos en el modelo (cubitos color piel a la altura de la cintura, uno a cada lado)
    // y pone el mango de la espada en una mano y el escudo en el otro brazo.
    // Respeta el lado que ya tiene cada arma y la inclinación de la espada.
    [MenuItem("Kakariko/Acomodar espada y escudo en las manos")]
    public static void AcomodarArmas()
    {
        if (EnPlay()) return;
        LinkMovimiento link = Object.FindFirstObjectByType<LinkMovimiento>();
        if (link == null) { Debug.LogWarning("No encontré a Link en la escena abierta."); return; }
        Transform t = link.transform;
        Transform modelo = t.Find("Modelo3D"), espada = t.Find("Espada3D"), escudo = t.Find("Escudo3D");
        if (modelo == null || espada == null || escudo == null)
        {
            EditorUtility.DisplayDialog("Kakariko", "Link necesita Modelo3D, Espada3D y Escudo3D (usa Poner modelos 3D).", "OK");
            return;
        }
        Limites(modelo.gameObject, out Bounds cuerpo);

        // Vértices color piel (u de la paleta = 0.01758) entre el 10% y el 42% de la altura
        const float uPiel = 0.01758f;
        Vector3 sumaI = Vector3.zero, sumaD = Vector3.zero; int nI = 0, nD = 0;
        foreach (MeshFilter mf in modelo.GetComponentsInChildren<MeshFilter>())
        {
            Mesh m = mf.sharedMesh; if (m == null) continue;
            Vector3[] v = m.vertices; Vector2[] uv = m.uv;
            for (int i = 0; i < v.Length && i < uv.Length; i++)
            {
                if (Mathf.Abs(uv[i].x - uPiel) > 0.0015f) continue;
                Vector3 p = mf.transform.TransformPoint(v[i]);
                float h = (p.y - cuerpo.min.y) / cuerpo.size.y;
                if (h < 0.10f || h > 0.42f) continue;
                if (Vector3.Dot(p - t.position, t.right) < 0f) { sumaI += p; nI++; } else { sumaD += p; nD++; }
            }
        }
        if (nI == 0 || nD == 0) { Debug.LogWarning("No encontré las dos manos en el modelo de Link."); return; }
        Vector3 manoI = sumaI / nI, manoD = sumaD / nD;

        // Cada arma va a la mano de su lado actual (si las dos están del mismo lado: espada derecha, escudo izquierda)
        bool espadaIzq = Vector3.Dot(espada.position - t.position, t.right) < 0f;
        bool escudoIzq = Vector3.Dot(escudo.position - t.position, t.right) < 0f;
        if (espadaIzq == escudoIzq) { espadaIzq = false; escudoIzq = true; }
        Vector3 manoEspada = espadaIzq ? manoI : manoD, manoEscudo = escudoIzq ? manoI : manoD;
        Vector3 haciaAfueraEscudo = escudoIzq ? -t.right : t.right;

        Undo.RecordObject(espada, "Acomodar espada");
        Undo.RecordObject(escudo, "Acomodar escudo");

        // Espada: sus dos puntas = los vértices más alejados entre sí; el mango es la punta más baja
        Vector3[] pts = PuntosMundo(espada);
        if (pts.Length > 1)
        {
            Vector3 c = Vector3.zero; foreach (Vector3 p in pts) c += p; c /= pts.Length;
            Vector3 a = Lejano(pts, c), b = Lejano(pts, a);
            Vector3 mango = a.y < b.y ? a : b, punta = a.y < b.y ? b : a;
            Vector3 agarre = Vector3.Lerp(mango, punta, 0.12f);          // un poco arriba del extremo: donde se agarra
            espada.position += (manoEspada + t.forward * 0.05f) - agarre;
        }

        // Escudo: su centro, pegado por fuera del antebrazo y un poco al frente
        Limites(escudo.gameObject, out Bounds be);
        escudo.position += (manoEscudo + haciaAfueraEscudo * 0.12f + t.forward * 0.1f + Vector3.up * 0.1f) - be.center;

        EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        EditorSceneManager.SaveScene(t.gameObject.scene);
        Debug.Log("Espada en la mano " + (espadaIzq ? "izquierda" : "derecha") + " y escudo en el brazo " +
                  (escudoIzq ? "izquierdo" : "derecho") + ". Escena guardada.");
    }

    static Vector3[] PuntosMundo(Transform raiz)
    {
        var lista = new System.Collections.Generic.List<Vector3>();
        foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            foreach (Vector3 v in mf.sharedMesh.vertices) lista.Add(mf.transform.TransformPoint(v));
        }
        return lista.ToArray();
    }

    static Vector3 Lejano(Vector3[] pts, Vector3 desde)
    {
        Vector3 mejor = desde; float d = -1f;
        foreach (Vector3 p in pts) { float e = (p - desde).sqrMagnitude; if (e > d) { d = e; mejor = p; } }
        return mejor;
    }

    // ---------- Hitbox de la Dragona ----------
    [MenuItem("Kakariko/Ajustar hitbox de la Dragona", true)]
    static bool ValidarHitbox() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    // Acuesta la cápsula a lo largo del cuerpo del dragón y la ajusta a su tamaño.
    // NO cambia la escala ni el modelo: solo el área donde recibe golpes.
    [MenuItem("Kakariko/Ajustar hitbox de la Dragona")]
    public static void AjustarHitboxDragona()
    {
        if (EnPlay()) return;
        AjustarHitbox("Assets/Prefabs/Dragona.prefab", 1.1f);
    }

    static void AjustarHitbox(string rutaPrefab, float margen)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab) == null) { Debug.LogWarning("No existe " + rutaPrefab); return; }
        GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
        Transform modelo = raiz.transform.Find("Modelo3D");
        CapsuleCollider cc = raiz.GetComponent<CapsuleCollider>();
        if (modelo == null || cc == null || !Limites(modelo.gameObject, out Bounds b))
        {
            Debug.LogWarning(rutaPrefab + " necesita un hijo Modelo3D y un Capsule Collider.");
            PrefabUtility.UnloadPrefabContents(raiz);
            return;
        }

        // Medidas del cuerpo en el espacio del prefab (sin la escala de la raíz)
        Vector3 escala = raiz.transform.lossyScale;
        Vector3 tam = new Vector3(b.size.x / escala.x, b.size.y / escala.y, b.size.z / escala.z) * margen;
        cc.center = raiz.transform.InverseTransformPoint(b.center);

        // El eje más largo del cuerpo es la dirección de la cápsula; el grosor sale de los otros dos
        int eje = (tam.x >= tam.y && tam.x >= tam.z) ? 0 : (tam.y >= tam.z ? 1 : 2);
        float largo = eje == 0 ? tam.x : (eje == 1 ? tam.y : tam.z);
        float a = eje == 0 ? tam.y : tam.x, c = eje == 2 ? tam.y : tam.z;
        cc.direction = eje;
        cc.radius = Mathf.Max(a, c) * 0.5f;
        cc.height = Mathf.Max(largo, cc.radius * 2f);

        PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
        PrefabUtility.UnloadPrefabContents(raiz);
        string[] ejes = { "X", "Y", "Z (a lo largo)" };
        Debug.Log("Hitbox de " + rutaPrefab + ": eje " + ejes[eje] + ", largo " + cc.height.ToString("0.00") +
                  ", radio " + cc.radius.ToString("0.00") + " (escala sin cambios).");
    }

    // ---------- Pieza de corazón ----------
    [MenuItem("Kakariko/Poner corazón", true)]
    static bool ValidarCorazon() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    [MenuItem("Kakariko/Poner corazón")]
    public static void PonerCorazon()
    {
        if (EnPlay()) return;
        const string rutaPieza = "Assets/Prefabs/PiezaCorazon.prefab";
        const string rutaModelo = M + "heart_voxvr.glb";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaPieza) == null) { Debug.LogWarning("No existe " + rutaPieza); return; }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo) == null) { Debug.LogWarning("No encontré " + rutaModelo); return; }

        GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPieza);
        raiz.transform.localRotation = Quaternion.identity;
        raiz.transform.localScale = Vector3.one;          // antes era un cubo escalado
        BoxCollider bc = raiz.GetComponent<BoxCollider>();
        if (bc != null) { bc.center = Vector3.zero; bc.size = Vector3.one * 0.8f; }   // fácil de recoger

        if (Existente(raiz.transform, "Modelo3D", rutaModelo) == null)
            PonerModelo(raiz.transform, rutaModelo, "Modelo3D", 0.7f, false, 0f);
        OcultarCapsula(raiz.transform);                   // esconde el cubo rojo

        PrefabUtility.SaveAsPrefabAsset(raiz, rutaPieza);
        PrefabUtility.UnloadPrefabContents(raiz);
        Debug.Log("Corazón puesto en PiezaCorazon (gira solo).");
    }

    // ---------- Roca del Goblin ----------
    [MenuItem("Kakariko/Poner roca del Goblin", true)]
    static bool ValidarRoca() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    [MenuItem("Kakariko/Poner roca del Goblin")]
    public static void PonerRoca()
    {
        if (EnPlay()) return;
        const string rutaRoca = "Assets/Prefabs/Roca.prefab";
        const string rutaModelo = M + "rock_b.glb";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaRoca) == null) { Debug.LogWarning("No existe " + rutaRoca); return; }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo) == null) { Debug.LogWarning("No encontré " + rutaModelo); return; }

        GameObject raiz = PrefabUtility.LoadPrefabContents(rutaRoca);
        SphereCollider sc = raiz.GetComponent<SphereCollider>();

        // Conservar el tamaño que tenía la esfera (su diámetro en el mundo)
        float diametro = 1f;
        if (sc != null) diametro = sc.radius * 2f * Mathf.Max(raiz.transform.localScale.x, raiz.transform.localScale.y, raiz.transform.localScale.z);
        raiz.transform.localRotation = Quaternion.identity;
        raiz.transform.localScale = Vector3.one;
        if (sc != null) { sc.center = Vector3.zero; sc.radius = diametro * 0.5f; }   // mismo tamaño si vuelves a usar el menú

        if (Existente(raiz.transform, "Modelo3D", rutaModelo) == null)
            PonerModelo(raiz.transform, rutaModelo, "Modelo3D", diametro, false, 0f);
        OcultarCapsula(raiz.transform);                   // esconde la esfera gris
        if (raiz.GetComponent<RotarModelo>() == null) raiz.AddComponent<RotarModelo>();

        PrefabUtility.SaveAsPrefabAsset(raiz, rutaRoca);
        PrefabUtility.UnloadPrefabContents(raiz);
        Debug.Log("Roca puesta (" + diametro.ToString("0.0") + " m, gira en el aire).");
    }

    // ==================== Música ====================
    [MenuItem("Kakariko/Configurar audio (música y sonidos)", true)]
    static bool ValidarMusica() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    [MenuItem("Kakariko/Configurar audio (música y sonidos)")]
    public static void ConfigurarMusica()
    {
        if (EnPlay()) return;
        const string carpeta = "Assets/Audio/Musica/";
        string[] nombres = { "musica_menu", "musica_ronda", "musica_jefe", "musica_perder", "musica_ganar" };
        AudioClip[] clips = new AudioClip[nombres.Length];

        for (int i = 0; i < nombres.Length; i++)
        {
            string ruta = carpeta + nombres[i] + ".mp3";
            AudioImporter imp = AssetImporter.GetAtPath(ruta) as AudioImporter;
            if (imp == null) { Debug.LogWarning("No encontré " + ruta); continue; }

            // Música: se lee del disco mientras suena (no ocupa memoria)
            AudioImporterSampleSettings ajustes = imp.defaultSampleSettings;
            if (ajustes.loadType != AudioClipLoadType.Streaming)
            {
                ajustes.loadType = AudioClipLoadType.Streaming;
                imp.defaultSampleSettings = ajustes;
                imp.SaveAndReimport();
            }
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }

        // Prefab en Resources: GestorAudio se crea solo al iniciar cualquier escena
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string rutaPrefab = "Assets/Resources/GestorAudio.prefab";
        GameObject go = new GameObject("GestorAudio");
        GestorAudio ga = go.AddComponent<GestorAudio>();
        GestorAudio anterior = AssetDatabase.LoadAssetAtPath<GestorAudio>(rutaPrefab);
        if (anterior != null) ga.volumenMusica = anterior.volumenMusica;   // conservar el volumen que hayas puesto
        ga.musicaMenu = clips[0];
        ga.musicaRonda = clips[1];
        ga.musicaJefe = clips[2];
        ga.musicaPerder = clips[3];
        ga.musicaGanar = clips[4];
        if (anterior != null) ga.volumenEfectos = anterior.volumenEfectos;

        // Efectos de sonido (Assets/Audio/SFX). Si reemplazas uno por un .mp3/.ogg con el mismo nombre, también lo toma.
        ga.boton = Sfx("boton");
        ga.espadazo = Sfx("espadazo");
        ga.espadazoCircular = Sfx("espadazo_circular");
        ga.disparoArco = Sfx("disparo_arco");
        ga.disparoDragona = Sfx("disparo_dragona");
        ga.goblinDisparo = Sfx("goblin_disparo");
        ga.golpeRecibido = Sfx("golpe_recibido");
        ga.golpeEscudo = Sfx("golpe_escudo");
        ga.rugidoGoblin = Sfx("rugido_goblin");
        if (anterior != null) ga.volumenVoz = anterior.volumenVoz;
        ga.vozDanio = Voz("danio");
        ga.vozMorir = Voz("morir");
        ga.vozSaltar = Voz("saltar");
        ga.vozGiro = Voz("giro");

        // Voz de Link al atacar: Assets/Audio/Link/ataque_1 ... ataque_N (en orden)
        var voces = new System.Collections.Generic.List<AudioClip>();
        for (int i = 1; i <= 20; i++)
        {
            AudioClip c = null;
            foreach (string ext in new[] { ".wav", ".ogg", ".mp3" })
                if (c == null) c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Link/ataque_" + i + ext);
            if (c == null) break;
            voces.Add(c);
        }
        ga.vozAtaque = voces.ToArray();
        PrefabUtility.SaveAsPrefabAsset(go, rutaPrefab);
        Object.DestroyImmediate(go);

        Debug.Log("Audio configurado en Assets/Resources/GestorAudio.prefab (5 canciones, 9 efectos y " + ga.vozAtaque.Length + " voces de ataque de Link). Funciona en ambas escenas sin ponerlo a mano.");
    }

    // Voces de Link: Assets/Audio/Link/<nombre>.wav/.ogg/.mp3
    static AudioClip Voz(string nombre)
    {
        foreach (string ext in new[] { ".wav", ".ogg", ".mp3" })
        {
            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Link/" + nombre + ext);
            if (c != null) return c;
        }
        Debug.LogWarning("No encontré la voz " + nombre + " en Assets/Audio/Link");
        return null;
    }

    static AudioClip Sfx(string nombre)
    {
        foreach (string ext in new[] { ".wav", ".ogg", ".mp3" })
        {
            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/" + nombre + ext);
            if (c != null) return c;
        }
        Debug.LogWarning("No encontré el sonido " + nombre + " en Assets/Audio/SFX");
        return null;
    }

    // Gira 'modelo' para que su lado más largo quede sobre 'eje' (en el espacio del padre)
    static void AlinearLargo(Transform modelo, Vector3 eje)
    {
        if (!Limites(modelo.gameObject, out Bounds b)) return;
        Vector3 s = b.size;
        Vector3 largo = (s.x >= s.y && s.x >= s.z) ? Vector3.right : (s.y >= s.z ? Vector3.up : Vector3.forward);
        Vector3 desde = modelo.parent.InverseTransformDirection(largo);
        modelo.localRotation = Quaternion.FromToRotation(desde, eje) * modelo.localRotation;
    }

    static void ModeloEnPrefab(string rutaPrefab, string rutaModelo)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab) == null) { Debug.LogWarning("No existe " + rutaPrefab); return; }
        GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
        raiz.transform.localRotation = Quaternion.identity;   // la raíz no se gira (el script la controla); se gira el modelo
        BorrarHijo(raiz.transform, "Nariz");

        if (Existente(raiz.transform, "Modelo3D", rutaModelo) == null)   // mismo modelo: se conserva
        {
            float alto = AlturaCapsula(raiz.transform, out float pies);
            PonerModelo(raiz.transform, rutaModelo, "Modelo3D", alto, true, pies);
        }
        if (raiz.transform.Find("Modelo3D") != null) OcultarCapsula(raiz.transform);

        PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    // Mete el modelo como hijo, lo endereza según la tabla y lo ajusta al tamaño
    static Transform PonerModelo(Transform padre, string ruta, string nombre, float medida, bool porAltura, float piesY)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (asset == null) { Debug.LogWarning("No encontré el modelo " + ruta + " (¿está instalado glTFast?)"); return null; }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, padre);
        go.name = nombre;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(Orientacion(ruta));   // enderezar ANTES de medir
        go.transform.localScale = Vector3.one;
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

        Ajustar(go.transform, padre, medida, porAltura, piesY);
        return go.transform;
    }

    // Escala 'modelo' para que mida 'medida' (alto o lado más largo) y lo centra en el padre.
    // Si porAltura, sus pies quedan a la altura piesY (mundo).
    static void Ajustar(Transform modelo, Transform padre, float medida, bool porAltura, float piesY)
    {
        if (!Limites(modelo.gameObject, out Bounds b)) return;
        float tamaño = porAltura ? b.size.y : Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (tamaño > 0.0001f) modelo.localScale *= medida / tamaño;

        Limites(modelo.gameObject, out b);
        Vector3 destino = padre.position;
        Vector3 actual = b.center;
        if (porAltura) { destino.y = piesY; actual.y = b.min.y; }
        modelo.position += destino - actual;
    }

    static bool Limites(GameObject go, out Bounds b)
    {
        b = new Bounds();
        bool hay = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
        }
        return hay;
    }

    // Alto en el mundo del collider (cápsula) y la altura de sus pies
    static float AlturaCapsula(Transform t, out float piesY)
    {
        CapsuleCollider cc = t.GetComponent<CapsuleCollider>();
        if (cc != null)
        {
            // Solo cuenta el largo si la cápsula está parada (eje Y); acostada, su alto es el diámetro
            float alto = (cc.direction == 1 ? Mathf.Max(cc.height, cc.radius * 2f) : cc.radius * 2f) * t.lossyScale.y;
            float centroY = t.TransformPoint(cc.center).y;
            piesY = centroY - alto / 2f;
            return alto;
        }
        piesY = t.position.y - t.lossyScale.y;
        return 2f * t.lossyScale.y;
    }

    // Esconde la cápsula del graybox (el collider se queda)
    static void OcultarCapsula(Transform t)
    {
        MeshRenderer mr = t.GetComponent<MeshRenderer>();
        if (mr != null) { Undo.RecordObject(mr, "Ocultar"); mr.enabled = false; }
    }

    static void BorrarHijo(Transform padre, string nombre)
    {
        Transform h = padre.Find(nombre);
        if (h != null) Object.DestroyImmediate(h.gameObject);
    }

    // ---------- Corregir a mano el modelo seleccionado ----------
    [MenuItem("Kakariko/Modelo seleccionado/Girar 90° en X")] static void GirarX() { GirarSeleccion(new Vector3(90f, 0f, 0f)); }
    [MenuItem("Kakariko/Modelo seleccionado/Girar 90° en Y")] static void GirarY() { GirarSeleccion(new Vector3(0f, 90f, 0f)); }
    [MenuItem("Kakariko/Modelo seleccionado/Girar 90° en Z")] static void GirarZ() { GirarSeleccion(new Vector3(0f, 0f, 90f)); }
    [MenuItem("Kakariko/Modelo seleccionado/Girar 45° en Z")] static void GirarZ45() { GirarSeleccion(new Vector3(0f, 0f, 45f)); }

    [MenuItem("Kakariko/Modelo seleccionado/Girar 90° en X", true)]
    [MenuItem("Kakariko/Modelo seleccionado/Girar 90° en Y", true)]
    [MenuItem("Kakariko/Modelo seleccionado/Girar 90° en Z", true)]
    [MenuItem("Kakariko/Modelo seleccionado/Girar 45° en Z", true)]
    static bool ValidarGirar()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && Selection.activeTransform != null;
    }

    static void GirarSeleccion(Vector3 euler)
    {
        Transform t = Selection.activeTransform;

        // Si seleccionaste la raíz (Link, Esqueleto...), girar su hijo Modelo3D:
        // la raíz la endereza el script en cada frame, así que girarla no sirve.
        Transform modelo = t.Find("Modelo3D");
        if (modelo != null) t = modelo;
        else if (t.parent == null)
        {
            EditorUtility.DisplayDialog("Kakariko",
                "Selecciona el hijo 'Modelo3D' (o Espada3D / Escudo3D).\nLa raíz no se puede girar: el script la endereza al jugar.", "OK");
            return;
        }

        // Si la raíz quedó girada a mano, regresarla a 0
        Transform raizPersonaje = t.parent;
        if (t.name == "Modelo3D" && raizPersonaje.localRotation != Quaternion.identity)
        {
            Undo.RecordObject(raizPersonaje, "Enderezar raíz");
            raizPersonaje.localRotation = Quaternion.identity;
        }

        Undo.RecordObject(t, "Girar modelo");
        t.localRotation = Quaternion.Euler(euler) * t.localRotation;   // gira en el espacio del padre

        Transform padre = t.parent;
        if (t.name == "Modelo3D")
        {
            float alto = AlturaCapsula(padre, out float pies);
            Ajustar(t, padre, alto, true, pies);   // mismo alto y pies en el piso
        }

        EditorUtility.SetDirty(t);
        var etapa = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (etapa != null) EditorSceneManager.MarkSceneDirty(etapa.scene);
        else EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
    }

    // ==================== Mapa de Kakariko ====================
    const float EscalaMapaPorDefecto = 0.18f;

    [MenuItem("Kakariko/Poner mapa de Kakariko", true)]
    static bool ValidarMapa() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    [MenuItem("Kakariko/Poner mapa de Kakariko")]
    public static void PonerMapa()
    {
        if (EnPlay()) return;
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(M + "voxel_kakariko_full_scene.glb");
        if (asset == null) { Debug.LogWarning("No encontré voxel_kakariko_full_scene.glb"); return; }

        // Escala: la que ya tenía el mapa si la cambiaste, si no la de por defecto
        float escala = EscalaMapaPorDefecto;
        GameObject viejo = GameObject.Find("MapaKakariko");
        if (viejo != null)
        {
            float s0 = viejo.transform.localScale.x;
            if (Mathf.Abs(s0 - 1f) > 0.001f) escala = s0;
            Undo.DestroyObjectImmediate(viejo);
        }
        foreach (string n in new[] { "Limites", "Suelo", "Navegacion" })
        {
            GameObject o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }

        EditorUtility.DisplayProgressBar("Kakariko", "Colocando el mapa (son miles de piezas)...", 0.1f);
        try
        {
            GameObject mapa = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            mapa.name = "MapaKakariko";
            Undo.RegisterCreatedObjectUndo(mapa, "Mapa");
            mapa.transform.position = Vector3.zero;
            mapa.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // viene acostado
            mapa.transform.localScale = Vector3.one * escala;

            // Centrar en el origen con lo más bajo en y = 0
            Limites(mapa, out Bounds b);
            mapa.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            Limites(mapa, out b);
            Vector3 c = b.center, s = b.size;

            // --- Altura del piso: la parte de arriba más común entre las piezas ---
            MeshRenderer[] piezas = mapa.GetComponentsInChildren<MeshRenderer>();
            var conteo = new System.Collections.Generic.Dictionary<int, int>();
            foreach (MeshRenderer r in piezas)
            {
                int k = Mathf.RoundToInt(r.bounds.max.y / 0.05f);
                conteo[k] = conteo.TryGetValue(k, out int v) ? v + 1 : 1;
            }
            int mejor = 0, veces = -1;
            foreach (var kv in conteo) if (kv.Value > veces) { veces = kv.Value; mejor = kv.Key; }
            float piso = mejor * 0.05f;
            float umbral = piso + 0.35f;

            // --- Colliders: solo casas y objetos; el piso voxel NO (para no tropezar) ---
            EditorUtility.DisplayProgressBar("Kakariko", "Agregando colliders...", 0.4f);
            int conCollider = 0;
            foreach (MeshFilter mf in mapa.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                Renderer r = mf.GetComponent<Renderer>();
                mf.gameObject.isStatic = true;
                if (r != null && r.bounds.max.y <= umbral) continue;   // pieza del piso
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                conCollider++;
            }

            // Suelo plano e invisible justo a la altura del piso
            GameObject suelo = new GameObject("Suelo", typeof(BoxCollider));
            Undo.RegisterCreatedObjectUndo(suelo, "Suelo");
            suelo.transform.position = new Vector3(c.x, piso - 0.5f, c.z);
            suelo.GetComponent<BoxCollider>().size = new Vector3(s.x + 10f, 1f, s.z + 10f);
            suelo.isStatic = true;

            // Desactivar los planos del graybox / parche (no se borran)
            foreach (string n in new[] { "Plane", "Plane (1)" })
            {
                GameObject p = GameObject.Find(n);
                if (p != null) { Undo.RecordObject(p, "Plane"); p.SetActive(false); }
            }

            // Paredes invisibles en el borde
            GameObject limites = new GameObject("Limites");
            Undo.RegisterCreatedObjectUndo(limites, "Limites");
            float alto = 40f, grosor = 2f, yp = piso + alto / 2f;
            Pared(limites.transform, "Norte", new Vector3(c.x, yp, b.max.z + grosor / 2), new Vector3(s.x + grosor * 2, alto, grosor));
            Pared(limites.transform, "Sur",   new Vector3(c.x, yp, b.min.z - grosor / 2), new Vector3(s.x + grosor * 2, alto, grosor));
            Pared(limites.transform, "Este",  new Vector3(b.max.x + grosor / 2, yp, c.z), new Vector3(grosor, alto, s.z));
            Pared(limites.transform, "Oeste", new Vector3(b.min.x - grosor / 2, yp, c.z), new Vector3(grosor, alto, s.z));

            // --- NavMesh para que los enemigos rodeen las casas ---
            EditorUtility.DisplayProgressBar("Kakariko", "Calculando el NavMesh (caminos de los enemigos)...", 0.7f);
            LinkMovimiento link = Object.FindFirstObjectByType<LinkMovimiento>();
            if (link != null)
            {
                var mod = link.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
                if (mod == null) mod = Undo.AddComponent<Unity.AI.Navigation.NavMeshModifier>(link.gameObject);
                mod.ignoreFromBuild = true;   // Link no es un obstáculo fijo
            }
            GameObject nav = new GameObject("Navegacion");
            Undo.RegisterCreatedObjectUndo(nav, "Navegacion");
            var surf = nav.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            surf.collectObjects = Unity.AI.Navigation.CollectObjects.All;
            surf.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            Physics.SyncTransforms();
            surf.BuildNavMesh();
            GuardarNavMesh(surf);

            // --- Inicio: centro del pueblo, en el suelo (no en un techo) ---
            Vector3 centro = new Vector3(c.x, piso, c.z);
            if (UnityEngine.AI.NavMesh.SamplePosition(centro, out UnityEngine.AI.NavMeshHit hc, 30f, UnityEngine.AI.NavMesh.AllAreas))
                centro = hc.position;

            if (link != null)
            {
                Undo.RecordObject(link.transform, "Link");
                link.transform.position = centro + Vector3.up * 1.1f;
                link.transform.rotation = Quaternion.identity;
            }

            // --- Spawns en anillo alrededor del centro ---
            GestorOleadas g = Object.FindFirstObjectByType<GestorOleadas>();
            if (g != null && g.contenedorSpawns != null)
            {
                Transform cont = g.contenedorSpawns;
                Undo.RecordObject(cont, "Spawns");
                cont.position = Vector3.zero;
                const int cantidad = 8;
                while (cont.childCount < cantidad)
                {
                    GameObject nuevo = new GameObject("Spawn (" + cont.childCount + ")");
                    Undo.RegisterCreatedObjectUndo(nuevo, "Spawn");
                    nuevo.transform.SetParent(cont, false);
                }

                float radio = Mathf.Min(25f, 0.3f * Mathf.Min(s.x, s.z));
                for (int i = 0; i < cont.childCount; i++)
                {
                    float ang = i * Mathf.PI * 2f / cantidad;
                    Vector3 p = centro + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radio;
                    p.y = piso;
                    if (UnityEngine.AI.NavMesh.SamplePosition(p, out UnityEngine.AI.NavMeshHit hs, 10f, UnityEngine.AI.NavMesh.AllAreas))
                        p = hs.position;
                    else
                        p = SobreSuelo(p, b.max.y + 5f);

                    Transform sp = cont.GetChild(i);
                    Undo.RecordObject(sp, "Spawn");
                    sp.position = p + Vector3.up;
                }
            }

            Debug.Log("Mapa colocado (escala " + escala + "). Tamaño: " + s.x.ToString("0") + " x " + s.z.ToString("0") +
                      " m. Piso en y = " + piso.ToString("0.00") + ", piezas con collider: " + conCollider + "/" + piezas.Length +
                      ". Si Link se ve muy chico o grande, cambia la escala de MapaKakariko y vuelve a usar este menú.");
            EditorSceneManager.MarkSceneDirty(mapa.scene);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    // Guarda el NavMesh como archivo (si no, se pierde al cerrar la escena)
    static void GuardarNavMesh(Unity.AI.Navigation.NavMeshSurface surf)
    {
        if (surf.navMeshData == null) { Debug.LogWarning("No se pudo crear el NavMesh."); return; }
        const string carpeta = "Assets/Scenes/Game";
        if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder("Assets/Scenes", "Game");
        string ruta = carpeta + "/NavMesh-Navegacion.asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(surf.navMeshData, ruta);
        AssetDatabase.SaveAssets();
    }

    static void Pared(Transform padre, string nombre, Vector3 pos, Vector3 tam)
    {
        GameObject p = new GameObject("Pared" + nombre, typeof(BoxCollider));
        p.transform.SetParent(padre, false);
        p.transform.position = pos;
        p.GetComponent<BoxCollider>().size = tam;
    }

    // Punto más alto bajo (x, z)
    static Vector3 SobreSuelo(Vector3 p, float desdeY)
    {
        Physics.SyncTransforms();
        if (Physics.Raycast(new Vector3(p.x, desdeY, p.z), Vector3.down, out RaycastHit hit, desdeY + 50f))
            return hit.point;
        return new Vector3(p.x, 0f, p.z);
    }

    // ==================== Valores del GDD ====================
    // Unity guarda los valores del Inspector en la escena/prefabs, así que cambiar el código no basta.
    // Este menú los pone todos según el GDD de una vez.
    [MenuItem("Kakariko/Aplicar valores del GDD")]
    public static void AplicarValoresGDD()
    {
        if (EnPlay()) return;
        PhysicsMaterial sinFriccion = MaterialSinFriccion();

        // --- Link (en la escena abierta) ---
        LinkMovimiento link = Object.FindFirstObjectByType<LinkMovimiento>();
        if (link != null)
        {
            Vida v = link.GetComponent<Vida>();
            if (v != null) { Undo.RecordObject(v, "GDD"); v.vidaMaxima = 20f; v.invulnerabilidad = 0.5f; }

            LinkCombate c = link.GetComponent<LinkCombate>();
            if (c != null)
            {
                Undo.RecordObject(c, "GDD"); c.daño = 1f; c.dañoFlecha = 1f; c.knockback = 1.5f;
                c.segundosDañoMin = 1f; c.segundosDañoMax = 2f; c.dañoGiroMin = 1f; c.dañoGiroMax = 4f;   // giro cargado
            }

            LinkEscudo e = link.GetComponent<LinkEscudo>();
            if (e != null) { Undo.RecordObject(e, "GDD"); e.golpesMaximos = 5; e.tiempoRoto = 10f; }

            Undo.RecordObject(link, "GDD");
            link.velocidad = 8f;

            Collider col = link.GetComponent<Collider>();
            if (col != null) { Undo.RecordObject(col, "GDD"); col.sharedMaterial = sinFriccion; }
        }
        else Debug.LogWarning("No encontré a Link en la escena abierta.");

        // --- GestorOleadas (en la escena abierta) ---
        GestorOleadas g = Object.FindFirstObjectByType<GestorOleadas>();
        if (g != null)
        {
            Undo.RecordObject(g, "GDD");
            g.esqueletosPorRonda = 4;
            g.baseDragonas = GestorOleadas.BaseDragonas.Ronda;
            g.factorDragonas = 0.7f;
            g.rondaInicioDragonas = 2;
            g.cadaCuantasGoblin = 10;
            g.sumarGoblins = false;
            g.cadaCuantasCurar = 3;
            g.corazonesCuracion = 10f;
        }

        // --- Prefabs de enemigos ---
        EditarPrefab<Esqueleto>("Assets/Prefabs/Esqueleto.prefab", (raiz, e) =>
        {
            e.velocidad = 4.5f; e.cadenciaAtaque = 1f; e.usarNavMesh = true; e.daño = 0.5f; e.probCorazon = 0.05f;
            raiz.transform.localScale = Vector3.one * 0.75f;
            raiz.GetComponent<Collider>().sharedMaterial = sinFriccion;
            raiz.GetComponent<Vida>().vidaMaxima = 2f;
        });
        EditarPrefab<Dragona>("Assets/Prefabs/Dragona.prefab", (raiz, d) =>
        {
            d.velocidad = 7f; d.usarNavMesh = false; d.cadenciaAtaque = 2.5f; d.rangoAtaque = 15f; d.daño = 2f; d.probCorazon = 0.05f;
            raiz.transform.localScale = Vector3.one * 1.2f;
            raiz.GetComponent<Collider>().sharedMaterial = sinFriccion;
            raiz.GetComponent<Vida>().vidaMaxima = 2f;
        });
        EditarPrefab<Goblin>("Assets/Prefabs/Goblin.prefab", (raiz, gb) =>
        {
            gb.velocidad = 2f; gb.usarNavMesh = true; gb.cadenciaAtaque = 1f; gb.rangoAtaque = 25f; gb.recibeKnockback = false;
            gb.dañoGolpe = 1.5f; gb.dañoRoca = 3f; gb.cadenciaRoca = 3f; gb.probCorazon = 1f;
            raiz.transform.localScale = Vector3.one * 4f;
            raiz.GetComponent<Collider>().sharedMaterial = sinFriccion;
            raiz.GetComponent<Vida>().vidaMaxima = 30f;
        });

        if (link != null) EditorSceneManager.MarkSceneDirty(link.gameObject.scene);
        Debug.Log("Valores del GDD aplicados (Link 20 corazones y velocidad 8, enemigos, oleadas y material sin fricción). Guarda la escena (Ctrl+S).");
    }

    // Material de física sin fricción: Link y enemigos no se pegan a bordes ni paredes
    static PhysicsMaterial MaterialSinFriccion()
    {
        const string ruta = "Assets/Physics/SinFriccion.physicMaterial";
        PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(ruta);
        if (mat != null) return mat;
        if (!AssetDatabase.IsValidFolder("Assets/Physics")) AssetDatabase.CreateFolder("Assets", "Physics");
        mat = new PhysicsMaterial("SinFriccion")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = 0f,
        };
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    static void EditarPrefab<T>(string ruta, System.Action<GameObject, T> editar) where T : Component
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
        {
            Debug.LogWarning("No existe el prefab " + ruta);
            return;
        }
        GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
        T comp = raiz.GetComponent<T>();
        if (comp != null && raiz.GetComponent<Vida>() != null)
        {
            editar(raiz, comp);
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        }
        else Debug.LogWarning(ruta + " no tiene el script " + typeof(T).Name + " o Vida.");
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    // Escala la interfaz según la resolución (en vez de píxeles fijos)
    static void ConfigurarEscala(Canvas canvas)
    {
        CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
        if (cs == null) cs = canvas.gameObject.AddComponent<CanvasScaler>();
        Undo.RecordObject(cs, "Escala Canvas");
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;
    }

    // ==================== Utilidades UI ====================
    static RectTransform CrearUI(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        go.transform.SetParent(padre, false);
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string texto, float tamaño, TextAlignmentOptions alineacion)
    {
        RectTransform rt = CrearUI(nombre, padre);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tamaño;
        t.alignment = alineacion;
        t.color = Color.white;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        return t;
    }

    // Ancla en una esquina/borde con posición y tamaño fijos
    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 posicion, Vector2 tamaño)
    {
        rt.anchorMin = ancla; rt.anchorMax = ancla; rt.pivot = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamaño;
    }

    static void Estirar(RectTransform rt, float margen = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.one * margen; rt.offsetMax = -Vector2.one * margen;
    }

    // ==================== Sprites generados ====================
    // Corazón pixel-art: 0 = transparente, X = relleno (blanco, se tiñe), el contorno se calcula solo
    static readonly string[] MascaraCorazon =
    {
        ".XX.XX.",
        "XXXXXXX",
        "XXXXXXX",
        ".XXXXX.",
        "..XXX..",
        "...X...",
    };

    static Sprite CrearSpriteCorazon()
    {
        string ruta = CarpetaUI + "/corazon.png";
        if (!File.Exists(ruta))
        {
            int w = MascaraCorazon[0].Length + 2, h = MascaraCorazon.Length + 2;   // +1 px de contorno
            bool[,] relleno = new bool[w, h];
            for (int fila = 0; fila < MascaraCorazon.Length; fila++)
                for (int col = 0; col < MascaraCorazon[fila].Length; col++)
                    relleno[col + 1, h - 2 - fila] = MascaraCorazon[fila][col] == 'X';   // la textura empieza abajo

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    Color c = Color.clear;
                    if (relleno[x, y]) c = Color.white;
                    else if (Vecino(relleno, x, y, w, h)) c = Color.black;
                    tex.SetPixel(x, y, c);
                }
            GuardarPNG(tex, ruta);
        }
        return CargarSprite(ruta);
    }

    static bool Vecino(bool[,] m, int x, int y, int w, int h)
    {
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i], ny = y + dy[i];
            if (nx >= 0 && ny >= 0 && nx < w && ny < h && m[nx, ny]) return true;
        }
        return false;
    }

    // Mira: cruz con hueco al centro y punto, blanca con contorno negro
    static readonly string[] MascaraMira =
    {
        "....X....",
        "....X....",
        ".........",
        ".........",
        "XX..X..XX",
        ".........",
        ".........",
        "....X....",
        "....X....",
    };

    static Sprite CrearSpriteMira()
    {
        string ruta = CarpetaUI + "/mira.png";
        if (!File.Exists(ruta))
        {
            int w = MascaraMira[0].Length + 2, h = MascaraMira.Length + 2;
            bool[,] relleno = new bool[w, h];
            for (int fila = 0; fila < MascaraMira.Length; fila++)
                for (int col = 0; col < MascaraMira[fila].Length; col++)
                    relleno[col + 1, h - 2 - fila] = MascaraMira[fila][col] == 'X';

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    Color c = Color.clear;
                    if (relleno[x, y]) c = Color.white;
                    else if (Vecino(relleno, x, y, w, h)) c = new Color(0, 0, 0, 0.8f);
                    tex.SetPixel(x, y, c);
                }
            GuardarPNG(tex, ruta);
        }
        return CargarSprite(ruta);
    }

    static Sprite CrearSpriteBlanco()
    {
        string ruta = CarpetaUI + "/blanco.png";
        if (!File.Exists(ruta))
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++) tex.SetPixel(x, y, Color.white);
            GuardarPNG(tex, ruta);
        }
        return CargarSprite(ruta);
    }

    static void GuardarPNG(Texture2D tex, string ruta)
    {
        if (!AssetDatabase.IsValidFolder(CarpetaUI)) AssetDatabase.CreateFolder("Assets", "UI");
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(ruta);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.filterMode = FilterMode.Point;          // pixel-art nítido
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
    }

    static Sprite CargarSprite(string ruta)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }
}
