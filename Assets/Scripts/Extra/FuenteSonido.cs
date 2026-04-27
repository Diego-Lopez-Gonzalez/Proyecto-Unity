using UnityEngine;

namespace GuardiaIA
{
    /// Componente que se coloca en cualquier objeto que pueda emitir ruido
    /// detectable por el guardia
    
    public class FuenteSonido : MonoBehaviour
    {
        [Header("Parámetros del sonido")]
        [Tooltip("Radio en el que este sonido puede ser escuchado por un guardia.")]
        [SerializeField] private float intensidad = 8f;

        // True durante el frame en que se emitió el sonido.
        public bool EstaActiva  { get; set; } = false;

        // Radio de emisión del sonido.
        public float Intensidad => intensidad;

        public void SetIntensidad(float nuevaIntensidad)
        {
            intensidad = nuevaIntensidad;
        }
        public void Emitir()
        {
            EstaActiva = true;
        }

        //  GIZMOS
        private void OnDrawGizmos()
        {
            Gizmos.color = EstaActiva ? Color.red : new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, intensidad);
        }
    }
}