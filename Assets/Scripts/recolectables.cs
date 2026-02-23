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
        // Buscar al jugador
        jugador = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (jugador == null)
        {
            Debug.LogWarning("No se encontró el jugador. Asegúrate de que tenga el tag 'Player'");
        }
        
    }
    
    void Update()
    {
        // Rotación decorativa (opcional)
        if (rotarObjeto)
        {
            transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);
        }
        
        // Comprobar distancia al jugador
        if (jugador != null)
        {
            float distancia = Vector3.Distance(transform.position, jugador.position);
            jugadorCerca = distancia <= rangoDeteccion;
            
            
            // Recoger con E
            if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
            {
                Recoger();
            }
        }
    }
    
    private void Recoger()
    {
        // Notificar al sistema de inventario
        InventarioMago inventario = jugador.GetComponent<InventarioMago>();
        
        if (inventario != null)
        {
            if (tipo == TipoObjeto.Pocion)
            {
                inventario.RecogerPocion();
            }
            else if (tipo == TipoObjeto.Libro)
            {
                inventario.RecogerLibro();
            }
            
            Debug.Log($"¡Has recogido: {nombreObjeto}!");
            
            // Destruir el objeto
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("El jugador no tiene el componente InventarioMago");
        }
    }
    
    // Visualizar el rango en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}