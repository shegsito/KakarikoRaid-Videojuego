using UnityEngine;

// Efecto visual de un corte de espada: un arco brillante que se "dibuja" barriendo y se desvanece.
// - Corte normal: media luna blanca, delgada en las puntas (como un tajo).
// - Giro: anillo de fuego completo alrededor de Link.
// La malla se genera por código cada frame (no necesita texturas ni partículas).
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EfectoCorte : MonoBehaviour
{
    // Parámetros (los pone Crear)
    float radio, grosor, anguloInicio, anguloTotal, duracion;
    Color nucleo, borde;
    bool mediaLuna;            // true = afilado en las puntas (tajo); false = grosor parejo (anillo)

    float t;
    Mesh mesh;
    const int Segmentos = 48;

    static Material materialCompartido;
    static Texture2D texturaCompartida;

    /// <summary>
    /// Crea un corte. Los ángulos se miden desde el frente (+Z) hacia la derecha (+X), en grados.
    /// </summary>
    public static EfectoCorte Crear(Vector3 posicion, Quaternion rotacion, float radio, float grosor,
                                    float anguloInicio, float anguloTotal, float duracion,
                                    Color nucleo, Color borde, bool mediaLuna, Material material = null)
    {
        GameObject go = new GameObject("EfectoCorte");
        go.transform.SetPositionAndRotation(posicion, rotacion);
        EfectoCorte e = go.AddComponent<EfectoCorte>();
        e.radio = radio; e.grosor = grosor;
        e.anguloInicio = anguloInicio; e.anguloTotal = anguloTotal;
        e.duracion = duracion; e.nucleo = nucleo; e.borde = borde; e.mediaLuna = mediaLuna;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = material != null ? material : MaterialPorDefecto();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        e.mesh = new Mesh { name = "Corte" };
        e.mesh.MarkDynamic();
        go.GetComponent<MeshFilter>().sharedMesh = e.mesh;
        e.Construir(0f);
        return e;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = t / duracion;
        if (k >= 1f) { Destroy(mesh); Destroy(gameObject); return; }
        Construir(k);
    }

    // k: 0 -> 1 durante la vida del efecto
    void Construir(float k)
    {
        // Primer 45%: el arco se dibuja (barrido). Después: se desvanece y se abre un poco.
        float barrido = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.45f));
        float desvanecer = k < 0.45f ? 1f : 1f - (k - 0.45f) / 0.55f;
        float abrir = 1f + 0.15f * k;

        int n = Segmentos + 1;
        Vector3[] v = new Vector3[n * 2];
        Vector2[] uv = new Vector2[n * 2];
        Color[] c = new Color[n * 2];
        int[] tri = new int[Segmentos * 6];

        float total = anguloTotal * Mathf.Max(barrido, 0.02f);
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)Segmentos;                     // 0 = cola, 1 = punta (lo más nuevo)
            float ang = (anguloInicio + total * u) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));

            float g = mediaLuna ? grosor * Mathf.Sin(u * Mathf.PI) : grosor * Mathf.Lerp(0.35f, 1f, u);
            float rExt = radio * abrir;
            float rInt = Mathf.Max(0f, rExt - g);

            v[i * 2] = dir * rInt;
            v[i * 2 + 1] = dir * rExt;
            uv[i * 2] = new Vector2(u, 0f);
            uv[i * 2 + 1] = new Vector2(u, 1f);

            // La cola se apaga; el borde exterior brilla más
            float a = Mathf.Pow(u, 0.6f) * desvanecer;
            c[i * 2] = new Color(borde.r, borde.g, borde.b, borde.a * a * 0.35f);
            c[i * 2 + 1] = new Color(nucleo.r, nucleo.g, nucleo.b, nucleo.a * a);

            if (i < Segmentos)
            {
                int b = i * 6, p = i * 2;
                tri[b] = p; tri[b + 1] = p + 1; tri[b + 2] = p + 2;
                tri[b + 3] = p + 1; tri[b + 4] = p + 3; tri[b + 5] = p + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = v;
        mesh.uv = uv;
        mesh.colors = c;
        mesh.triangles = tri;
        mesh.RecalculateBounds();
    }

    // ---------- Material aditivo (brilla) con textura de "estelas" generada por código ----------
    public static Material MaterialPorDefecto()
    {
        if (materialCompartido != null) return materialCompartido;
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        materialCompartido = CrearMaterial(s);
        return materialCompartido;
    }

    public static Material CrearMaterial(Shader shader)
    {
        Material m = new Material(shader) { name = "EfectoCorte" };
        m.mainTexture = TexturaEstelas();
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);   // transparente
            m.SetFloat("_Blend", 2f);     // aditivo
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);      // se ve por ambos lados
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return m;
    }

    // u = a lo largo del arco, v = de adentro (0) hacia afuera (1)
    public static Texture2D TexturaEstelas()
    {
        if (texturaCompartida != null) return texturaCompartida;
        const int W = 128, H = 32;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Estelas" };
        System.Random azar = new System.Random(7);
        float[] estela = new float[H];
        for (int y = 0; y < H; y++) estela[y] = 0.55f + 0.45f * (float)azar.NextDouble();   // rayas finas

        for (int y = 0; y < H; y++)
        {
            float v = y / (float)(H - 1);
            // Brilla cerca del borde exterior y se desvanece hacia adentro; borde exterior suave
            float bordeSuave = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, v));
            float perfil = Mathf.Pow(v, 1.6f) * bordeSuave * 1.6f;
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float ruido = 0.75f + 0.25f * Mathf.PerlinNoise(u * 12f, v * 30f);
                float a = Mathf.Clamp01(perfil * estela[y] * ruido);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        texturaCompartida = tex;
        return tex;
    }
}
