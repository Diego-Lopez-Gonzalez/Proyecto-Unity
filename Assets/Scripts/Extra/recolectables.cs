using UnityEngine;

public class ObjetoRecolectable : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private TipoObjeto tipo;
    [SerializeField] private string nombreObjeto = "RECOLECTABLE";
    [SerializeField] private float rangoDeteccion = 2f;
    
    [Header("Visual (Opcional)")]
    [SerializeField] private bool rotarObjeto = true;
    [SerializeField] private float velocidadRotacion = 50f;
    
    private Transform jugador;
    private bool jugadorCerca = false;
    
    public enum TipoObjeto
    {
        Pocion,
        Libro
    }
    
    void Start()
    {
        jugador = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (jugador == null)
            Debug.LogWarning("No se encontró el jugador. Asegúrate de que tenga el tag 'Player'");
    }
    
    void Update()
    {
        if (rotarObjeto)
            transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);
        
        if (jugador != null)
        {
            float distancia = Vector3.Distance(transform.position, jugador.position);
            jugadorCerca = distancia <= rangoDeteccion;
            
            if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
                Recoger();
        }
    }
    
    private void Recoger()
    {
        InventarioMago inventario = jugador.GetComponent<InventarioMago>();
        
        if (inventario != null)
        {
            inventario.RecogerObjeto();
            
            
            Debug.Log($"¡Has recogido: {nombreObjeto}!");
            
            // SetActive en lugar de Destroy para que el guardia pueda detectar su ausencia
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("El jugador no tiene el componente InventarioMago");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}