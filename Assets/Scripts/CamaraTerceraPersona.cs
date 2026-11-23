using UnityEngine;

// Cámara en tercera persona que gira con el mouse alrededor de Link.
// Con el arco (primeraPersona = true) se coloca en la cabeza de Link.
public class CamaraTerceraPersona : MonoBehaviour
{
    public Transform objetivo;             // Link (se asigna en el Inspector)
    public float distancia = 6f;           // qué tan atrás va la cámara
    public float altura = 1.5f;            // a qué altura de Link mira
    public float hombro = 0.8f;            // desplazamiento a la derecha (vista sobre el hombro, 0 = centrada)
    public float sensibilidad = 3f;
    public float minVertical = -20f;
    public float maxVertical = 60f;

    [HideInInspector] public bool primeraPersona = false;   // lo activa el arco

    private float giroX;   // horizontal (yaw)
    private float giroY = 15f;   // vertical (pitch)

    void Start()
    {
        // Esconde y bloquea el cursor en el centro de la pantalla
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // LateUpdate: después de que Link ya se movió (evita temblores)
    void LateUpdate()
    {
        if (objetivo == null || Time.timeScale == 0f) return;

        giroX += Input.GetAxis("Mouse X") * sensibilidad;
        giroY -= Input.GetAxis("Mouse Y") * sensibilidad;
        giroY = Mathf.Clamp(giroY, minVertical, maxVertical);

        Quaternion rotacion = Quaternion.Euler(giroY, giroX, 0f);
        Vector3 cabeza = objetivo.position + Vector3.up * altura;
        if (!primeraPersona) cabeza += rotacion * Vector3.right * hombro;

        if (primeraPersona)
        {
            transform.position = cabeza;
        }
        else
        {
            Vector3 posicion = cabeza - rotacion * Vector3.forward * distancia;

            // Si hay una pared entre Link y la cámara, acercar la cámara
            if (Physics.Linecast(cabeza, posicion, out RaycastHit hit) && !hit.transform.IsChildOf(objetivo))
                posicion = hit.point + hit.normal * 0.2f;

            transform.position = posicion;
        }

        transform.rotation = rotacion;
    }
}
