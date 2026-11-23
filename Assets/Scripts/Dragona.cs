using UnityEngine;

// Dragona: vuela, mantiene distancia de Link y le dispara bolas de fuego (2 corazones cada 2.5 s).
public class Dragona : Enemigo
{
    [Header("Dragona")]
    public GameObject prefabBolaFuego;
    public float alturaVuelo = 3f;          // metros por encima de Link
    public float distanciaPreferida = 8f;   // se queda a esta distancia
    public float daño = 2f;

    // Unity llama Reset() al agregar el componente: valores del GDD por defecto
    void Reset()
    {
        velocidad = 7f;          // rápida
        rangoAtaque = 15f;       // dispara desde lejos
        cadenciaAtaque = 2.5f;   // 1 disparo cada 2.5 s
        usarNavMesh = false;     // vuela: no necesita buscar camino
    }

    public float alturaExtraMaxima = 12f;   // cuánto puede subir para pasar sobre casas
    private float alturaExtra;

    protected override void Start()
    {
        usarNavMesh = false;
        base.Start();
        rb.useGravity = false;   // vuela
        if (animador != null) animador.volador = true;   // usa la animación de volar
    }

    // En vez de pegarse a Link, se acerca/aleja hasta su distancia preferida y lo rodea
    protected override void Mover(Vector3 direccion, float distancia)
    {
        Vector3 v;
        if (distancia > distanciaPreferida + 1f)       v = direccion * velocidad;           // acercarse
        else if (distancia < distanciaPreferida - 1f)  v = -direccion * velocidad;          // alejarse
        else v = Vector3.Cross(Vector3.up, direccion) * velocidad * 0.5f;                   // rodearlo

        // ¿Hay una casa enfrente? Subir para pasarla por encima
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        bool bloqueada = false;
        if (horizontal.sqrMagnitude > 0.01f)
        {
            foreach (RaycastHit h in Physics.SphereCastAll(transform.position, 0.6f, horizontal.normalized, 4f))
            {
                if (h.collider.isTrigger || h.transform.IsChildOf(transform)) continue;
                if (h.collider.GetComponentInParent<Enemigo>() != null) continue;
                if (h.collider.GetComponentInParent<LinkMovimiento>() != null) continue;
                bloqueada = true;
                break;
            }
        }
        alturaExtra = bloqueada
            ? Mathf.MoveTowards(alturaExtra, alturaExtraMaxima, 8f * Time.fixedDeltaTime)
            : Mathf.MoveTowards(alturaExtra, 0f, 2f * Time.fixedDeltaTime);
        if (bloqueada) { v.x *= 0.3f; v.z *= 0.3f; }   // frena mientras sube

        // Subir/bajar hacia la altura de vuelo
        float alturaObjetivo = link.position.y + alturaVuelo + alturaExtra;
        v.y = (alturaObjetivo - transform.position.y) * 2f;

        rb.linearVelocity = v;
    }

    protected override void Atacar(float distancia)
    {
        if (prefabBolaFuego == null) return;

        Vector3 salida = transform.position + transform.forward * 0.8f;
        Vector3 objetivo = link.position + Vector3.up * 0.5f;   // al pecho de Link

        GestorAudio.Reproducir(GestorAudio.Sfx.DisparoDragona, 0.7f);
        GameObject bola = Instantiate(prefabBolaFuego, salida, Quaternion.LookRotation(objetivo - salida));
        Proyectil p = bola.GetComponent<Proyectil>();
        if (p == null)
        {
            Debug.LogWarning("El prefab '" + prefabBolaFuego.name + "' no tiene el script Proyectil. Se lo agrego, pero agrégalo al prefab.");
            p = bola.AddComponent<Proyectil>();
        }
        p.dañaA = Proyectil.Bando.Link;
        p.daño = daño;
    }
}
