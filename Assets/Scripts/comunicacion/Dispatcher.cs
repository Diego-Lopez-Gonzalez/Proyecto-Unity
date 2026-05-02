using System.Collections.Generic;
using UnityEngine;
using GuardiaIA.estados.Contratista;

namespace GuardiaIA
{
    public class Dispatcher
    {
        private readonly Dictionary<string, MaquinaEstados> maquinas =
            new Dictionary<string, MaquinaEstados>();

        private readonly GestorComunicacion gestor;

        public Dispatcher(GestorComunicacion gestor)
        {
            this.gestor = gestor;
        }

        public void Enrutar(MensajeACL msg)
        {
            if (string.IsNullOrEmpty(msg.ConversationId))
            {
                Debug.LogWarning($"[Dispatcher] {gestor.name} recibió mensaje sin ConversationId: {msg}");
                return;
            }

            if (msg.Performativa == Performativa.Cancel)
            {
                CancelarConversacion(msg.ConversationId);
                return;
            }

            if (maquinas.TryGetValue(msg.ConversationId, out var maquina))
            {
                maquina.Handle(msg);
            }
            else if (msg.Performativa == Performativa.Cfp)
            {
                var nueva = ContractNet.CrearParticipante(msg, gestor, Liberar);
                if (nueva != null)
                    Registrar(msg.ConversationId, nueva);
            }
            else
            {
                Debug.LogWarning($"[Dispatcher] {gestor.name} descartó mensaje " +
                                 $"para conv desconocida: {msg}");
            }
        }

        public void Registrar(string convId, MaquinaEstados maquina)
        {
            maquinas[convId] = maquina;
            Debug.Log($"[Dispatcher] {gestor.name} registró conv:{convId}. " +
                      $"Conversaciones activas: {maquinas.Count}");
        }

        public void TickTimers(float delta)
        {
            var claves = new List<string>(maquinas.Keys);
            foreach (var clave in claves)
            {
                if (maquinas.TryGetValue(clave, out var m))
                    m.Tick(delta);
            }
        }

        public void Liberar(string convId)
        {
            if (maquinas.Remove(convId))
                Debug.Log($"[Dispatcher] {gestor.name} liberó conv:{convId}. " +
                          $"Conversaciones activas: {maquinas.Count}");
        }

        public void CancelarConversacion(string convId)
        {
            if (!maquinas.TryGetValue(convId, out var maquina))
            {
                Debug.LogWarning($"[Dispatcher] CancelarConversacion: conv:{convId} no encontrada.");
                return;
            }

            // FIX: eliminado el segundo Liberar(convId) manual.
            // Cancelar() → Cancelled.OnEnter → OnConversacionTerminada → Liberar ya lo hace.
            maquina.Cancelar();
        }

        public void NotificarTareaCompletada(string convId)
        {
            if (!maquinas.TryGetValue(convId, out var maquina))
            {
                Debug.LogWarning($"[Dispatcher] NotificarTareaCompletada: conv:{convId} no encontrada.");
                return;
            }

            if (maquina.EstadoActual is EjecutandoTarea estadoActivo)
            {
                var done = estadoActivo.TareaCompletada(maquina.Ctx);
                maquina.ForzarTransicion(done);
            }
            else
            {
                Debug.LogWarning($"[Dispatcher] NotificarTareaCompletada: " +
                                 $"el estado actual de conv:{convId} no es EjecutandoTarea " +
                                 $"(es {maquina.EstadoActual?.GetType().Name}). Ignorado.");
            }
        }

        public void NotificarTareaFallida(string convId)
        {
            if (!maquinas.TryGetValue(convId, out var maquina))
            {
                Debug.LogWarning($"[Dispatcher] NotificarTareaFallida: conv:{convId} no encontrada.");
                return;
            }

            if (maquina.EstadoActual is EjecutandoTarea estadoActivo)
            {
                var done = estadoActivo.TareaFallida(maquina.Ctx);
                maquina.ForzarTransicion(done);
            }
            else
            {
                Debug.LogWarning($"[Dispatcher] NotificarTareaFallida: " +
                                 $"el estado actual de conv:{convId} no es EjecutandoTarea. Ignorado.");
            }
        }
    }
}