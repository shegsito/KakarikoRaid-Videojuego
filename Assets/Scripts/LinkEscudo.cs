using UnityEngine;
using System;
using System.Collections;

// Escudo de Link: mantener click derecho bloquea los golpes que vienen de frente.
// Cada bloqueo empuja a Link hacia atrás. Tras 5 bloqueos el escudo se rompe 10 segundos.
[RequireComponent(typeof(Vida))]
public class LinkEscudo : MonoBehaviour
{
    public float anguloBloqueo = 70f;       // grados a cada lado del frente que cubre el escudo
    public int golpesMaximos = 5;
    public float tiempoRoto = 10f;
    public float knockback = 1f;            // metros que retrocede Link al bloquear
    public float factorVelocidad = 0.5f;    // camina más lento mientras se cubre
    public Transform escudoVisual;          // modelo del escudo (si está vacío se crea un cubo)

    public bool Bloqueando { get; private set; }
    public bool Roto { get; private set; }
    public int GolpesRestantes { get; private set; }

    // (golpes restantes, roto) -> para el HUD
    public event Action<int, bool> OnEscudoCambio;

    private LinkMovimiento movimiento;
    private Rigidbody rb;
    private Renderer rendererEscudo;

    [Header("Indicador (barrera azul enfrente de Link)")]
    public bool mostrarIndicador = true;
    public Color colorLleno = new Color(0.3f, 0.75f, 1f);    // escudo con todos sus golpes
    public Color colorCasiRoto = new Color(1f, 0.3f, 0.2f);  // le queda 1 golpe
    private MeshRenderer indicador;
    private MaterialPropertyBlock bloque;
    private float destello;                                  // 1 al bloquear, baja a 0
    private AnimadorModelo animador;

    void Start()
    {
        movimiento = GetComponent<LinkMovimiento>();
        rb = GetComponent<Rigidbody>();
        GetComponent<Vida>().Bloqueo = IntentarBloquear;
        GolpesRestantes = golpesMaximos;

        if (escudoVisual == null) escudoVisual = CrearEscudoTemporal();
        rendererEscudo = escudoVisual.GetComponentInChildren<Renderer>();
        escudoVisual.gameObject.SetActive(false);

        if (mostrarIndicador) CrearIndicador();
        OnEscudoCambio?.Invoke(GolpesRestantes, Roto);
    }

