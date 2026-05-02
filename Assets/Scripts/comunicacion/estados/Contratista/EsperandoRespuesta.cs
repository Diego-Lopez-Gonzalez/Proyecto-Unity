using UnityEngine;

namespace GuardiaIA.estados.Contratista
{
    /// Estado del CONTRATISTA: el agente envió su Propose y espera la decisión del gestor.
    ///
    /// · AcceptProposal → notifica al agente local y transiciona a EjecutandoTarea.
    /// · RejectProposal → la conversación termina (Done); el agente queda libre.
    /// · Cancel         → el gestor abortó antes de decidir; vamos directamente a Done.
    public class EsperandoRespuesta : IEstadoCN
    {
        public void OnEnter(ContextoCN ctx)
        {
            Debug.Log($"[EsperandoRespuesta] conv:{ctx.ConversationId} — " +
                      $"esperando decisión del gestor.");
        }

        public IEstadoCN Handle(MensajeACL msg, ContextoCN ctx)
        {
            if (msg.ConversationId != ctx.ConversationId) return this;

            switch (msg.Performativa)
            {
                case Performativa.AcceptProposal:
                    // Guardamos referencia al gestor remoto para poder enviarle
                    // InformDone / Failure cuando terminemos la tarea
                    ctx.GestorRemoto = msg.Emisor;

                    Debug.Log($"[EsperandoRespuesta] Aceptado por {msg.Emisor?.name} " +
                              $"| tarea: {msg.Contenido.Tarea}");

                    // Notificamos al cerebro local para que ejecute la tarea
                    ctx.Gestor.Agente.OnTareaAsignada(msg.Contenido.Tarea, msg.ConversationId);
                    return new EjecutandoTarea();

                case Performativa.RejectProposal:
                    Debug.Log($"[EsperandoRespuesta] Rechazado por {msg.Emisor?.name}. → Done");
                    return new Done();

                case Performativa.Cancel:
                    Debug.Log($"[EsperandoRespuesta] Cancelado por gestor antes de decidir. → Done");
                    return new Done();
            }

            return this;
        }

        public IEstadoCN Tick(float delta, ContextoCN ctx) => this;
    }
}
