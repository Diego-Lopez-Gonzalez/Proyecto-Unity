using UnityEngine;

public class MagoControllerSimple : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadCaminar = 3f;
    [SerializeField] private float velocidadCorrer = 6f;
    [SerializeField] private float suavizadoRotacion = 10f;
    
    private CharacterController characterController;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }
    
    void Update()
    {
        // Leer input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direccion = new Vector3(horizontal, 0f, vertical).normalized;
        
        // Velocidad según si corre o camina
        bool corriendo = Input.GetKey(KeyCode.LeftShift);
        float velocidad = corriendo ? velocidadCorrer : velocidadCaminar;
        
        // Mover
        if (direccion.magnitude >= 0.1f)
        {
            Vector3 movimiento = direccion * velocidad * Time.deltaTime;
            characterController.Move(movimiento);
            
            // Rotar
            Quaternion rotacion = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotacion, suavizadoRotacion * Time.deltaTime);
        }
    }
}