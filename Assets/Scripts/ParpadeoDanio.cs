using UnityEngine;
using System.Collections;

// Pone rojo el modelo un instante cuando recibe daño (Link). Parpadea dos veces, como en Zelda.
// Se agrega solo desde LinkMovimiento.
[RequireComponent(typeof(Vida))]
public class ParpadeoDanio : MonoBehaviour
{
    public Color colorDaño = Color.red;
    public int parpadeos = 2;
    public float tiempoRojo = 0.1f;
    public float tiempoNormal = 0.07f;

    private Vida vida;
    private float vidaAnterior;
    private Renderer[] renderers;
    private Material[][] materiales;
    private Color[][] originales;
    private Coroutine rutina;

    void Start()
    {
        vida = GetComponent<Vida>();
        vidaAnterior = vida.Actual;
        vida.OnCambio += AlCambiarVida;

        // Guardar los materiales y colores originales (sin los efectos ni la barrera del escudo)
        var lista = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r.name == "IndicadorEscudo" || r.GetComponent<EfectoCorte>() != null) continue;
            if (r is ParticleSystemRenderer) continue;
            lista.Add(r);
        }
        renderers = lista.ToArray();
        materiales = new Material[renderers.Length][];
        originales = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            materiales[i] = renderers[i].materials;   // copia propia de cada material (no cambia el asset)
            originales[i] = new Color[materiales[i].Length];
            for (int j = 0; j < materiales[i].Length; j++)
                originales[i][j] = materiales[i][j].HasProperty("_Color") || materiales[i][j].HasProperty("_BaseColor")
                    ? materiales[i][j].color : Color.white;
        }
    }

    void OnDestroy()
    {
        if (vida != null) vida.OnCambio -= AlCambiarVida;
    }

    void AlCambiarVida(float actual, float maxima)
    {
        if (actual < vidaAnterior)
        {
            if (rutina != null) { StopCoroutine(rutina); Pintar(false); }
            rutina = StartCoroutine(Parpadear());
        }
        vidaAnterior = actual;
    }

    IEnumerator Parpadear()
    {
        for (int n = 0; n < parpadeos; n++)
        {
            Pintar(true);
            yield return new WaitForSecondsRealtime(tiempoRojo);    // funciona aunque el juego se congele al morir
            Pintar(false);
            yield return new WaitForSecondsRealtime(tiempoNormal);
        }
        rutina = null;
    }

    void Pintar(bool rojo)
    {
        if (materiales == null) return;
        for (int i = 0; i < materiales.Length; i++)
        {
            if (renderers[i] == null) continue;
            for (int j = 0; j < materiales[i].Length; j++)
            {
                Material m = materiales[i][j];
                if (m == null) continue;
                m.color = rojo ? colorDaño : originales[i][j];
            }
        }
    }
}
