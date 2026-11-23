using UnityEngine;
using UnityEngine.SceneManagement;

// Menú principal: botón START carga el juego, SALIR cierra la aplicación.
public class MenuPrincipal : MonoBehaviour
{
    public string escenaJuego = "Game";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;   // el juego bloquea el cursor; aquí se libera
        Cursor.visible = true;

        AsegurarEventSystem.Revisar();

        // Conectar los botones por su nombre (si no tienen nada en On Click)
        foreach (UnityEngine.UI.Button b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.onClick.GetPersistentEventCount() > 0) continue;
            string n = b.name.ToLower();
            if (n.Contains("start") || n.Contains("jugar")) b.onClick.AddListener(Jugar);
            else if (n.Contains("salir")) b.onClick.AddListener(Salir);
        }
    }

    public void Jugar()
    {
        GestorAudio.Reproducir(GestorAudio.Sfx.Boton);
        SceneManager.LoadScene(escenaJuego);
    }

    public void Salir()
    {
        GestorAudio.Reproducir(GestorAudio.Sfx.Boton);
        Debug.Log("Salir del juego");
        Application.Quit();   // no hace nada dentro del editor, solo en el juego compilado
    }
}
