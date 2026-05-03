using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA.estados.Gestor
{
    public class EsperandoResultados : IEstadoCN
    {
        // FIX: timeout de seguridad para evitar conversaciones colgadas si un
        // contratista cae sin enviar InformDone ni Failure.
        private const float TIMEOUT = 120f;
        private float timer;

        private HashSet<GestorComunicacion> pendientes;
        private readonly HashSet<GestorComunicacion> fallidos = new HashSet<GestorComunicacion>();

        public void OnEnter(ContextoCN ctx)
        {
            pendientes = new HashSet<GestorComunicacion>(ctx.Asignaciones.Keys);
            timer      = TIMEOUT;
            Debug.Log($"[EsperandoResultados] conv:{ctx.ConversationId} — " +
                      $"esperando confirmación de {pendientes.Count} contratistas (timeout:{TIMEOUT}s).");
        }

        public IEstadoCN Handle(MensajeACL msg, ContextoCN ctx)
        {
            if (msg.ConversationId != ctx.ConversationId) return this;

            switch (msg.Performativa)
            {
                case Performativa.InformDone:
                    pendientes.Remove(msg.Emisor);
                    Debug.Log($"[EsperandoResultados] InformDone de {msg.Emisor?.name}. " +
                              $"Pendientes: {pendientes.Count}");
                    break;

                case Performativa.Failure:
                    pendientes.Remove(msg.Emisor);
                    fallidos.Add(msg.Emisor);
                    Debug.LogWarning($"[EsperandoResultados] Failure de {msg.Emisor?.name}. " +
                                     $"Pendientes: {pendientes.Count}");
                    break;
            }

            if (pendientes.Count == 0)
            {
                if (fallidos.Count > 0)
                    Debug.LogWarning($"[EsperandoResultados] {fallidos.Count} tarea(s) fallidas. → Done");
                else
                    Debug.Log($"[EsperandoResultados] Todas las tareas completadas. → Done");

                return new Done();
            }

            return this;
        }

        public IEstadoCN Tick(float delta, ContextoCN ctx)
        {
            timer -= delta;
            if (timer <= 0f)
            {
                Debug.LogWarning($"[EsperandoResultados] Timeout esperando resultados " +
                                 $"({pendientes.Count} contratistas sin responder). → Done");
                return new Done();
            }
            return this;
        }
    }
}