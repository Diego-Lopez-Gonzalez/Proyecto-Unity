using UnityEngine;
using UnityEngine.AI;

public class GuardiaPatrulla : MonoBehaviour
{
    private enum Estado
    {
        Patrullando,
        Persiguiendo,
        Buscando,
        YendoAPalanca
    }

    private Estado estadoActual = Estado.Patrullando;

    private CapaReactiva capaReactiva;
    private CapaTactica  capaTactica;
    private NavMeshAgent navAgent;

    [Header("Captura")]
    [SerializeField] private float radioCaptura = 1.2f;

    [Header("Objeto Vigilado")]
    [SerializeField] private Transform objetoVigilado;

    [Header("Secuencia al detectar desaparición")]
    [SerializeField] private Transform palanca;
    [SerializeField] private Transform[] nuevaRutaTrasActivarPalanca;

    private bool secuenciaPalancaPendiente = false;

    void Start()
    {
        navAgent     = GetComponent<NavMeshAgent>();
        capaReactiva = GetComponent<CapaReactiva>();
        capaTactica  = GetComponent<CapaTactica>();

        capaReactiva.Inicializar(navAgent);
        capaTactica.Inicializar(capaReactiva);

        capaTactica.IniciarPatrulla();
    }

    void Update()
    {
        // ── PERCEPCIÓN ───────────────────────────────────────────
        bool jugadorDetectado = capaReactiva.DetectarJugador(out Vector3 posicionJugador);
        capaReactiva.GestionarPuertasCercanas();

        // ── PRIORIDAD 1: Perseguir al jugador ────────────────────
        if (jugadorDetectado)
        {
            if (estadoActual == Estado.YendoAPalanca)
                secuenciaPalancaPendiente = true;

            if (estadoActual != Estado.Persiguiendo)
                CambiarAPersecucion(posicionJugador);
            else
                capaTactica.EjecutarPersecucion(posicionJugador);

            // ── CAPTURA ───────────────────────────────────────────
            if (capaReactiva.DetectarCaptura(radioCaptura))
            {
                Debug.Log("[Planificación] ¡JUGADOR ATRAPADO! → GAME OVER");
                GameOverManager.Instancia.ActivarGameOver();
                return;
            }

            return;
        }

        // ── PRIORIDAD 2: Objeto vigilado desaparecido ────────────
        // Se comprueba en todos los estados salvo YendoAPalanca (ya en marcha)
        if (estadoActual != Estado.YendoAPalanca)
        {
            if (capaReactiva.DetectarDesaparicionObjeto(objetoVigilado, out _))
            {
                secuenciaPalancaPendiente = false;
                CambiarAYendoAPalanca();
                return;
            }
        }

        // ── MÁQUINA DE ESTADOS ───────────────────────────────────
        switch (estadoActual)
        {
            case Estado.Patrullando:
                capaTactica.EjecutarPatrulla();
                break;

            case Estado.Persiguiendo:
                // Jugador perdido
                if (secuenciaPalancaPendiente)
                {
                    secuenciaPalancaPendiente = false;
                    CambiarAYendoAPalanca();
                }
                else
                {
                    CambiarABusqueda(posicionJugador);
                }
                break;

            case Estado.Buscando:
                if (!capaTactica.EjecutarBusqueda())
                    CambiarAPatrulla();
                break;

            case Estado.YendoAPalanca:
                if (!capaTactica.EjecutarSecuenciaPalanca())
                    CambiarAPatrullaTrasPalanca();
                break;
        }
    }

    private void CambiarAPersecucion(Vector3 posicion)
    {
        estadoActual = Estado.Persiguiendo;
        capaTactica.EjecutarPersecucion(posicion);
        Debug.Log("[Planificación] → PERSIGUIENDO");
    }

    private void CambiarABusqueda(Vector3 ultimaPosicion)
    {
        estadoActual = Estado.Buscando;
        capaTactica.IniciarBusqueda(ultimaPosicion);
        Debug.Log("[Planificación] → BUSCANDO");
    }

    private void CambiarAPatrulla()
    {
        estadoActual = Estado.Patrullando;
        capaTactica.IniciarPatrulla();
        Debug.Log("[Planificación] → PATRULLANDO");
    }

    private void CambiarAYendoAPalanca()
    {
        estadoActual = Estado.YendoAPalanca;
        capaTactica.IniciarSecuenciaPalanca(palanca, nuevaRutaTrasActivarPalanca);
        Debug.Log("[Planificación] → YENDO A PALANCA");
    }

    private void CambiarAPatrullaTrasPalanca()
    {
        estadoActual = Estado.Patrullando;
        Debug.Log("[Planificación] → PATRULLANDO (ruta nueva tras palanca)");
    }
}