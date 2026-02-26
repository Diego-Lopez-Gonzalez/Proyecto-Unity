using UnityEngine;

public class InventarioMago : MonoBehaviour
{
    [Header("Inventario")]
    [SerializeField] private int objetosRecogidos = 0;
    [SerializeField] private int totalObjetos = 5;
    
    
    void Start()
    {
    }
    
    public void RecogerObjeto()
    {
        objetosRecogidos++;
        Debug.Log($"Pociones: {objetosRecogidos}/{totalObjetos}");
        
        // Comprobar si ha recogido todas
        if (objetosRecogidos >= totalObjetos)
        {
            Debug.Log("¡Has recogido todas las pociones!");
        }
    }

    
    public int GetObjetosRecogidos()
    {
        return objetosRecogidos;
    }
    
    public bool TieneTodosLosObjetos()
    {
        return objetosRecogidos >= totalObjetos;
    }
    
}