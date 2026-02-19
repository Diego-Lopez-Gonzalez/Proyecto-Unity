using UnityEngine;
using UnityEngine.AI;

public class GuardiaPatrulla : MonoBehaviour
{
    [Header("Patrulla")]
    [SerializeField] private Transform[] puntosPatrulla;
    [SerializeField] private float tiempoEspera = 2f;
    
    [Header("Puertas")]
    [SerializeField] private float rangoDeteccionPuerta = 3f;
    
    private NavMeshAgent navAgent;
    private int indiceActual = 0;
    private float tiempoEnPunto = 0f;
    private bool esperando = false;
    
    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        
        if (puntosPatrulla.Length > 0)
        {
            navAgent.SetDestination(puntosPatrulla[0].position);
        }
    }
    
    void Update()
    {
        Patrullar();
        GestionarPuertas();
    }
    
    private void Patrullar()
    {
        if (puntosPatrulla.Length == 0) return;
        
        // Si llegó al punto
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            if (!esperando)
            {
                esperando = true;
                tiempoEnPunto = 0f;
            }
            else
            {
                tiempoEnPunto += Time.deltaTime;
                
                if (tiempoEnPunto >= tiempoEspera)
                {
                    // Ir al siguiente punto
                    indiceActual = (indiceActual + 1) % puntosPatrulla.Length;
                    navAgent.SetDestination(puntosPatrulla[indiceActual].position);
                    esperando = false;
                }
            }
        }
    }
    
    private void GestionarPuertas()
    {
        // Buscar puertas cercanas
        Collider[] cercanos = Physics.OverlapSphere(transform.position, rangoDeteccionPuerta);
        
        foreach (Collider col in cercanos)
        {
            PuertaNavMesh puerta = col.GetComponent<PuertaNavMesh>();
            
            if (puerta != null && !puerta.EstaAbierta())
            {
                // Abrir puerta si está cerrada
                puerta.Abrir();
            }
        }
    }
}