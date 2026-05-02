using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public class GestorComunicacion : MonoBehaviour
    {
        private BuzonMensajes            buzon;
        private IAgente                  agente;
        private Dispatcher               dispatcher;

        // Lista de TODOS los gestores conocidos (excluye a uno mismo y a cámaras).
        // Se rellena una sola vez en Start y nunca cambia: todos los agentes
        // que pueden ser contratistas están aquí.
        // La disponibilidad real se comprueba en el momento de enviar el Cfp.
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
                // Excluimos a nosotros mismos y a agentes que NUNCA pueden
                // ser contratistas (cámaras: EstaOcupado siempre true).
                // No filtramos por EstaOcupado aquí porque cambia en tiempo real.
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

        /// <param name="prioridad">Prioridad del contrato. Los contratistas con tarea
        /// de menor prioridad la interrumpirán para aceptar este contrato.</param>
        public void IniciarContractNet(
            Vector3           posicionLadron,
            TareaContrato[]   tareasDisponibles,
            PrioridadContrato prioridad = PrioridadContrato.Media)
        {
            // Evaluamos la disponibilidad AHORA, no en Start.
            // Así un guardia que acaba de terminar su tarea ya aparece como libre,
            // y uno que acaba de aceptar un contrato ya no recibe el Cfp.
            var vecinosDisponibles = new List<GestorComunicacion>();
            foreach (var v in todosLosVecinos)
            {
                if (!v.Agente.EstaOcupado)
                    vecinosDisponibles.Add(v);
            }

            if (vecinosDisponibles.Count == 0)
            {
                Debug.Log($"[GestorComunicacion] {name}: ningún vecino disponible, " +
                          $"no se inicia Contract Net.");
                return;
            }

            string convId = $"cn_{name}_{Time.frameCount}";

            var maquina = ContractNet.CrearIniciador(
                convId,
                posicionLadron,
                tareasDisponibles,
                vecinosDisponibles,
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

        /// Llamado por EsperandoRespuesta al recibir AcceptProposal.
        /// Pasa la prioridad al cerebro para que la guarde en BaseConocimiento.
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