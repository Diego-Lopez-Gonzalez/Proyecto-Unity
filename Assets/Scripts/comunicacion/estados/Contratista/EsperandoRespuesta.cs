using UnityEngine;

namespace GuardiaIA.estados.Contratista
{
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
                    ctx.GestorRemoto = msg.Emisor;

                    Debug.Log($"[EsperandoRespuesta] Aceptado por {msg.Emisor?.name} " +
                              $"| tarea:{msg.Contenido.Tarea} [{ctx.Prioridad}]");

                    // Notificamos al cerebro con la prioridad del contrato
                    // para que pueda comparar con futuros Cfp entrantes.
                    ctx.Gestor.NotificarTareaAsignada(
                        msg.Contenido.Tarea,
                        msg.ConversationId,
                        ctx.Prioridad);

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