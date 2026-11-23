using UnityEngine;

// Reproduce en loop la animación que traiga el modelo (ej. las llamas de la bola de fuego).
public class AnimacionEnLoop : MonoBehaviour
{
    public float velocidad = 1f;
    public bool inicioAlAzar = true;   // cada bola empieza en un punto distinto de la animación

    void Start()
    {
        Animation anim = GetComponentInChildren<Animation>();
        if (anim == null || anim.clip == null) return;

        AnimationState st = anim[anim.clip.name];
        st.wrapMode = WrapMode.Loop;
        st.speed = velocidad;
        if (inicioAlAzar) st.time = Random.Range(0f, st.length);
        anim.Play(anim.clip.name);
    }
}
