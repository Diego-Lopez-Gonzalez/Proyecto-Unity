using UnityEngine;

namespace GuardiaIA
{
    /// Estado de cancelación forzada del protocolo.
    /// Se entra cuando el gestor llama a MaquinaEstados.Cancelar()
    /// (p.ej. porque perdió al ladrón antes de que terminara la conversación).
    ///
    /// OnEnter envía Cancel a todos los participantes aún en ctx.Pendientes
    /// para que no queden agentes bloqueados esperando mensajes que no llegarán.
    public class Cancelled : IEstadoCN
    {
        public void OnEnter(ContextoCN ctx)
        {
            Debug.Log($"[Cancelled] Cancelando conv:{ctx.ConversationId}. " +
                      $"Participantes pendientes: {ctx.Pendientes.Count}");

            foreach (var participante in ctx.Pendientes)
            {
                ctx.Gestor.Enviar(new MensajeACL
                {
                    Performativa   = Performativa.Cancel,
                    Emisor         = ctx.Gestor,
                    Receptor       = participante,
                    ConversationId = ctx.ConversationId
                });
            }

            ctx.OnConversacionTerminada?.Invoke();
        }

        public IEstadoCN Handle(MensajeACL msg, ContextoCN ctx) => this;
        public IEstadoCN Tick(float delta, ContextoCN ctx)      => this;
    }
}
