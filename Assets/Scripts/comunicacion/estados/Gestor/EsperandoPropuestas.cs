using UnityEngine;

namespace GuardiaIA.estados.Gestor
{
    /// Estado del GESTOR: espera propuestas de los contratistas a los que envió Cfp.
    ///
    /// Transiciona a Evaluando cuando:
    ///   · Todos los participantes han respondido (Propose o Refuse), O
    ///   · Expira el timer de espera definido en ctx.Timeout.
    public class EsperandoPropuestas : IEstadoCN
    {
        private float timer;

        public void OnEnter(ContextoCN ctx)
        {
            timer = ctx.Timeout;
            Debug.Log($"[EsperandoPropuestas] conv:{ctx.ConversationId} " +
                      $"esperando {ctx.Pendientes.Count} respuestas (timeout:{timer}s)");
        }

        public IEstadoCN Handle(MensajeACL msg, ContextoCN ctx)
        {
            if (msg.ConversationId != ctx.ConversationId) return this;

            switch (msg.Performativa)
            {
                case Performativa.Propose:
                    ctx.Propuestas.Add(msg);
                    ctx.Pendientes.Remove(msg.Emisor);
                    Debug.Log($"[EsperandoPropuestas] Propose de {msg.Emisor?.name} " +
                              $"(dist:{msg.Contenido.DistanciaAlLadron:F1}). " +
                              $"Pendientes restantes: {ctx.Pendientes.Count}");
                    break;

                case Performativa.Refuse:
                    ctx.Pendientes.Remove(msg.Emisor);
                    Debug.Log($"[EsperandoPropuestas] Refuse de {msg.Emisor?.name}. " +
                              $"Pendientes restantes: {ctx.Pendientes.Count}");
                    break;
            }

            // Todos respondieron: pasamos a adjudicar sin esperar más
            if (ctx.Pendientes.Count == 0)
            {
                Debug.Log($"[EsperandoPropuestas] Todos respondieron → Evaluando");
                return new Evaluando();
            }

            return this;
        }

        public IEstadoCN Tick(float delta, ContextoCN ctx)
        {
            timer -= delta;
            if (timer <= 0f)
            {
                Debug.Log($"[EsperandoPropuestas] Timeout → Evaluando " +
                          $"({ctx.Propuestas.Count} propuestas recibidas)");
                return new Evaluando();
            }
            return this;
        }
    }
}
