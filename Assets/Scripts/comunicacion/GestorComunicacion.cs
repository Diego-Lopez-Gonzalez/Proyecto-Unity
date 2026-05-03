using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public class GestorComunicacion : MonoBehaviour
    {
        private BuzonMensajes            buzon;
        private IAgente                  agente;
        private Dispatcher               dispatcher;

        private List<GestorComunicacion> todosLosVecinos = new List<GestorComunicacion>();

        internal BuzonMensajes Buzon  => buzon;
        internal IAgente       Agente => agente;

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
                if (g != this && !(g.Agente is CerebroCamara))
                    todosLosVecinos.Add(g);
            }
            Debug.Log($"[GestorComunicacion] {name} descubrió {todosLosVecinos.Count} vecinos.");
        }

        private void Update()
        {
            foreach (var msg in buzon.ProcesarPendientes())
                dispatcher.Enrutar(msg);

            dispatcher.TickTimers(Time.deltaTime);
        }

        // ── API pública ───────────────────────────────────────────────────────

        public void IniciarContractNet(
            Vector3           posicionLadron,
            TareaContrato[]   tareasDisponibles,
            PrioridadContrato prioridad = PrioridadContrato.Media)
        {
            if (todosLosVecinos.Count == 0)
            {
                Debug.Log($"[GestorComunicacion] {name}: ningún vecino, " +
                          $"no se inicia Contract Net.");
                return;
            }

            string convId = $"cn_{name}_{Time.frameCount}";

            var maquina = ContractNet.CrearIniciador(
                convId,
                posicionLadron,
                tareasDisponibles,
                todosLosVecinos, // ← todos los vecinos, la prioridad se gestiona en CrearParticipante
                this,
                asignaciones =>
                {
                    Debug.Log($"[GestorComunicacion] {name}: adjudicación completada " +
                              $"en conv:{convId} — {asignaciones.Count} asignaciones.");
                },
                onTerminada: () => dispatcher.Liberar(convId),
                prioridad:   prioridad
            );

            dispatcher.Registrar(convId, maquina);
        }

        public void NotificarTareaCompletada(string conversationId)
        {
            dispatcher.NotificarTareaCompletada(conversationId);
        }

        public void NotificarTareaFallida(string conversationId)
        {
            dispatcher.NotificarTareaFallida(conversationId);
        }

        public void CancelarConversacion(string conversationId)
        {
            Debug.Log($"[GestorComunicacion] {name} cancela conv:{conversationId}");
            dispatcher.CancelarConversacion(conversationId);
        }

        public void LiberarConversacion(string conversationId)
        {
            dispatcher.Liberar(conversationId);
        }

        internal void Enviar(MensajeACL mensaje)
        {
            Debug.Log($"[GestorComunicacion] Enviando: {mensaje}");
            mensaje.Receptor?.Buzon.Recibir(mensaje);
        }

        internal void NotificarTareaAsignada(
            TareaContrato     tarea,
            string            conversationId,
            PrioridadContrato prioridad)
        {
            if (agente is Cerebro cerebro)
                cerebro.OnTareaAsignada(tarea, conversationId, prioridad);
        }
    }
}