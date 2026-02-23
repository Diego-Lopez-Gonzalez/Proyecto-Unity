using UnityEngine;
using UnityEngine.AI;

public class GuardiaPatrulla : MonoBehaviour
{
    [Header("Patrulla")]
    [SerializeField] private Transform[] puntosPatrulla;
    [SerializeField] private float tiempoEspera = 2f;

    [Header("Puertas")]
    [SerializeField] private float rangoDeteccionPuerta = 3f;

    [Header("Campo de Visión")]
    [SerializeField] private Transform jugador;
    [SerializeField] private float rangoVision = 10f;
    [SerializeField] private float anguloVision = 90f;
    [SerializeField] private LayerMask capasObstaculo;
    [SerializeField] private float velocidadPersecucion = 5f;
    [SerializeField] private float velocidadPatrulla = 2f;

    private NavMeshAgent navAgent;
    private int indiceActual = 0;
    private float tiempoEnPunto = 0f;
    private bool esperando = false;

    private enum Estado { Patrullando, Persiguiendo, Buscando }
    private Estado estadoActual = Estado.Patrullando;
    private Vector3 ultimaPosicionVista;

    [Header("Búsqueda")]
    [SerializeField] private float tiempoExploracion = 3f;
    [SerializeField] private float radioExploracion  = 4f;

    private float timerExploracion = 0f;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = velocidadPatrulla;
        if (puntosPatrulla.Length > 0)
            navAgent.SetDestination(puntosPatrulla[0].position);
    }

    void Update()
    {
        bool jugadorVisible = VerificarVision();

        switch (estadoActual)
        {
            case Estado.Patrullando:
                if (jugadorVisible) EntrarEnPersecucion();
                else                Patrullar();
                break;

            case Estado.Persiguiendo:
                if (jugadorVisible)
                {
                    ultimaPosicionVista = jugador.position;
                    navAgent.SetDestination(jugador.position);
                }
                else EntrarEnBusqueda();
                break;

            case Estado.Buscando:
                if (jugadorVisible) { EntrarEnPersecucion(); break; }

                timerExploracion -= Time.deltaTime;
                Debug.Log($"[Buscando] Timer: {timerExploracion:F1} | destino actual: {navAgent.destination} | distancia restante: {navAgent.remainingDistance:F1}");

                if (timerExploracion <= 0f)
                {
                    Debug.Log("[Buscando] Timer agotado, volviendo a patrullar.");
                    VolverAPatrullar();
                    break;
                }

                // Cuando termina de ir a un punto, elegir otro cercano aleatorio
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    Vector3 nuevoPunto = PuntoAleatorioEnZona(ultimaPosicionVista, radioExploracion);
                    Debug.Log($"[Buscando] Llegué al punto, yendo a nuevo punto: {nuevoPunto}");
                    navAgent.SetDestination(nuevoPunto);
                }

                break;
        }

        GestionarPuertas();
    }

    private Vector3 PuntoAleatorioEnZona(Vector3 centro, float radio)
    {
        Vector2 circulo = Random.insideUnitCircle * radio;
        Vector3 candidato = centro + new Vector3(circulo.x, 0f, circulo.y);
        if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, radio, NavMesh.AllAreas))
            return hit.position;
        return centro;
    }

    private bool VerificarVision()
    {
        if (jugador == null) return false;

        Vector3 dirAlJugador = jugador.position - transform.position;
        float distancia = dirAlJugador.magnitude;

        if (distancia > rangoVision) return false;
        if (Vector3.Angle(transform.forward, dirAlJugador) > anguloVision * 0.5f) return false;
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, dirAlJugador.normalized, distancia, capasObstaculo)) return false;

        return true;
    }

    private void EntrarEnPersecucion()
    {
        estadoActual = Estado.Persiguiendo;
        navAgent.speed = velocidadPersecucion;
        ultimaPosicionVista = jugador.position;
        navAgent.SetDestination(jugador.position);
    }

    private void EntrarEnBusqueda()
    {
        estadoActual = Estado.Buscando;
        timerExploracion = tiempoExploracion;
        navAgent.SetDestination(ultimaPosicionVista);
        Debug.Log($"[Buscando] Entrando en búsqueda. Última posición vista: {ultimaPosicionVista}");
    }

    private void VolverAPatrullar()
    {
        estadoActual = Estado.Patrullando;
        navAgent.speed = velocidadPatrulla;
        esperando = false;
        if (puntosPatrulla.Length > 0)
            navAgent.SetDestination(puntosPatrulla[indiceActual].position);
    }

    private void Patrullar()
    {
        if (puntosPatrulla.Length == 0) return;

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
                    indiceActual = (indiceActual + 1) % puntosPatrulla.Length;
                    navAgent.SetDestination(puntosPatrulla[indiceActual].position);
                    esperando = false;
                }
            }
        }
    }

    private void GestionarPuertas()
    {
        Collider[] cercanos = Physics.OverlapSphere(transform.position, rangoDeteccionPuerta);
        foreach (Collider col in cercanos)
        {
            PuertaNavMesh puerta = col.GetComponent<PuertaNavMesh>();
            if (puerta != null && !puerta.EstaAbierta())
                puerta.Abrir();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float semi = anguloVision * 0.5f;
        Vector3 o = transform.position + Vector3.up * 1.5f;
        Gizmos.DrawRay(o, Quaternion.Euler(0,  semi, 0) * transform.forward * rangoVision);
        Gizmos.DrawRay(o, Quaternion.Euler(0, -semi, 0) * transform.forward * rangoVision);
        Gizmos.DrawRay(o, transform.forward * rangoVision);
    }
}