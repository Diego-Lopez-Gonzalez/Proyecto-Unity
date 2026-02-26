using UnityEngine;
using GuardiaIA;

public class MagoControllerSimple : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadCaminar = 3f;
    [SerializeField] private float velocidadCorrer  = 6f;
    [SerializeField] private float suavizadoRotacion = 10f;

    [Header("Sonido")]
    [SerializeField] private float intensidadCaminar = 4f;
    [SerializeField] private float intensidadCorrer  = 9f;

    private CharacterController characterController;
    private FuenteSonido         fuenteSonido;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        fuenteSonido        = GetComponent<FuenteSonido>();
    }

    void Update()
    {
        // Leer input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical   = Input.GetAxisRaw("Vertical");
        Vector3 direccion = new Vector3(horizontal, 0f, vertical).normalized;

        // Velocidad e intensidad de sonido según si corre o camina
        bool  corriendo  = Input.GetKey(KeyCode.LeftShift);
        float velocidad  = corriendo ? velocidadCorrer  : velocidadCaminar;

        // Mover
        if (direccion.magnitude >= 0.1f)
        {
            Vector3 movimiento = direccion * velocidad * Time.deltaTime;
            characterController.Move(movimiento);

            // Rotar
            Quaternion rotacion = Quaternion.LookRotation(direccion);
            transform.rotation  = Quaternion.Lerp(transform.rotation, rotacion,
                                                   suavizadoRotacion * Time.deltaTime);

            // Emitir sonido con la intensidad correspondiente al tipo de movimiento
            if (fuenteSonido != null)
            {
                fuenteSonido.SetIntensidad(corriendo ? intensidadCorrer : intensidadCaminar);
                fuenteSonido.Emitir();
            }
        }

        transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
    }
}