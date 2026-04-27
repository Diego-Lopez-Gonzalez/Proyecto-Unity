using UnityEngine;

namespace GuardiaIA
{
    /// Agente cámara de vigilancia: estático, sin movimiento ni estados.
    ///
    /// Responsabilidades:
    ///   · Detectar al jugador mediante SensorVision (mismo componente que el guardia).
    ///   · Al detectarlo, lanzar el Contract Net para alertar a los guardias vecinos.
    ///   · Rotar físicamente hacia el jugador mientras lo tiene en cono (opcional visual).
    ///   · Nunca actúa como contratista: no puede moverse ni ejecutar tareas físicas.
    ///
    /// Componentes requeridos en el mismo GameObject:
    ///   · SensorVision
    ///   · GestorComunicacion
    ///   · BuzonMensajes
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
        [SerializeField] private bool  rotarHaciaJugador  = true;
        [SerializeField] private float velocidadRotacion   = 3f;

        // Componentes internos
        private SensorVision       sensorVision;
        private GestorComunicacion gestorComunicacion;

        // Acceso público para GestorComunicacion (igual que en Cerebro).
        public GestorComunicacion GestorComunicacion => gestorComunicacion;

        // La cámara nunca persigue: siempre puede actuar como... espera, en realidad
        // la cámara NUNCA debe ser contratista porque no puede moverse.
        // Devolvemos true para que GestorComunicacion la rechace como contratista
        // en ProcesarCfp, igual que rechaza a un guardia en persecución.
        public bool EstaEnPersecucion => true;

        // Estado interno
        private bool   jugadorEnCono       = false;
        private Vector3 posicionJugadorActual;

        private void Start()
        {
            sensorVision       = GetComponent<SensorVision>();
            gestorComunicacion = GetComponent<GestorComunicacion>();

            // La cámara no vigila objeto: pasamos null como objetoVigilado.
            sensorVision.Inicializar(
                this,
                jugador,
                objetoVigilado: null,
                rangoVision,
                anguloVision,
                capasObstaculo
            );
        }

        private void Update()
        {
            if (rotarHaciaJugador && jugadorEnCono)
                RotarHaciaJugador();
        }

        // ── Implementación de IAgente — callbacks de visión ──────────────────

        public void OnJugadorDetectado(Vector3 posicion)
        {
            jugadorEnCono          = true;
            posicionJugadorActual  = posicion;

            Debug.Log($"[CerebroCamara] {name} detectó al jugador en {posicion}");

            // Lanzamos el Contract Net para que los guardias vecinos reaccionen.
            gestorComunicacion?.IniciarContractNet(posicion);
        }

        public void OnActualizarPosicionJugador(Vector3 posicion)
        {
            // Actualizamos la posición para que la rotación sea fluida.
            posicionJugadorActual = posicion;
        }

        public void OnJugadorPerdido()
        {
            jugadorEnCono = false;
            Debug.Log($"[CerebroCamara] {name} perdió al jugador.");
        }

        public void OnObjetoDesaparecido()
        {
            // La cámara no gestiona objetos vigilados: no hace nada.
            // El método existe para cumplir la interfaz IAgente.
        }

        // ── Implementación de IAgente — callbacks de contrato ────────────────

        public void OnAsignadoCerrarZona(Vector3 posicionLadron, string conversationId)
        {
            // La cámara no puede moverse: nunca debería recibir esta tarea
            // porque EstaEnPersecucion = true la excluye de las propuestas.
            // Lo registramos como advertencia por si acaso.
            Debug.LogWarning($"[CerebroCamara] {name} recibió OnAsignadoCerrarZona inesperadamente.");
        }

        public void OnAsignadoIrAPalanca(string conversationId)
        {
            // Ídem: no debería ocurrir, pero lo registramos.
            Debug.LogWarning($"[CerebroCamara] {name} recibió OnAsignadoIrAPalanca inesperadamente.");
        }

        // ── Rotación ─────────────────────────────────────────────────────────

        private void RotarHaciaJugador()
        {
            Vector3 direccion = (posicionJugadorActual - transform.position).normalized;
            direccion.y = 0f;
            if (direccion == Vector3.zero) return;

            Quaternion objetivo = Quaternion.LookRotation(direccion);
            transform.rotation  = Quaternion.Slerp(
                transform.rotation,
                objetivo,
                velocidadRotacion * Time.deltaTime
            );
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            // Dibujamos el cono de visión en cian para distinguirlo del guardia (amarillo).
            Gizmos.color = Color.cyan;
            float semi    = anguloVision * 0.5f;
            Vector3 o     = transform.position + Vector3.up * 1.5f;

            Gizmos.DrawRay(o, Quaternion.Euler(0,  semi, 0) * transform.forward * rangoVision);
            Gizmos.DrawRay(o, Quaternion.Euler(0, -semi, 0) * transform.forward * rangoVision);
            Gizmos.DrawRay(o, transform.forward * rangoVision);
        }
    }
}