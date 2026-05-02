namespace GuardiaIA
{
    /// Interfaz que implementan todos los estados del protocolo Contract Net.
    ///
    /// Cada estado es inmutable respecto al flujo: Handle y Tick devuelven el
    /// estado siguiente en lugar de mutar el estado actual. Si no hay transición,
    /// devuelven this. La máquina de estados compara la referencia para decidir
    /// si hubo cambio real y llamar OnEnter solo entonces.
    public interface IEstadoCN
    {
        /// Llamado por la máquina exactamente una vez al entrar en este estado.
        void OnEnter(ContextoCN ctx);

        /// Procesa un mensaje entrante y devuelve el estado siguiente (o this).
        IEstadoCN Handle(MensajeACL msg, ContextoCN ctx);

        /// Avanza el timer interno y devuelve el estado siguiente (o this).
        IEstadoCN Tick(float delta, ContextoCN ctx);
    }
}
