using UnityEngine;
using UnityEngine.EventSystems;

// Sin un EventSystem con el módulo de entrada correcto, los botones no reciben clics.
// GestorPartida y MenuPrincipal llaman esto al iniciar su escena.
public static class AsegurarEventSystem
{
    public static void Revisar()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        // El proyecto tiene el Input System nuevo: usar su módulo para la UI
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            StandaloneInputModule viejo = es.GetComponent<StandaloneInputModule>();
            if (viejo != null) Object.Destroy(viejo);
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#else
        if (es.GetComponent<BaseInputModule>() == null) es.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
