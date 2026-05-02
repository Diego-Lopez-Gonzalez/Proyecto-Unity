using UnityEngine;

namespace GuardiaIA
{
    /// Estado terminal de la conversación: éxito o rechazo normal.
    /// Compartido por gestor y contratista.
    /// OnEnter notifica el fin de la conversación; después absorbe todo sin actuar.
    public class Done : IEstadoCN
    {
        public void OnEnter(ContextoCN ctx)
        {
            Debug.Log($"[Done] Conversación {ctx.ConversationId} finalizada correctamente.");
            ctx.OnConversacionTerminada?.Invoke();
        }

        public IEstadoCN Handle(MensajeACL msg, ContextoCN ctx) => this;
        public IEstadoCN Tick(float delta, ContextoCN ctx)      => this;
    }
}
