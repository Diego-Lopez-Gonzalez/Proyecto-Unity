using UnityEngine;
using UnityEngine.AI;

public class CapaTactica : MonoBehaviour
{
    [Header("Patrulla")]
    [SerializeField] private Transform[] puntosPatrulla;
    [SerializeField] private float tiempoEspera = 2f;
    [SerializeField] private float velocidadPatrulla = 2f;

    [Header("Persecución")]
    [SerializeField] private float velocidadPersecucion = 5f;

    [Header("Búsqueda")]
    [SerializeField] private float tiempoExploracion = 3f;
    [SerializeField] private float radioExploracion = 4f;


    private CapaReactiva capaReactiva;

    // Estado interno de la capa táctica
    private int indiceActualPatrulla = 0;
    private float tiempoEnPunto = 0f;
    private bool esperandoEnPunto = false;

    private float timerExploracion = 0f;
    private Vector3 ultimaPosicionConocida;

    // Estado interno de la secuencia palanca → nueva ruta
    private Transform palancaObjetivo;
    private Transform[] rutaTrasPalanca;

    public void Inicializar(CapaReactiva reactiva)
    {
        capaReactiva = reactiva;
    }

    // ─────────────────────────────────────────────────────────────
    //  PATRULLA
    // ─────────────────────────────────────────────────────────────

    public void EjecutarPatrulla()
    {
        if (puntosPatrulla.Length == 0) return;

        if (capaReactiva.HaLlegadoADestino())
        {
            if (!esperandoEnPunto)
            {
                esperandoEnPunto = true;
                tiempoEnPunto = 0f;
            }
            else
            {
                tiempoEnPunto += Time.deltaTime;
                if (tiempoEnPunto >= tiempoEspera)
                {
                    indiceActualPatrulla = (indiceActualPatrulla + 1) % puntosPatrulla.Length;
                    capaReactiva.MoverHacia(puntosPatrulla[indiceActualPatrulla].position, velocidadPatrulla);
                    esperandoEnPunto = false;
                }
            }
        }
    }

    public void IniciarPatrulla()
    {
        esperandoEnPunto = false;
        if (puntosPatrulla.Length > 0)
            capaReactiva.MoverHacia(puntosPatrulla[indiceActualPatrulla].position, velocidadPatrulla);
    }

    /// <summary>
    /// Sustituye la ruta de patrulla actual y reinicia el recorrido desde el primer punto.
    /// </summary>
    /// <param name="nuevaRuta">Nuevos puntos de patrulla.</param>
    public void CambiarRutaPatrulla(Transform[] nuevaRuta)
    {
        if (nuevaRuta == null || nuevaRuta.Length == 0)
        {
            Debug.LogWarning("[Táctica] CambiarRutaPatrulla: la nueva ruta está vacía o es nula.");
            return;
        }

        puntosPatrulla = nuevaRuta;
        indiceActualPatrulla = 0;
        esperandoEnPunto = false;
        tiempoEnPunto = 0f;

        capaReactiva.MoverHacia(puntosPatrulla[0].position, velocidadPatrulla);
        Debug.Log($"[Táctica] Ruta cambiada. Yendo al punto 0: {puntosPatrulla[0].position}");
    }

    // ─────────────────────────────────────────────────────────────
    //  PERSECUCIÓN
    // ─────────────────────────────────────────────────────────────

    public void EjecutarPersecucion(Vector3 posicionObjetivo)
    {
        ultimaPosicionConocida = posicionObjetivo;
        capaReactiva.MoverHacia(posicionObjetivo, velocidadPersecucion);
    }

    // ─────────────────────────────────────────────────────────────
    //  BÚSQUEDA EN ÁREA
    // ─────────────────────────────────────────────────────────────

    public void IniciarBusqueda(Vector3 posicionInicial)
    {
        ultimaPosicionConocida = posicionInicial;
        timerExploracion = tiempoExploracion;
        capaReactiva.MoverHacia(ultimaPosicionConocida, velocidadPatrulla);
        Debug.Log($"[Táctica-Búsqueda] Iniciando búsqueda en: {ultimaPosicionConocida}");
    }