    // Barrera curva que cubre exactamente el ángulo que bloquea el escudo
    void CrearIndicador()
    {
        GameObject go = new GameObject("IndicadorEscudo", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(transform, false);
        Mesh m = new Mesh { name = "Barrera" };
        const int seg = 32;
        float radio = 1.05f, abajo = -0.75f, arriba = 0.7f;
        Vector3[] v = new Vector3[(seg + 1) * 2];
        Vector2[] uv = new Vector2[v.Length];
        Color[] c = new Color[v.Length];
        int[] tri = new int[seg * 6];
        for (int i = 0; i <= seg; i++)
        {
            float u = i / (float)seg;
            float ang = Mathf.Lerp(-anguloBloqueo, anguloBloqueo, u) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * radio;
            float orilla = Mathf.Sin(u * Mathf.PI);               // se desvanece en los lados
            v[i * 2] = dir + Vector3.up * abajo;
            v[i * 2 + 1] = dir + Vector3.up * arriba;
            uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
            c[i * 2] = new Color(1f, 1f, 1f, 0.25f * orilla);
            c[i * 2 + 1] = new Color(1f, 1f, 1f, 0.9f * orilla);
            if (i < seg)
            {
                int b = i * 6, p = i * 2;
                tri[b] = p; tri[b + 1] = p + 1; tri[b + 2] = p + 2;
                tri[b + 3] = p + 1; tri[b + 4] = p + 3; tri[b + 5] = p + 2;
            }
        }
        m.vertices = v; m.uv = uv; m.colors = c; m.triangles = tri; m.RecalculateBounds();
        go.GetComponent<MeshFilter>().sharedMesh = m;
        indicador = go.GetComponent<MeshRenderer>();
        indicador.sharedMaterial = EfectoCorte.MaterialPorDefecto();
        indicador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        indicador.enabled = false;
        bloque = new MaterialPropertyBlock();
    }

    void ActualizarIndicador()
    {
        if (indicador == null) return;
        indicador.enabled = Bloqueando;
        if (!Bloqueando) return;

        destello = Mathf.MoveTowards(destello, 0f, Time.deltaTime * 6f);
        float vida = golpesMaximos > 1 ? (GolpesRestantes - 1) / (float)(golpesMaximos - 1) : 1f;
        Color col = Color.Lerp(colorCasiRoto, colorLleno, vida);
        col = Color.Lerp(col, Color.white, destello);
        col.a = 0.6f + 0.15f * Mathf.Sin(Time.time * 6f) + 0.4f * destello;   // late suavemente
        bloque.SetColor("_BaseColor", col);
        indicador.SetPropertyBlock(bloque);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        Bloqueando = Input.GetMouseButton(1) && !Roto;
        escudoVisual.gameObject.SetActive(Bloqueando);
        if (movimiento != null) movimiento.factorVelocidad = Bloqueando ? factorVelocidad : 1f;

        if (animador == null) animador = GetComponent<AnimadorModelo>();
        if (animador != null) animador.cubriendose = Bloqueando;   // Link se agacha detrás del escudo
        ActualizarIndicador();
    }

    // Vida.cs llama esto antes de aplicar daño. true = golpe bloqueado.
    bool IntentarBloquear(Vector3 origen)
    {
        if (!Bloqueando) return false;

        Vector3 haciaAtacante = origen - transform.position;
        haciaAtacante.y = 0f;
        if (Vector3.Angle(transform.forward, haciaAtacante) > anguloBloqueo) return false;   // vino de lado/atrás

        GolpesRestantes--;
        GestorAudio.Reproducir(GestorAudio.Sfx.GolpeEscudo);
        destello = 1f;
        StartCoroutine(Retroceder(-haciaAtacante.normalized));

        if (GolpesRestantes <= 0) StartCoroutine(Romper());
        OnEscudoCambio?.Invoke(GolpesRestantes, Roto);
        return true;
    }

    IEnumerator Retroceder(Vector3 direccion)
    {
        if (movimiento != null) movimiento.puedeMoverse = false;
        float duracion = 0.12f, t = 0f;
        while (t < duracion)
        {
            Vector3 v = direccion * (knockback / duracion);
            v.y = rb.linearVelocity.y;
            rb.linearVelocity = v;
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        if (movimiento != null) movimiento.puedeMoverse = true;
    }

    IEnumerator Romper()
    {
        Roto = true;
        Bloqueando = false;
        Debug.Log("¡El escudo se rompió! Vuelve en " + tiempoRoto + " s");
        OnEscudoCambio?.Invoke(0, true);

        yield return new WaitForSeconds(tiempoRoto);

        Roto = false;
        GolpesRestantes = golpesMaximos;
        Debug.Log("Escudo recuperado");
        OnEscudoCambio?.Invoke(GolpesRestantes, false);
    }

    // Placeholder: una placa enfrente de Link
    Transform CrearEscudoTemporal()
    {
        GameObject placa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placa.name = "EscudoTemporal";
        Destroy(placa.GetComponent<Collider>());
        placa.transform.SetParent(transform);
        placa.transform.localPosition = new Vector3(0f, 0.1f, 0.65f);
        placa.transform.localRotation = Quaternion.identity;
        placa.transform.localScale = new Vector3(0.9f, 1.1f, 0.1f);
        placa.GetComponent<Renderer>().material.color = new Color(0.3f, 0.45f, 0.9f);
        return placa.transform;
    }
}
