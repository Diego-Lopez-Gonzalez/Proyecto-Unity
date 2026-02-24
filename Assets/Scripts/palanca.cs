using UnityEngine;
using UnityEngine.Events;

public class Palanca : MonoBehaviour
{
    [SerializeField] private float rangoInteraccion = 2f;
    [SerializeField] private Transform jugador;

    [SerializeField] private UnityEvent onActivada;
    [SerializeField] private UnityEvent onDesactivada;

    private bool activada = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && jugador != null)
        {
            if (Vector3.Distance(jugador.position, transform.position) <= rangoInteraccion)
                Alternar();
        }
    }

    public bool EstaActivada() => activada;

    // Llamado por el agente
    public void Activar()
    {
        if (activada) return;
        activada = true;
        Debug.Log("[Palanca] Activada.");
        onActivada?.Invoke();
    }

    // Llamado por el jugador al pulsar E
    private void Alternar()
    {
        activada = !activada;
        Debug.Log($"[Palanca] {(activada ? "Activada" : "Desactivada")} por el jugador.");

        if (activada) onActivada?.Invoke();
        else          onDesactivada?.Invoke();
    }
}