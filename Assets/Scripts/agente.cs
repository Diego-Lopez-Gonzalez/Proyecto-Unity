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
    [SerializeField] private float anguloVision = 90f;        // Semiángulo total del cono
    [SerializeField] private float tiempoMemoria = 3f;        // Segundos que "recuerda" al jugador tras perderlo
    [SerializeField] private LayerMask capasObstaculo;        // Capas que bloquean la visión (paredes, etc.)
    [SerializeField] private float velocidadPersecucion = 5f;
    [SerializeField] private float velocidadPatrulla = 2f;

    private NavMeshAgent navAgent;
    private int indiceActual = 0;
    private float tiempoEnPunto = 0f;
    private bool esperando = false;

    // Estado
    private enum Estado { Patrullando, Persiguiendo, Buscando }
    private Estado estadoActual = Estado.Patrullando;
    private float timerMemoria = 0f;
    private Vector3 ultimaPosicionVista;

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
                if (jugadorVisible)
                    EntrarEnPersecucion();
                else
                    Patrullar();
                break;

            case Estado.Persiguiendo:
                if (jugadorVisible)
                {
                    ultimaPosicionVista = jugador.position;
                    navAgent.SetDestination(jugador.position);
                    timerMemoria = tiempoMemoria;
                }
                else
                {
                    timerMemoria -= Time.deltaTime;
                    if (timerMemoria <= 0f)
                        EntrarEnBusqueda();
                }
                break;

            case Estado.Buscando:
                // Camina hacia la última posición conocida
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                    VolverAPatrullar();

                // Si lo vuelve a ver mientras busca
                if (jugadorVisible)
                    EntrarEnPersecucion();
                break;
        }

        GestionarPuertas();
    }


    private bool VerificarVision()
    {
        if (jugador == null) return false;

        Vector3 dirAlJugador = jugador.position - transform.position;
        float distancia = dirAlJugador.magnitude;

        // 1. ¿Está dentro del rango?
        if (distancia > rangoVision) return false;

        // 2. ¿Está dentro del ángulo del cono?
        float angulo = Vector3.Angle(transform.forward, dirAlJugador);
        if (angulo > anguloVision * 0.5f) return false;

        // 3. ¿Hay línea de visión directa? (Raycast)
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f,
                            dirAlJugador.normalized,
                            distancia,
                            capasObstaculo))
            return false; // Algo lo bloquea

        return true;
    }


    private void EntrarEnPersecucion()
    {
        estadoActual = Estado.Persiguiendo;
        navAgent.speed = velocidadPersecucion;
        timerMemoria = tiempoMemoria;
        Debug.Log("¡Guardia detectó al jugador!");
    }

    private void EntrarEnBusqueda()
    {
        estadoActual = Estado.Buscando;
        navAgent.SetDestination(ultimaPosicionVista);
        Debug.Log("Guardia busca en la última posición vista.");
    }

    private void VolverAPatrullar()
    {
        estadoActual = Estado.Patrullando;
        navAgent.speed = velocidadPatrulla;
        esperando = false;

        if (puntosPatrulla.Length > 0)
            navAgent.SetDestination(puntosPatrulla[indiceActual].position);

        Debug.Log("Guardia vuelve a patrullar.");
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

    
}