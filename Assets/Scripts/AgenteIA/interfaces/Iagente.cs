using UnityEngine;

namespace GuardiaIA
{
    /// Contrato que deben cumplir todos los cerebros de agente (guardias, cámaras…).
    /// SensorVision y GestorComunicacion dependen únicamente de esta interfaz,
    /// lo que permite añadir nuevos tipos de agente sin tocar esos componentes.
    ///
    /// Callbacks de visión — llamados por SensorVision:
    ///   · OnJugadorDetectado          → el jugador acaba de entrar en el cono.
    ///   · OnActualizarPosicionJugador → el jugador sigue visible, posición nueva.
    ///   · OnJugadorPerdido            → el jugador ha salido del cono.
    ///   · OnObjetoDesaparecido        → el objeto vigilado no está donde estaba.
    ///
    /// Callbacks de contrato — llamados por GestorComunicacion:
    ///   · OnTareaAsignada   → el gestor aceptó la propuesta y asigna una tarea concreta.
    ///   · OnTareaCancelada  → el gestor canceló la conversación en curso.
    ///
    /// Propiedad de estado — leída por GestorComunicacion para decidir disponibilidad:
    ///   · EstaEnPersecucion → true solo mientras el agente persigue activamente.
    ///                         Las cámaras devuelven true siempre para quedar excluidas
    ///                         como contratistas.
    public interface IAgente
    {
        // ── Callbacks de visión ──────────────────────────────────────────────
        void OnJugadorDetectado(Vector3 posicion);
        void OnActualizarPosicionJugador(Vector3 posicion);
        void OnJugadorPerdido();
        void OnObjetoDesaparecido();

        // ── Callbacks de contrato ────────────────────────────────────────────
        void OnTareaAsignada(TareaContrato tarea, string conversationId);
        void OnTareaCancelada(string conversationId);

        // ── Estado ───────────────────────────────────────────────────────────
        bool EstaEnPersecucion { get; }
    }
}