using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    /// Clase de datos pura: almacena todo el estado de una conversación Contract Net.
    /// No contiene lógica; los estados leen y escriben en ella directamente.
    /// Una instancia se crea en ContractNet.CrearIniciador / CrearParticipante
    /// y vive mientras la MaquinaEstados asociada esté activa.
    public class ContextoCN
    {
        // ── Identificación ────────────────────────────────────────────────────
        public string   ConversationId;
        public Vector3  PosicionLadron;

        // ── Plan del gestor ───────────────────────────────────────────────────
        /// Tareas que el gestor quiere cubrir en esta convocatoria.
        public List<TareaContrato> TareasDisponibles = new List<TareaContrato>();

        /// Duración máxima de la fase de espera de propuestas (segundos).
        public float Timeout = 0.5f;

        // ── Datos de negociación (gestor) ─────────────────────────────────────
        /// Propuestas recibidas de los contratistas.
        public List<MensajeACL> Propuestas = new List<MensajeACL>();

        /// Contratistas adjudicados: GestorComunicacion → tarea asignada.
        public Dictionary<GestorComunicacion, TareaContrato> Asignaciones =
            new Dictionary<GestorComunicacion, TareaContrato>();

        /// Participantes de los que aún esperamos respuesta (Propose / Refuse
        /// en fase de espera; InformDone / Failure en fase de resultados).
        public HashSet<GestorComunicacion> Pendientes = new HashSet<GestorComunicacion>();

        // ── Agentes implicados ────────────────────────────────────────────────
        /// GestorComunicacion local (el agente dueño de esta máquina de estados).
        /// En el contexto del gestor es el iniciador; en el del contratista, él mismo.
        public GestorComunicacion Gestor;

        /// Para contextos de contratista: referencia al gestor remoto que envió
        /// el AcceptProposal. Se asigna en EsperandoRespuesta al procesar ese mensaje.
        /// Necesaria para enviar InformDone / Failure de vuelta al gestor correcto.
        public GestorComunicacion GestorRemoto;

        // ── Callbacks ─────────────────────────────────────────────────────────
        /// Invocado por Evaluando cuando el gestor ha adjudicado todas las tareas.
        /// Recibe el diccionario de asignaciones para que el llamador pueda reaccionar.
        public Action<Dictionary<GestorComunicacion, TareaContrato>> OnResult;

        /// Invocado por Done y Cancelled al entrar: señal de fin de conversación.
        /// Debe usarse para liberar recursos (p.ej. eliminar la máquina del Dispatcher).
        public Action OnConversacionTerminada;
    }
}
