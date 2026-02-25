using UnityEngine;
using UnityEngine.AI;

public class CapaReactiva : MonoBehaviour
{
    [Header("Campo de Visión")]
    [SerializeField] private Transform jugador;
    [SerializeField] private float rangoVision = 10f;
    [SerializeField] private float anguloVision = 90f;
    [SerializeField] private LayerMask capasObstaculo;

    [Header("Puertas")]
    [SerializeField] private float rangoDeteccionPuerta = 3f;

    private NavMeshAgent navAgent;

    private bool objetoVigiladoConfirmado = false;
    private bool objetoHaDesaparecido     = false;
    private Vector3 posicionMemorizada;

    public void Inicializar(NavMeshAgent agent)
    {
        navAgent = agent;
    }

    public bool DetectarJugador(out Vector3 posicionJugador)
    {
        posicionJugador = Vector3.zero;
        if (jugador == null) return false;

        Vector3 dirAlJugador = jugador.position - transform.position;
        float distancia = dirAlJugador.magnitude;

        if (distancia > rangoVision) return false;
        if (Vector3.Angle(transform.forward, dirAlJugador) > anguloVision * 0.5f) return false;
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, dirAlJugador.normalized, distancia, capasObstaculo)) return false;

        posicionJugador = jugador.position;
        return true;
    }

    public bool DetectarDesaparicionObjeto(Transform objeto, out bool esVisibleAhora)
    {
        esVisibleAhora = false;

        if (objetoHaDesaparecido) return false;

        if (objeto == null)
        {
            if (objetoVigiladoConfirmado) { objetoHaDesaparecido = true; return true; }
            return false;
        }

        Vector3 origen = transform.position + Vector3.up * 1.5f;
        Vector3 dirAlObjeto = objeto.position - transform.position;
        float distancia = dirAlObjeto.magnitude;

        // Ver si el objeto está activo y dentro del cono
        bool enCono = distancia <= rangoVision &&
                      Vector3.Angle(transform.forward, dirAlObjeto) <= anguloVision * 0.5f;

        if (enCono && objeto.gameObject.activeInHierarchy)
        {
            esVisibleAhora = true;
            if (!objetoVigiladoConfirmado)
            {
                objetoVigiladoConfirmado = true;
                Debug.Log("[Reactiva] Objeto confirmado por primera vez.");
            }
            posicionMemorizada = objeto.position;
            return false;
        }

        if (!objetoVigiladoConfirmado) return false;

        float distanciaAMemorizada = Vector3.Distance(transform.position, posicionMemorizada);
        if (distanciaAMemorizada > rangoVision) return false;
        if (Vector3.Angle(transform.forward, posicionMemorizada - transform.position) > anguloVision * 0.5f) return false;

        // Miramos hacia donde estaba y el objeto no está activo: ha desaparecido
        Debug.Log("[Reactiva] ¡Objeto vigilado ha desaparecido!");
        objetoHaDesaparecido = true;
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    //  CAPTURA
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve true si el guardia está lo suficientemente cerca del jugador
    /// como para considerarlo atrapado. Solo tiene sentido llamarlo mientras
    /// se persigue al jugador.
    /// </summary>
    public bool DetectarCaptura(float radioCaptura)
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) <= radioCaptura;
    }

    public void GestionarPuertasCercanas()
    {
        Collider[] cercanos = Physics.OverlapSphere(transform.position, rangoDeteccionPuerta);
        foreach (Collider col in cercanos)
        {
            PuertaNavMesh puerta = col.GetComponent<PuertaNavMesh>();
            if (puerta != null && !puerta.EstaAbierta())
                puerta.Abrir();
        }
    }

    public void MoverHacia(Vector3 destino, float velocidad)
    {
        navAgent.speed = velocidad;
        navAgent.SetDestination(destino);
    }

    public bool HaLlegadoADestino()
    {
        return !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        float semi = anguloVision * 0.5f;
        Vector3 o = transform.position + Vector3.up * 1.5f;
        Gizmos.DrawRay(o, Quaternion.Euler(0, semi, 0) * transform.forward * rangoVision);
        Gizmos.DrawRay(o, Quaternion.Euler(0, -semi, 0) * transform.forward * rangoVision);
        Gizmos.DrawRay(o, transform.forward * rangoVision);
    }
}