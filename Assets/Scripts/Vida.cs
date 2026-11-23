using UnityEngine;
using System;

// Vida genérica para Link y enemigos. Se mide en corazones (acepta medios: 0.5).
public class Vida : MonoBehaviour
{
    public float vidaMaxima = 10f;
    public float invulnerabilidad = 0f;   // segundos sin recibir daño tras un golpe (Link: 0.5)

    [HideInInspector] public bool invencible = false;   // Link al rodar
    private bool esJugador;                             // Link: suena al recibir golpes

    public float Actual { get; private set; }
    public bool Muerto { get; private set; }

    // (vida actual, vida máxima) -> el HUD se suscribe aquí
    public event Action<float, float> OnCambio;
    public event Action OnMuerte;

    // Si devuelve true, el golpe se bloqueó (lo usará el escudo). Recibe la posición del atacante.
    public Func<Vector3, bool> Bloqueo;

    private float ultimoGolpe = -999f;

    void Awake()
    {
        Actual = vidaMaxima;
        esJugador = GetComponent<LinkMovimiento>() != null;
    }

    void Start()
    {
        OnCambio?.Invoke(Actual, vidaMaxima);
    }

    public void RecibirDaño(float corazones, Vector3 origen)
    {
        if (Muerto || invencible) return;
        if (Time.time - ultimoGolpe < invulnerabilidad) return;
        if (Bloqueo != null && Bloqueo(origen)) return;

        ultimoGolpe = Time.time;
        Actual = Mathf.Max(0f, Actual - corazones);
        OnCambio?.Invoke(Actual, vidaMaxima);
        if (esJugador)
        {
            GestorAudio.Reproducir(GestorAudio.Sfx.GolpeRecibido);
            // Voz de Link: grito de daño, o el de morir si este golpe lo mató
            GestorAudio.Reproducir(Actual <= 0f ? GestorAudio.Sfx.VozMorir : GestorAudio.Sfx.VozDanio);
        }
        Debug.Log(name + " recibió " + corazones + " de daño. Vida: " + Actual + "/" + vidaMaxima);

        if (Actual <= 0f)
        {
            Muerto = true;
            OnMuerte?.Invoke();
        }
    }

    public void Curar(float corazones)
    {
        if (Muerto) return;
        Actual = Mathf.Min(vidaMaxima, Actual + corazones);
        OnCambio?.Invoke(Actual, vidaMaxima);
    }

    public void CurarTodo()
    {
        Curar(vidaMaxima);
    }
}
