using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuardiaIA.estados.Gestor
{
    public class Evaluando : IEstadoCN
    {
        public void OnEnter(ContextoCN ctx)
        {
            Debug.Log($"[Evaluando] conv:{ctx.ConversationId} — " +
                      $"{ctx.Propuestas.Count} propuestas, {ctx.TareasDisponibles.Count} tareas");

            var propuestasOrdenadas   = ctx.Propuestas
                .OrderBy(p => p.Contenido.DistanciaAlLadron)
                .ToList();

            var contratistasUsados    = new HashSet<GestorComunicacion>();
            var contratistasAceptados = new HashSet<GestorComunicacion>();

            foreach (var tarea in ctx.TareasDisponibles)
            {
                bool asignado = false;

                foreach (var prop in propuestasOrdenadas)
                {
                    if (contratistasUsados.Contains(prop.Emisor)) continue;

                    var posibles = prop.Contenido.TareasPosibles;
                    if (posibles == null || !posibles.Contains(tarea)) continue;

                    contratistasUsados.Add(prop.Emisor);
                    contratistasAceptados.Add(prop.Emisor);
                    ctx.Asignaciones[prop.Emisor] = tarea;

                    ctx.Gestor.Enviar(new MensajeACL
                    {
                        Performativa   = Performativa.AcceptProposal,
                        Emisor         = ctx.Gestor,
                        Receptor       = prop.Emisor,
                        ConversationId = ctx.ConversationId,
                        InReplyTo      = ctx.ConversationId,
                        Contenido      = new ContenidoMensaje
                        {
                            Tarea          = tarea,
                            PosicionLadron = ctx.PosicionLadron
                        }
                    });

                    Debug.Log($"[Evaluando] AcceptProposal → {prop.Emisor?.name} | tarea:{tarea}");
                    asignado = true;
                    break;
                }

                if (!asignado)
                    Debug.LogWarning($"[Evaluando] No se encontró contratista disponible para tarea: {tarea}");
            }

            // Rechazar contratistas no seleccionados.
            // FIX: eliminada la llamada manual a LiberarConversacion — el RejectProposal
            // hace transicionar al contratista a Done, que invoca OnConversacionTerminada → Liberar.
            foreach (var prop in propuestasOrdenadas)
            {
                if (contratistasAceptados.Contains(prop.Emisor)) continue;

                ctx.Gestor.Enviar(new MensajeACL
                {
                    Performativa   = Performativa.RejectProposal,
                    Emisor         = ctx.Gestor,
                    Receptor       = prop.Emisor,
                    ConversationId = ctx.ConversationId,
                    InReplyTo      = ctx.ConversationId
                });

                Debug.Log($"[Evaluando] RejectProposal → {prop.Emisor?.name}");
            }

            ctx.OnResult?.Invoke(ctx.Asignaciones);

            Debug.Log($"[Evaluando] Adjudicación completada: {ctx.Asignaciones.Count} asignaciones.");
        }

        public IEstadoCN Handle(MensajeACL msg, ContextoCN ctx) => this;

        public IEstadoCN Tick(float delta, ContextoCN ctx)
        {
            if (ctx.Asignaciones.Count == 0)
            {
                Debug.Log($"[Evaluando] Sin asignaciones → Done");
                return new Done();
            }

            return new EsperandoResultados();
        }
    }
}