using UnityEngine;

namespace GuardiaIA.estados.Contratista
{
    /// Estado del CONTRATISTA: el agente está ejecutando la tarea asignada por el gestor.
    ///
    /// El agente notifica la finalización llamando a
    /// GestorComunicacion.NotificarTareaCompletada(), que localiza este estado
    /// y llama a TareaCompletada(). Si no puede completar la tarea llama a TareaFallida().
    ///
    /// Si el gestor cancela la conversación mientras el agente ejecuta, llega un
    /// mensaje Cancel que hace abortar la tarea y transiciona a Done.
    public class EjecutandoTarea : IEstadoCN
    {
        public void OnEnter(ContextoCN ctx)
        {
            Debug.Log($"[EjecutandoTarea] conv:{ctx.ConversationId} — tarea en ejecución.");
        }

        public IEstadoCN Handle(MensajeACL msg, ContextoCN ctx)
        {
            if (msg.ConversationId != ctx.ConversationId) return this;

            if (msg.Performativa == Performativa.Cancel)
            {
                Debug.Log($"[EjecutandoTarea] Cancel recibido de {msg.Emisor?.name}. Abortando tarea.");
                ctx.Gestor.Agente.OnTareaCancelada(ctx.ConversationId);
                return new Done();
            }

            return this;
        }

        public IEstadoCN Tick(float delta, ContextoCN ctx) => this;

        // ── API pública llamada por el agente / GestorComunicacion ────────────

        /// El agente completó su tarea con éxito.
        /// Envía InformDone al gestor remoto y devuelve el estado Done.
        /// GestorComunicacion llama a este método desde NotificarTareaCompletada()
        /// y después fuerza la transición con el Done devuelto.
        public Done TareaCompletada(ContextoCN ctx)
        {
            Debug.Log($"[EjecutandoTarea] Tarea completada en conv:{ctx.ConversationId}. Enviando InformDone.");

            ctx.Gestor.Enviar(new MensajeACL
            {
                Performativa   = Performativa.InformDone,
                Emisor         = ctx.Gestor,
                Receptor       = ctx.GestorRemoto,
                ConversationId = ctx.ConversationId,
                InReplyTo      = ctx.ConversationId
            });

            return new Done();
        }

        /// El agente no pudo completar su tarea (perdió al objetivo, bloqueado…).
        /// Envía Failure al gestor remoto y devuelve el estado Done.
        public Done TareaFallida(ContextoCN ctx)
        {
            Debug.LogWarning($"[EjecutandoTarea] Tarea fallida en conv:{ctx.ConversationId}. Enviando Failure.");

            ctx.Gestor.Enviar(new MensajeACL
            {
                Performativa   = Performativa.Failure,
                Emisor         = ctx.Gestor,
                Receptor       = ctx.GestorRemoto,
                ConversationId = ctx.ConversationId,
                InReplyTo      = ctx.ConversationId
            });

            return new Done();
        }
    }
}
