using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    /// Componente Unity que conecta el buzón de mensajes con el Dispatcher y
    /// expone la API pública que los cerebros (IAgente) usan para participar
    /// en el protocolo Contract Net.
    ///
    /// No contiene NINGUNA variable de estado del protocolo:
    /// toda esa responsabilidad está en ContextoCN + estados + Dispatcher.
    public class GestorComunicacion : MonoBehaviour
    {
        // ── Componentes ───────────────────────────────────────────────────────
        private BuzonMensajes           buzon;
        private IAgente                 agente;
        private Dispatcher              dispatcher;

        // ── Vecinos ───────────────────────────────────────────────────────────
        private List<GestorComunicacion> vecinos = new List<GestorComunicacion>();

        // ── Accesores internos (usados por estados y ContractNet) ─────────────
        internal BuzonMensajes Buzon  => buzon;
        internal IAgente       Agente => agente;

        // ══════════════════════════════════════════════════════════════════════
        //  CICLO DE VIDA UNITY
        // ══════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            buzon      = GetComponent<BuzonMensajes>();
            agente     = GetComponent<IAgente>();
            dispatcher = new Dispatcher(this);

            if (agente == null)
                Debug.LogError($"[GestorComunicacion] {name}: no se encontró componente IAgente.");
        }

        private void Start()
        {
            foreach (var g in FindObjectsOfType<GestorComunicacion>())
            {
                if (g != this)
                    vecinos.Add(g);
            }

            Debug.Log($"[GestorComunicacion] {name} descubrió {vecinos.Count} vecinos.");
        }

        private void Update()
        {
            // Drenar el buzón y enrutar cada mensaje a su máquina de estados
            foreach (var msg in buzon.ProcesarPendientes())
                dispatcher.Enrutar(msg);

            // Avanzar todos los timers activos
            dispatcher.TickTimers(Time.deltaTime);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  API PÚBLICA — llamada desde IAgente / estados de comportamiento
        // ══════════════════════════════════════════════════════════════════════

        /// El agente acaba de detectar al ladrón y quiere lanzar el Contract Net.
        /// Genera un ConversationId único, crea la máquina iniciadora y la registra.
        ///
        /// <param name="posicionLadron">Posición actual del ladrón.</param>
        /// <param name="tareasDisponibles">Tareas que el gestor quiere cubrir.</param>
        public void IniciarContractNet(Vector3 posicionLadron, TareaContrato[] tareasDisponibles)
        {
            string convId = $"cn_{name}_{Time.frameCount}";

            var maquina = ContractNet.CrearIniciador(
                convId,
                posicionLadron,
                tareasDisponibles,
                vecinos,
                this,
                asignaciones =>
                {
                    Debug.Log($"[GestorComunicacion] {name}: adjudicación completada " +
                              $"en conv:{convId} — {asignaciones.Count} asignaciones.");
                },
                onTerminada: () => dispatcher.Liberar(convId)
            );

            dispatcher.Registrar(convId, maquina);
        }

        /// El agente completó su tarea asignada.
        /// Localiza la máquina activa para esa conversación y notifica el InformDone.
        public void NotificarTareaCompletada(string conversationId)
        {
            dispatcher.NotificarTareaCompletada(conversationId);
        }

        /// El agente no pudo completar su tarea (perdió al objetivo, bloqueado…).
        /// Envía Failure al gestor y cierra la conversación local.
        public void NotificarTareaFallida(string conversationId)
        {
            dispatcher.NotificarTareaFallida(conversationId);
        }

        /// El gestor pierde al ladrón: cancela la conversación activa enviando
        /// Cancel a todos los participantes pendientes y limpiando el estado.
        public void CancelarConversacion(string conversationId)
        {
            Debug.Log($"[GestorComunicacion] {name} cancela conv:{conversationId}");
            dispatcher.CancelarConversacion(conversationId);
        }

        /// Libera explícitamente una conversación del registro del dispatcher.
        /// Llamado desde Evaluando cuando rechaza a un contratista que quedó libre.
        public void LiberarConversacion(string conversationId)
        {
            dispatcher.Liberar(conversationId);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  UTILIDADES INTERNAS
        // ══════════════════════════════════════════════════════════════════════

        /// Deposita un mensaje en el buzón del receptor.
        /// Acceso internal para que los estados y ContractNet puedan enviar mensajes.
        internal void Enviar(MensajeACL mensaje)
        {
            Debug.Log($"[GestorComunicacion] Enviando: {mensaje}");
            mensaje.Receptor?.Buzon.Recibir(mensaje);
        }
    }
}
