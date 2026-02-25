using UnityEngine;

public class InventarioMago : MonoBehaviour
{
    [Header("Inventario")]
    [SerializeField] private int pocionesRecogidas = 0;
    [SerializeField] private int totalPociones = 6; // Total de pociones en el mapa
    [SerializeField] private bool tieneLibro = false;
    
    
    void Start()
    {
    }
    
    public void RecogerPocion()
    {
        pocionesRecogidas++;
        Debug.Log($"Pociones: {pocionesRecogidas}/{totalPociones}");
        
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
    
}