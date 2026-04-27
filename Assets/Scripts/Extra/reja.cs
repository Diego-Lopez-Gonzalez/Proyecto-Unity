using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class ParedBloqueadora : MonoBehaviour
{
    [Tooltip("Cuánto sube en Y respecto a su posición inicial.")]
    [SerializeField] private float desplazamientoArriba = 4f;

    private NavMeshObstacle obstaculo;
    private Vector3 posAbajo;
    private Vector3 posArriba;

    private void Awake()
    {
        obstaculo = GetComponent<NavMeshObstacle>();
        obstaculo.enabled = false;
        obstaculo.carving = false;

        // Guardamos la posición actual como "abajo" y calculamos "arriba"
        posArriba  = transform.position;
        posAbajo = posArriba - new Vector3(0f, desplazamientoArriba, 0f);

        // Empezamos arriba (retraída)
        transform.position = posArriba;
    }

    public void Bajar()
    {
        transform.position    = posAbajo;
        obstaculo.enabled     = true;
        obstaculo.carving     = true;
        Debug.Log("[ParedBloqueadora] Bajada.");
    }

    public void Subir()
    {
        transform.position    = posArriba;
        obstaculo.enabled     = false;
        obstaculo.carving     = false;
        Debug.Log("[ParedBloqueadora] Subida.");
    }
}