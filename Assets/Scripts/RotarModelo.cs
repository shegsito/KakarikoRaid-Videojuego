using UnityEngine;

// Hace girar el modelo visual (hijo "Modelo3D") mientras el proyectil vuela, como una roca lanzada.
// Gira solo el hijo: la raíz no se toca porque Proyectil avanza hacia su frente.
public class RotarModelo : MonoBehaviour
{
    public float gradosPorSegundo = 360f;
    private Transform modelo;
    private Vector3 eje;

    void Start()
    {
        modelo = transform.Find("Modelo3D");
        eje = Random.onUnitSphere;   // cada roca gira distinto
    }

    void Update()
    {
        if (modelo != null) modelo.Rotate(eje, gradosPorSegundo * Time.deltaTime, Space.World);
    }
}
