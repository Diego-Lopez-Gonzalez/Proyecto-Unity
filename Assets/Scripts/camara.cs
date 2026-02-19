using UnityEngine;

public class CamaraSeguir : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private Transform objetivo; // El mago
    [SerializeField] private Vector3 offset = new Vector3(0, 8, -10); // Posición relativa
    [SerializeField] private float suavizado = 5f; // Suavidad del seguimiento (0 = instantáneo)
    
    void LateUpdate()
    {
        if (objetivo == null)
        {
            Debug.LogWarning("¡Asigna el mago en el Inspector!");
            return;
        }
        
        // Calcular posición deseada (posición del mago + offset)
        Vector3 posicionDeseada = objetivo.position + offset;
        
        // Mover la cámara suavemente
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);
        
        // La rotación se mantiene FIJA (no cambia)
    }
}
