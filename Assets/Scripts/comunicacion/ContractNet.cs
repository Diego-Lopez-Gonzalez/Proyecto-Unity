using System;
using System.Collections.Generic;
using UnityEngine;
using GuardiaIA.estados.Gestor;
using GuardiaIA.estados.Contratista;

namespace GuardiaIA
{
    /// Fábrica estática del protocolo Contract Net.
    /// Solo contiene métodos de construcción: no almacena estado propio.
    ///
    /// Dos fábricas:
    ///   CrearIniciador   — para el agente que detectó al ladrón y lanza el protocolo.
    ///   CrearParticipante — para los agentes que reciben un Cfp.
    public static class ContractNet
    {
        // ── Gestor ────────────────────────────────────────────────────────────

        /// Construye el contexto del gestor, envía el Cfp a todos los participantes
        /// y devuelve una MaquinaEstados iniciada en EsperandoPropuestas.
        ///
        /// <param name="convId">Identificador único de la conversación.</param>
        /// <param name="posLadron">Posición actual del ladrón.</param>
        /// <param name="tareasDisponibles">Tareas que el gestor quiere cubrir.</param>
        /// <param name="participantes">Vecinos a los que se enviará el Cfp.</param>
        /// <param name="gestor">GestorComunicacion del agente iniciador.</param>
        /// <param name="onResult">Callback invocado al adjudicar; recibe el mapa de asignaciones.</param>
        /// <param name="onTerminada">Callback invocado al terminar la conversación (Done/Cancelled).</param>
        public static MaquinaEstados CrearIniciador(
            string                                                      convId,
            Vector3                                                     posLadron,
            TareaContrato[]                                             tareasDisponibles,
            List<GestorComunicacion>                                    participantes,
            GestorComunicacion                                          gestor,
            Action<Dictionary<GestorComunicacion, TareaContrato>>       onResult,
            Action                                                      onTerminada = null)
        {
            var ctx = new ContextoCN
            {
                ConversationId       = convId,
                PosicionLadron       = posLadron,
                TareasDisponibles    = new List<TareaContrato>(tareasDisponibles),
                Timeout              = 0.5f,
                Gestor               = gestor,
                OnResult             = onResult,
                OnConversacionTerminada = onTerminada
            };

            // Añadir participantes al set de pendientes y enviarles el Cfp
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
                        TareasDisponibles = tareasDisponibles
                    }
                });
            }

            Debug.Log($"[ContractNet] Iniciador {gestor.name} lanzó Cfp a {participantes.Count} vecinos. " +
                      $"Tareas: {string.Join(", ", tareasDisponibles)}");

            return new MaquinaEstados(new EsperandoPropuestas(), ctx);
        }

        // ── Contratista ───────────────────────────────────────────────────────

        /// Evalúa si el agente puede participar y, si puede, envía Propose y devuelve
        /// una MaquinaEstados iniciada en EsperandoRespuesta.
        /// Si el agente está ocupado (EstaEnPersecucion == true), envía Refuse directamente
        /// y devuelve null sin instanciar ninguna máquina.
        ///
        /// <param name="cfp">El mensaje Cfp recibido (contiene convId, posición y tareas).</param>
        /// <param name="gestor">GestorComunicacion del agente participante.</param>
        /// <param name="onLiberar">Callback que recibe el convId al terminar la conversación.</param>
        public static MaquinaEstados CrearParticipante(
            MensajeACL          cfp,
            GestorComunicacion  gestor,
            Action<string>      onLiberar)
        {
            // ── Comprobación de disponibilidad ANTES de instanciar nada ────────
            if (gestor.Agente.EstaEnPersecucion)
            {
                gestor.Enviar(new MensajeACL
                {
                    Performativa   = Performativa.Refuse,
                    Emisor         = gestor,
                    Receptor       = cfp.Emisor,
                    ConversationId = cfp.ConversationId,
                    InReplyTo      = cfp.ConversationId
                });

                Debug.Log($"[ContractNet] {gestor.name} ocupado → Refuse a {cfp.Emisor?.name}");
                return null;
            }

            // ── Evaluar qué tareas puede asumir ───────────────────────────────
            // Por defecto el contratista acepta todas las tareas disponibles en el Cfp.
            // Aquí podría añadirse lógica específica según el tipo de agente.
            var tareasPosibles = cfp.Contenido.TareasDisponibles
                                 ?? System.Array.Empty<TareaContrato>();

            float distancia = Vector3.Distance(
                gestor.transform.position, cfp.Contenido.PosicionLadron);

            // ── Enviar Propose ────────────────────────────────────────────────
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
                    TareasPosibles    = tareasPosibles
                }
            });

            Debug.Log($"[ContractNet] {gestor.name} propone para conv:{cfp.ConversationId} " +
                      $"(dist:{distancia:F1}, tareas:{tareasPosibles.Length})");

            // ── Construir contexto y máquina ──────────────────────────────────
            var ctx = new ContextoCN
            {
                ConversationId          = cfp.ConversationId,
                PosicionLadron          = cfp.Contenido.PosicionLadron,
                Gestor                  = gestor,
                OnConversacionTerminada = () => onLiberar(cfp.ConversationId)
            };

            return new MaquinaEstados(new EsperandoRespuesta(), ctx);
        }
    }
}
