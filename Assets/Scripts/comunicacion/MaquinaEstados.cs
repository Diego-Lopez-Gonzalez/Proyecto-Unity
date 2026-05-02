using UnityEngine;

namespace GuardiaIA
{
    /// Motor genérico de estados para el protocolo Contract Net.
    /// No contiene ninguna referencia a clases concretas del protocolo:
    /// solo almacena el estado actual y el contexto, y delega toda la lógica
    /// en los estados vía IEstadoCN.
    public class MaquinaEstados
    {
        private IEstadoCN   estadoActual;
        private readonly ContextoCN ctx;

        /// Estado actual (lectura externa para casteos en NotificarTareaCompletada).
        public IEstadoCN EstadoActual => estadoActual;

        /// Contexto asociado (lectura externa para que Dispatcher pase ctx a
        /// métodos de estados que necesitan enviar mensajes, p.ej. TareaCompletada).
        public ContextoCN Ctx => ctx;

        public MaquinaEstados(IEstadoCN estadoInicial, ContextoCN ctx)
        {
            this.ctx      = ctx;
            estadoActual  = estadoInicial;
            estadoActual.OnEnter(ctx);
        }

        /// Entrega un mensaje al estado actual; transiciona si el estado cambia.
        public void Handle(MensajeACL msg)
        {
            var siguiente = estadoActual.Handle(msg, ctx);
            Transicionar(siguiente);
        }

        /// Avanza el tiempo del estado actual; transiciona si el estado cambia.
        public void Tick(float delta)
        {
            var siguiente = estadoActual.Tick(delta, ctx);
            Transicionar(siguiente);
        }

        /// Cancela la conversación forzando la transición a Cancelled.
        /// Útil cuando el gestor pierde al ladrón antes de que acabe el protocolo.
        public void Cancelar() => Transicionar(new Cancelled());

        /// Permite que agentes externos fuercen una transición ya calculada
        /// (p.ej. Dispatcher al procesar NotificarTareaCompletada).
        public void ForzarTransicion(IEstadoCN siguiente) => Transicionar(siguiente);

        // ── Interno ───────────────────────────────────────────────────────────

        private void Transicionar(IEstadoCN siguiente)
        {
            if (siguiente == estadoActual) return;

            Debug.Log($"[MaquinaEstados] conv:{ctx.ConversationId} " +
                      $"{estadoActual.GetType().Name} → {siguiente.GetType().Name}");

            estadoActual = siguiente;
            estadoActual.OnEnter(ctx);
        }
    }
}
