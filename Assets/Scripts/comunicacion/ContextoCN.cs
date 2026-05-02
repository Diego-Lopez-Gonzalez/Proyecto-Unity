using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public class ContextoCN
    {
        public string   ConversationId;
        public Vector3  PosicionLadron;

        /// Prioridad declarada por el gestor al crear el contrato.
        public PrioridadContrato Prioridad;

        public List<TareaContrato> TareasDisponibles = new List<TareaContrato>();
        public float Timeout = 0.5f;

        public List<MensajeACL> Propuestas = new List<MensajeACL>();
        public Dictionary<GestorComunicacion, TareaContrato> Asignaciones =
            new Dictionary<GestorComunicacion, TareaContrato>();
        public HashSet<GestorComunicacion> Pendientes = new HashSet<GestorComunicacion>();

        public GestorComunicacion Gestor;
        public GestorComunicacion GestorRemoto;

        public Action<Dictionary<GestorComunicacion, TareaContrato>> OnResult;
        public Action OnConversacionTerminada;
    }
}