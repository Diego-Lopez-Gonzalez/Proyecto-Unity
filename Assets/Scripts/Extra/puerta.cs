using UnityEngine;
using UnityEngine.AI;

public class PuertaNavMesh : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float anguloApertura = 90f;
    [SerializeField] private float velocidadApertura = 2f;
    [SerializeField] private float rangoInteraccion = 2.5f;
    [SerializeField] private Transform bisagra;
    
    [Header("Referencias (Opcional)")]
    [SerializeField] private GameObject indicadorE;
    
    private NavMeshObstacle navObstacle;
    private bool estaAbierta = false;
    private bool estaMoviendose = false;
    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;
    private bool jugadorCerca = false;
    
    void Start()
    {
        navObstacle = GetComponent<NavMeshObstacle>();
        
        if (bisagra == null)
            bisagra = transform;
            
        rotacionCerrada = bisagra.localRotation;
        rotacionAbierta = rotacionCerrada * Quaternion.Euler(0, anguloApertura, 0);
        
        if (indicadorE != null)
            indicadorE.SetActive(false);
    }
    
    void Update()
    {
        // Detectar jugador cercano
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        
        if (jugador != null)
        {
            float distancia = Vector3.Distance(transform.position, jugador.transform.position);
            jugadorCerca = distancia <= rangoInteraccion;
            
            // Mostrar indicador
            if (indicadorE != null)
            {
                indicadorE.SetActive(jugadorCerca && !estaMoviendose);
            }
            
            // Jugador pulsa E para abrir/cerrar
            if (jugadorCerca && Input.GetKeyDown(KeyCode.E) && !estaMoviendose)
            {
                if (estaAbierta)
                    Cerrar();
                else
                    Abrir();
            }
        }
        
        // Animar la puerta
        if (estaMoviendose)
        {
            Quaternion objetivo = estaAbierta ? rotacionAbierta : rotacionCerrada;
            bisagra.localRotation = Quaternion.Lerp(bisagra.localRotation, objetivo, velocidadApertura * Time.deltaTime);
            
            if (Quaternion.Angle(bisagra.localRotation, objetivo) < 1f)
            {
                bisagra.localRotation = objetivo;
                estaMoviendose = false;
                
                // Reactivar obstáculo al cerrar
                if (!estaAbierta && navObstacle != null)
                    navObstacle.enabled = true;
            }
        }
    }
    
    // Método PÚBLICO para que los guardias puedan llamarlo
    public void Abrir()
    {
        if (!estaAbierta && !estaMoviendose)
        {
            estaAbierta = true;
            estaMoviendose = true;
            
            if (navObstacle != null)
                navObstacle.enabled = false;
                
            Debug.Log("Puerta abierta");
        }
    }
    
    // Método PÚBLICO para que los guardias puedan llamarlo
    public void Cerrar()
    {
        if (estaAbierta && !estaMoviendose)
        {
            estaAbierta = false;
            estaMoviendose = true;
            
            Debug.Log("Puerta cerrada");
        }
    }
    
    // Método para verificar si está abierta
    public bool EstaAbierta()
    {
        return estaAbierta;
    }
    
    // Método para verificar si está en movimiento
    public bool EstaMoviendose()
    {
        return estaMoviendose;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, rangoInteraccion);
    }
}