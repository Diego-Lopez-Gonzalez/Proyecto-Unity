using UnityEngine;

namespace GuardiaIA
{
    [RequireComponent(typeof(SensorVision))]
    [RequireComponent(typeof(GestorComunicacion))]
    [RequireComponent(typeof(BuzonMensajes))]
    public class CerebroCamara : MonoBehaviour, IAgente
    {
        [Header("Visión")]
        [SerializeField] private Transform jugador;
        [SerializeField] private float     rangoVision      = 15f;
        [SerializeField] private float     anguloVision     = 60f;
        [SerializeField] private LayerMask capasObstaculo;

        [Header("Rotación")]
        [SerializeField] private bool  rotarHaciaJugador = true;
        [SerializeField] private float velocidadRotacion  = 3f;

        private SensorVision       sensorVision;
        private GestorComunicacion gestorComunicacion;

        public GestorComunicacion GestorComunicacion => gestorComunicacion;

        // La cámara siempre está "ocupada": nunca acepta tareas de contrato.
        public bool EstaOcupado => true;
        public PrioridadContrato PrioridadTareaActual => PrioridadContrato.Baja;
        public void InterrumpirTareaActual() { } // no hace nada: nunca tiene tarea

        private bool    jugadorEnCono      = false;
        private Vector3 posicionJugadorActual;

        private void Start()
        {
            sensorVision       = GetComponent<SensorVision>();
            gestorComunicacion = GetComponent<GestorComunicacion>();

            sensorVision.Inicializar(
                this, jugador, objetoVigilado: null,
                rangoVision, anguloVision, capasObstaculo);
        }

        private void Update()
        {
            if (rotarHaciaJugador && jugadorEnCono)
                RotarHaciaJugador();
        }

        public void OnJugadorDetectado(Vector3 posicion)
        {
            jugadorEnCono         = true;
            posicionJugadorActual = posicion;
            Debug.Log($"[CerebroCamara] {name} detectó al jugador en {posicion}");

            // El Planificador decide qué tareas delegar según evento y prioridad.
            const PrioridadContrato prioridad = PrioridadContrato.Alta;
            var tareas = Planificador.PlanParaEvento(EventoSeguridad.JugadorDetectado, prioridad);
            gestorComunicacion?.IniciarContractNet(posicion, tareas, prioridad);
        }

        public void OnActualizarPosicionJugador(Vector3 posicion)
        {
            posicionJugadorActual = posicion;
        }

        public void OnJugadorPerdido()
        {
            jugadorEnCono = false;
            Debug.Log($"[CerebroCamara] {name} perdió al jugador.");
        }

        public void OnObjetoDesaparecido() { }

        public void OnTareaAsignada(TareaContrato tarea, string conversationId)
        {
            Debug.LogWarning($"[CerebroCamara] {name} recibió OnTareaAsignada inesperadamente.");
        }

        public void OnTareaCancelada(string conversationId) { }

        private void RotarHaciaJugador()
        {
            Vector3 direccion = (posicionJugadorActual - transform.position).normalized;
            direccion.y = 0f;
            if (direccion == Vector3.zero) return;

            Quaternion objetivo = Quaternion.LookRotation(direccion);
            transform.rotation  = Quaternion.Slerp(
                transform.rotation, objetivo, velocidadRotacion * Time.deltaTime);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            float semi   = anguloVision * 0.5f;
            Vector3 o    = transform.position + Vector3.up * 1.5f;
            Gizmos.DrawRay(o, Quaternion.Euler(0,  semi, 0) * transform.forward * rangoVision);
            Gizmos.DrawRay(o, Quaternion.Euler(0, -semi, 0) * transform.forward * rangoVision);
            Gizmos.DrawRay(o, transform.forward * rangoVision);
        }
    }
}