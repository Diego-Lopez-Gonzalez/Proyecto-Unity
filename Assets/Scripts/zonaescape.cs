using UnityEngine;


[RequireComponent(typeof(Collider))]
public class ZonaEscape : MonoBehaviour
{
    [Header("Feedback (opcional)")]
    [Tooltip("Mensaje que aparece en pantalla si el jugador entra SIN todos los objetos.")]
    [SerializeField] private string mensajeFaltanObjetos = "¡Te faltan objetos para escapar!";

    [Tooltip("Segundos que permanece visible el aviso de objetos incompletos.")]
    [SerializeField] private float duracionAviso = 2f;

    private float timerAviso = 0f;
    private bool  mostrandoAviso = false;

    // Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        InventarioMago inventario = other.GetComponent<InventarioMago>();
        if (inventario == null)
        {
            Debug.LogWarning("[ZonaEscape] El jugador no tiene el componente InventarioMago.");
            return;
        }

        if (inventario.TieneTodosLosObjetos())
        {
            Debug.Log("[ZonaEscape] ¡Jugador escapó con todos los objetos! → VICTORIA");
            GameOverManager.Instancia.ActivarVictoria();
        }
        else
        {
            // Muestra aviso: faltan objetos
            int objetos   = inventario.GetObjetosRecogidos();
            Debug.Log($"[ZonaEscape] Faltan objetos. Pociones: {objetos}");

            mostrandoAviso = true;
            timerAviso     = duracionAviso;
        }
    }

    // Aviso en pantalla
    private void Update()
    {
        if (!mostrandoAviso) return;

        timerAviso -= Time.deltaTime;
        if (timerAviso <= 0f)
            mostrandoAviso = false;
    }

    private void OnGUI()
    {
        if (!mostrandoAviso) return;

        GUIStyle estilo = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 28,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        estilo.normal.textColor = Color.red;

        float ancho = 500f;
        float alto  = 60f;
        Rect rect = new Rect(
            (Screen.width  - ancho) * 0.5f,
            (Screen.height - alto)  * 0.5f + 80f,   // un poco por debajo del centro
            ancho, alto
        );

        GUI.Label(rect, mensajeFaltanObjetos, estilo);
    }

    // Gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;

        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere)
            Gizmos.DrawSphere(sphere.center, sphere.radius);

        Gizmos.color = new Color(0f, 1f, 0.3f, 0.8f);
        if (col is BoxCollider box2)
            Gizmos.DrawWireCube(box2.center, box2.size);
        else if (col is SphereCollider sphere2)
            Gizmos.DrawWireSphere(sphere2.center, sphere2.radius);
    }
}