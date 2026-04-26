using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    /// Componente pasivo: recibe y almacena mensajes entrantes.
    /// GestorComunicacion los consume cada frame llamando a ProcesarPendientes.
    ///
    /// El buzón no sabe nada del protocolo ni del cerebro: solo es una cola.
    public class BuzonMensajes : MonoBehaviour
    {
        private readonly Queue<MensajeACL> cola = new Queue<MensajeACL>();

        /// Llamado por el emisor para depositar un mensaje en este buzón.
        public void Recibir(MensajeACL mensaje)
        {
            cola.Enqueue(mensaje);
            Debug.Log($"[BuzonMensajes] {name} recibió: {mensaje}");
        }

        /// Devuelve todos los mensajes pendientes y vacía la cola.
        public IEnumerable<MensajeACL> ProcesarPendientes()
        {
            while (cola.Count > 0)
                yield return cola.Dequeue();
        }

        public bool TieneMensajes => cola.Count > 0;
    }
}
