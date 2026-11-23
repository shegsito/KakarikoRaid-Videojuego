using UnityEngine;
using System;

// Goblin (jefe): lento, mucha vida, sin knockback.
// Si Link está cerca lo golpea (1.5 corazones/s); si está lejos le lanza rocas (3 corazones).
public class Goblin : Enemigo
{
    [Header("Jefe")]
    public float rangoMelee = 4f;           // a esta distancia golpea en vez de lanzar
    public float dañoGolpe = 1.5f;
    public GameObject prefabRoca;
    public float dañoRoca = 3f;
    public float cadenciaRoca = 3f;         // segundos entre rocas

    // El HUD se suscribe para mostrar la barra de vida del jefe
    public static event Action<Goblin> OnGoblinAparece;
    public Vida VidaJefe { get { return vida; } }

    private float siguienteRoca;

    // Valores del GDD al agregar el componente
    void Reset()
    {
        velocidad = 2f;            // lento
        rangoAtaque = 25f;         // desde aquí ya puede lanzar rocas
        cadenciaAtaque = 1f;       // 1 golpe por segundo de cerca
        recibeKnockback = false;
        probCorazon = 1f;          // el jefe siempre suelta corazón
    }

    protected override void Start()
    {
        base.Start();
        siguienteRoca = Time.time + 2f;
        OnGoblinAparece?.Invoke(this);
        GestorAudio.Reproducir(GestorAudio.Sfx.RugidoGoblin);   // el jefe anuncia su llegada
        if (animador != null) { animador.pesado = true; animador.Rugir(); }
    }

    protected override bool AnimarAtaqueAutomatico { get { return false; } }

    // Camina hasta estar a distancia de golpe (no se queda a 25 m)
    protected override void Mover(Vector3 direccion, float distancia)
    {
        Vector3 v = distancia > rangoMelee * 0.8f ? direccion * velocidad : Vector3.zero;
        v.y = rb.linearVelocity.y;
        rb.linearVelocity = v;
    }

    protected override void Atacar(float distancia)
    {
        if (distancia <= rangoMelee)
        {
            if (animador != null) animador.Atacar(AnimadorModelo.TipoAtaque.Golpe);
            GolpearLink(dañoGolpe);
        }
        else if (Time.time >= siguienteRoca && prefabRoca != null)
        {
            siguienteRoca = Time.time + cadenciaRoca;
            if (animador != null) animador.Atacar(AnimadorModelo.TipoAtaque.Lanzar);
            LanzarRoca();
        }
    }

    void LanzarRoca()
    {
        // Sale de arriba de su cabeza hacia el pecho de Link
        Vector3 salida = transform.position + Vector3.up * (transform.localScale.y * 0.8f) + transform.forward * 1.5f;
        Vector3 objetivo = link.position + Vector3.up * 0.5f;

        GestorAudio.Reproducir(GestorAudio.Sfx.GoblinDisparo);
        GameObject roca = Instantiate(prefabRoca, salida, Quaternion.LookRotation(objetivo - salida));
        Proyectil p = roca.GetComponent<Proyectil>();
        if (p == null) p = roca.AddComponent<Proyectil>();
        p.dañaA = Proyectil.Bando.Link;
        p.daño = dañoRoca;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoMelee);
    }
}
