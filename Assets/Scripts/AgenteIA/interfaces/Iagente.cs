using UnityEngine;

namespace GuardiaIA
{
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

        /// True cuando el agente NO puede aceptar ningún contrato nuevo
        /// (persecución activa o tarea de contrato en curso).
        /// Sustituye a EstaEnPersecucion para reflejar la ocupación real.
        bool EstaOcupado { get; }

        /// Prioridad de la tarea de contrato que está ejecutando actualmente.
        /// Si no tiene ninguna tarea activa devuelve Baja.
        PrioridadContrato PrioridadTareaActual { get; }

        /// Llamado por ContractNet cuando llega un Cfp de mayor prioridad.
        /// El agente debe cancelar su tarea actual limpiamente antes de
        /// que ContractNet envíe el nuevo Propose.
        void InterrumpirTareaActual();
    }
}