using UnityEngine;

public class InventarioMago : MonoBehaviour
{
    [Header("Inventario")]
    [SerializeField] private int pocionesRecogidas = 0;
    [SerializeField] private int totalPociones = 6; // Total de pociones en el mapa
    [SerializeField] private bool tieneLibro = false;
    
    [Header("UI (Opcional)")]
    [SerializeField] private UnityEngine.UI.Text textoPociones;
    [SerializeField] private UnityEngine.UI.Text textoLibro;
    
    void Start()
    {
        ActualizarUI();
    }
    
    public void RecogerPocion()
    {
        pocionesRecogidas++;
        Debug.Log($"Pociones: {pocionesRecogidas}/{totalPociones}");
        ActualizarUI();
        
        // Comprobar si ha recogido todas
        if (pocionesRecogidas >= totalPociones)
        {
            Debug.Log("¡Has recogido todas las pociones!");
        }
    }
    
    public void RecogerLibro()
    {
        tieneLibro = true;
        Debug.Log("¡Has recogido el Libro de Recetas!");
        ActualizarUI();
    }
    
    public bool TieneLibro()
    {
        return tieneLibro;
    }
    
    public int GetPocionesRecogidas()
    {
        return pocionesRecogidas;
    }
    
    public bool TieneTodosLosObjetos()
    {
        return tieneLibro && pocionesRecogidas >= totalPociones;
    }
    
    private void ActualizarUI()
    {
        if (textoPociones != null)
        {
            textoPociones.text = $"Pociones: {pocionesRecogidas}/{totalPociones}";
        }
        
        if (textoLibro != null)
        {
            textoLibro.text = tieneLibro ? "Libro: ✓" : "Libro: ✗";
        }
    }
}