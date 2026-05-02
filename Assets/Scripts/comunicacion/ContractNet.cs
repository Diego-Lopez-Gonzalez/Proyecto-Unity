using System;
using System.Collections.Generic;
using UnityEngine;
using GuardiaIA.estados.Gestor;
using GuardiaIA.estados.Contratista;

namespace GuardiaIA
{
    public static class ContractNet
    {
        // ── Gestor ────────────────────────────────────────────────────────────

        /// <param name="prioridad">Prioridad del contrato. Los contratistas la usan
        /// para decidir si interrumpen una tarea activa de menor prioridad.</param>
        public static MaquinaEstados CrearIniciador(
            string                                                      convId,
            Vector3                                                     posLadron,
            TareaContrato[]                                             tareasDisponibles,
            List<GestorComunicacion>                                    participantes,
            GestorComunicacion                                          gestor,
            Action<Dictionary<GestorComunicacion, TareaContrato>>       onResult,
            Action                                                      onTerminada = null,
            PrioridadContrato                                           prioridad   = PrioridadContrato.Media)
        {
            var ctx = new ContextoCN
            {
                ConversationId          = convId,
                PosicionLadron          = posLadron,
                TareasDisponibles       = new List<TareaContrato>(tareasDisponibles),
                Timeout                 = 0.5f,
                Gestor                  = gestor,
                Prioridad               = prioridad,
                OnResult                = onResult,
                OnConversacionTerminada = onTerminada
            };

            foreach (var p in participantes)
            {
                ctx.Pendientes.Add(p);

                gestor.Enviar(new MensajeACL
                {
                    Performativa   = Performativa.Cfp,
                    Emisor         = gestor,
                    Receptor       = p,
                    ConversationId = convId,
                    Contenido      = new ContenidoMensaje
                    {
                        PosicionLadron    = posLadron,
                        TareasDisponibles = tareasDisponibles,
                        Prioridad         = prioridad
                    }
                });
            }

            Debug.Log($"[ContractNet] Iniciador {gestor.name} lanzó Cfp [{prioridad}] " +
                      $"a {participantes.Count} vecinos. Tareas: {string.Join(", ", tareasDisponibles)}");

            return new MaquinaEstados(new EsperandoPropuestas(), ctx);
        }

        // ── Contratista ───────────────────────────────────────────────────────

        /// Evalúa si el agente puede participar teniendo en cuenta la prioridad:
        ///   · Libre                          → acepta siempre.
        ///   · Ocupado con tarea de MENOR prioridad → cancela la tarea activa y acepta.
        ///   · Ocupado con tarea de IGUAL o MAYOR prioridad → Refuse.
        public static MaquinaEstados CrearParticipante(
            MensajeACL          cfp,
            GestorComunicacion  gestor,
            Action<string>      onLiberar)
        {
            PrioridadContrato prioridadEntrante = cfp.Contenido.Prioridad;
            PrioridadContrato prioridadActual   = gestor.Agente.PrioridadTareaActual;

            bool ocupado = gestor.Agente.EstaOcupado;

            // Si está ocupado comprobamos si el nuevo contrato tiene más prioridad
            if (ocupado)
            {
                if (prioridadEntrante <= prioridadActual)
                {
                    // Mismo nivel o menor → Refuse
                    gestor.Enviar(new MensajeACL
                    {
                        Performativa   = Performativa.Refuse,
                        Emisor         = gestor,
                        Receptor       = cfp.Emisor,
                        ConversationId = cfp.ConversationId,
                        InReplyTo      = cfp.ConversationId
                    });

                    Debug.Log($"[ContractNet] {gestor.name} ocupado [{prioridadActual}] " +
                              $"≥ entrante [{prioridadEntrante}] → Refuse");
                    return null;
                }

                // Mayor prioridad → cancelamos la tarea activa antes de aceptar
                Debug.Log($"[ContractNet] {gestor.name} interrumpe tarea [{prioridadActual}] " +
                          $"por contrato de mayor prioridad [{prioridadEntrante}]");
                gestor.Agente.InterrumpirTareaActual();
            }

            // Evaluar distancia y enviar Propose
            var tareasPosibles = cfp.Contenido.TareasDisponibles
                                 ?? System.Array.Empty<TareaContrato>();

            float distancia = Vector3.Distance(
                gestor.transform.position, cfp.Contenido.PosicionLadron);

            gestor.Enviar(new MensajeACL
            {
                Performativa   = Performativa.Propose,
                Emisor         = gestor,
                Receptor       = cfp.Emisor,
                ConversationId = cfp.ConversationId,
                InReplyTo      = cfp.ConversationId,
                Contenido      = new ContenidoMensaje
                {
                    PosicionLadron    = cfp.Contenido.PosicionLadron,
                    DistanciaAlLadron = distancia,
                    TareasPosibles    = tareasPosibles,
                    Prioridad         = prioridadEntrante
                }
            });

            Debug.Log($"[ContractNet] {gestor.name} propone para conv:{cfp.ConversationId} " +
                      $"[{prioridadEntrante}] (dist:{distancia:F1})");

            var ctx = new ContextoCN
            {
                ConversationId          = cfp.ConversationId,
                PosicionLadron          = cfp.Contenido.PosicionLadron,
                Prioridad               = prioridadEntrante,
                Gestor                  = gestor,
                OnConversacionTerminada = () => onLiberar(cfp.ConversationId)
            };

            return new MaquinaEstados(new EsperandoRespuesta(), ctx);
        }
    }
}