    public bool EjecutarBusqueda()
    {
        timerExploracion -= Time.deltaTime;
        Debug.Log($"[Táctica-Búsqueda] Timer: {timerExploracion:F1}");

        if (timerExploracion <= 0f)
        {
            Debug.Log("[Táctica-Búsqueda] Tiempo agotado.");
            return false;
        }

        if (capaReactiva.HaLlegadoADestino())
        {
            Vector3 nuevoPunto = GenerarPuntoAleatorio(ultimaPosicionConocida, radioExploracion);
            Debug.Log($"[Táctica-Búsqueda] Explorando nuevo punto: {nuevoPunto}");
            capaReactiva.MoverHacia(nuevoPunto, velocidadPatrulla);
        }

        return true;
    }

    // ─────────────────────────────────────────────────────────────
    //  SECUENCIA PALANCA → CAMBIO DE RUTA
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Arranca la secuencia: el agente se dirige a la palanca, la activa si aún no
    /// lo está, y después cambia a la nueva ruta de patrulla proporcionada.
    /// Debe llamarse una sola vez; ejecutar EjecutarSecuenciaPalanca() cada frame.
    /// </summary>
    /// <param name="palanca">Transform de la palanca a activar.</param>
    /// <param name="nuevaRuta">Ruta de patrulla que se aplicará tras activarla.</param>
    public void IniciarSecuenciaPalanca(Transform palanca, Transform[] nuevaRuta)
    {
        if (palanca == null)
        {
            Debug.LogWarning("[Táctica-Palanca] Palanca nula, secuencia cancelada.");
            return;
        }

        palancaObjetivo = palanca;
        rutaTrasPalanca = nuevaRuta;

        capaReactiva.MoverHacia(palancaObjetivo.position, velocidadPatrulla);
        Debug.Log($"[Táctica-Palanca] Yendo a la palanca en {palancaObjetivo.position}");
    }

    /// <summary>
    /// Ejecuta frame a frame la secuencia palanca → cambio de ruta.
    /// Devuelve true mientras la secuencia sigue activa, false cuando ha terminado
    /// (palanca activada y nueva ruta aplicada, o si la palanca ya estaba activada).
    /// </summary>
    public bool EjecutarSecuenciaPalanca()
    {
        if (palancaObjetivo == null) return false;

        if (!capaReactiva.HaLlegadoADestino()) return true;

        // Ha llegado junto a la palanca
        Palanca palanca = palancaObjetivo.GetComponent<Palanca>();
        if (palanca == null)
        {
            Debug.LogWarning("[Táctica-Palanca] El objeto no tiene componente Palanca.");
            FinalizarSecuenciaPalanca();
            return false;
        }

        if (!palanca.EstaActivada())
        {
            palanca.Activar();
            Debug.Log("[Táctica-Palanca] Palanca activada.");
        }
        else
        {
            Debug.Log("[Táctica-Palanca] La palanca ya estaba activada.");
        }

        FinalizarSecuenciaPalanca();
        return false;
    }

    private void FinalizarSecuenciaPalanca()
    {
        palancaObjetivo = null;

        if (rutaTrasPalanca != null && rutaTrasPalanca.Length > 0)
        {
            Debug.Log("[Táctica-Palanca] Secuencia completada. Cambiando ruta de patrulla.");
            CambiarRutaPatrulla(rutaTrasPalanca);
        }
        else
        {
            Debug.Log("[Táctica-Palanca] Secuencia completada. Sin nueva ruta, volviendo a patrullar.");
            IniciarPatrulla();
        }

        rutaTrasPalanca = null;
    }

    // ─────────────────────────────────────────────────────────────
    //  UTILIDADES
    // ─────────────────────────────────────────────────────────────

    private Vector3 GenerarPuntoAleatorio(Vector3 centro, float radio)
    {
        Vector2 circulo = Random.insideUnitCircle * radio;
        Vector3 candidato = centro + new Vector3(circulo.x, 0f, circulo.y);
        if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, radio, NavMesh.AllAreas))
            return hit.position;
        return centro;
    }
}