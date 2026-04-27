using UnityEngine;

namespace GuardiaIA
{
    /// Contrato común a todos los agentes del sistema (guardias, cámaras, etc.).
    ///
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
    ///   · OnAsignadoCerrarZona        → este agente debe bloquear la zona de escape.
    ///   · OnAsignadoIrAPalanca        → este agente debe activar la palanca.
    ///
    /// Propiedad de estado — leída por GestorComunicacion para decidir disponibilidad:
    ///   · EstaEnPersecucion           → true solo mientras el agente persigue activamente.
    public interface IAgente
    {
        // ── Callbacks de visión ──────────────────────────────────────────────
        void OnJugadorDetectado(Vector3 posicion);
        void OnActualizarPosicionJugador(Vector3 posicion);
        void OnJugadorPerdido();
        void OnObjetoDesaparecido();

        // ── Callbacks de contrato ────────────────────────────────────────────
        void OnAsignadoCerrarZona(Vector3 posicionLadron, string conversationId);
        void OnAsignadoIrAPalanca(string conversationId);

        // ── Estado ───────────────────────────────────────────────────────────
        bool EstaEnPersecucion { get; }
    }
}