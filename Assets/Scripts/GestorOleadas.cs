using UnityEngine;
using System;
using System.Collections;

// Controla las rondas: cuántos enemigos salen, dónde aparecen y cuándo empieza la siguiente.
public class GestorOleadas : MonoBehaviour
{
    public enum BaseDragonas { Ronda, Esqueletos }

    [Header("Prefabs (los que estén vacíos no se generan)")]
    public GameObject prefabEsqueleto;
    public GameObject prefabDragona;
    public GameObject prefabGoblin;

    [Header("Dónde aparecen")]
    public Transform contenedorSpawns;      // objeto padre: todos sus hijos son puntos de spawn
    public Transform[] puntosSpawn;         // (opcional) o asignarlos uno por uno
    public float dispersion = 2f;           // radio aleatorio alrededor de cada punto

    [Header("Fórmulas")]
    public int esqueletosPorRonda = 4;      // ronda 1 = 4, ronda 2 = 8, ...
    public BaseDragonas baseDragonas = BaseDragonas.Ronda;
    public float factorDragonas = 0.7f;     // dragonas = redondear(base * 0.7)
    public int rondaInicioDragonas = 2;
    public int cadaCuantasGoblin = 10;      // el Goblin aparece en las rondas 10, 20, 30...
    public bool sumarGoblins = false;       // false = siempre 1; true = ronda 10 -> 1, ronda 20 -> 2, ...

    [Header("Tiempos")]
    public float descansoEntreRondas = 3f;
    public float intervaloSpawn = 0.4f;     // para que no salgan todos en el mismo frame

    [Header("Curación")]
    public int cadaCuantasCurar = 3;        // al terminar la ronda 3, 6, 9... Link se cura
    public float corazonesCuracion = 10f;

    [Header("Pruebas")]
    public int rondaInicial = 1;            // ponlo en 5 para probar al Goblin
    public bool mostrarDebug = true;        // texto temporal en pantalla hasta tener HUD

    // Eventos para el HUD y el GestorPartida (como en Lab3)
    public static event Action<int> OnRondaInicia;      // número de ronda
    public static event Action<int> OnRondaTerminada;   // número de ronda

    public int Ronda { get; private set; }
    public int Vivos { get; private set; }

    private bool generando;
    private Vida vidaLink;

    void OnEnable()  { Enemigo.OnEnemigoMuerto += AlMorirEnemigo; }
    void OnDisable() { Enemigo.OnEnemigoMuerto -= AlMorirEnemigo; }

    void Start()
    {
        LinkMovimiento link = FindFirstObjectByType<LinkMovimiento>();
        if (link != null) vidaLink = link.GetComponent<Vida>();

        // Si hay un objeto padre, usar todos sus hijos como puntos de spawn
        if (contenedorSpawns != null && contenedorSpawns.childCount > 0)
        {
            puntosSpawn = new Transform[contenedorSpawns.childCount];
            for (int i = 0; i < contenedorSpawns.childCount; i++)
                puntosSpawn[i] = contenedorSpawns.GetChild(i);
        }

        if (prefabEsqueleto == null)
            Debug.LogWarning("GestorOleadas (" + name + "): falta asignar el Prefab Esqueleto");
        if (FindObjectsByType<GestorOleadas>(FindObjectsSortMode.None).Length > 1)
            Debug.LogWarning("Hay más de un GestorOleadas en la escena. Solo debe haber uno.");

        Ronda = rondaInicial - 1;
        StartCoroutine(SiguienteRonda());
    }

    // Spawn más cercano a un punto (los enemigos atorados reaparecen ahí)
    public Transform SpawnMasCercano(Vector3 p)
    {
        Transform mejor = null;
        float min = Mathf.Infinity;
        if (puntosSpawn == null) return null;
        foreach (Transform t in puntosSpawn)
        {
            if (t == null) continue;
            float d = (t.position - p).sqrMagnitude;
            if (d < min) { min = d; mejor = t; }
        }
        return mejor;
    }

    // --- Fórmulas del GDD ---
    public int CuantosEsqueletos(int r) { return esqueletosPorRonda * r; }

    public int CuantasDragonas(int r)
    {
        if (r < rondaInicioDragonas) return 0;
        float b = baseDragonas == BaseDragonas.Ronda ? r : CuantosEsqueletos(r);
        return Mathf.RoundToInt(b * factorDragonas);
    }

    public int CuantosGoblins(int r)
    {
        if (cadaCuantasGoblin <= 0 || r % cadaCuantasGoblin != 0) return 0;
        return sumarGoblins ? r / cadaCuantasGoblin : 1;
    }

    // --- Flujo de rondas ---
    IEnumerator SiguienteRonda()
    {
        yield return new WaitForSeconds(descansoEntreRondas);

        Ronda++;
        Debug.Log("=== Ronda " + Ronda + " ===");
        OnRondaInicia?.Invoke(Ronda);

        generando = true;
        yield return Generar(prefabGoblin, CuantosGoblins(Ronda));
        yield return Generar(prefabDragona, CuantasDragonas(Ronda));
        yield return Generar(prefabEsqueleto, CuantosEsqueletos(Ronda));
        generando = false;

        RevisarFinDeRonda();   // por si no se generó nada
    }

    IEnumerator Generar(GameObject prefab, int cantidad)
    {
        if (prefab == null || cantidad <= 0) yield break;
        if (puntosSpawn == null || puntosSpawn.Length == 0)
        {
            Debug.LogWarning("GestorOleadas: no hay puntos de spawn asignados");
            yield break;
        }

        for (int i = 0; i < cantidad; i++)
        {
            Transform punto = puntosSpawn[UnityEngine.Random.Range(0, puntosSpawn.Length)];
            Vector2 azar = UnityEngine.Random.insideUnitCircle * dispersion;
            Vector3 pos = punto.position + new Vector3(azar.x, 0f, azar.y);

            Instantiate(prefab, pos, punto.rotation);
            Vivos++;
            yield return new WaitForSeconds(intervaloSpawn);
        }
    }

    void AlMorirEnemigo(Enemigo e)
    {
        Vivos = Mathf.Max(0, Vivos - 1);
        RevisarFinDeRonda();
    }

    void RevisarFinDeRonda()
    {
        if (generando || Vivos > 0) return;

        Debug.Log("Ronda " + Ronda + " completada");
        if (cadaCuantasCurar > 0 && Ronda % cadaCuantasCurar == 0 && vidaLink != null)
            vidaLink.Curar(corazonesCuracion);

        OnRondaTerminada?.Invoke(Ronda);
        StartCoroutine(SiguienteRonda());
    }

    // Texto temporal (se quita cuando tengamos el HUD)
    void OnGUI()
    {
        if (!mostrarDebug) return;
        string texto = "Ronda: " + Ronda + "   Enemigos: " + Vivos;
        if (vidaLink != null) texto += "   Vida: " + vidaLink.Actual + "/" + vidaLink.vidaMaxima;
        GUI.Label(new Rect(10, 10, 500, 25), texto);
    }
}
