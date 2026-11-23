using UnityEngine;

// Esqueleto: solo persigue y golpea. 1/2 corazón por golpe, 1 golpe por segundo.
public class Esqueleto : Enemigo
{
    public float daño = 0.5f;

    protected override void Atacar(float distancia)
    {
        GolpearLink(daño);
    }
}
