using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public class EstadoBarrerMapa : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        private readonly string conversationId;
        private bool tareaNotificada = false;
        private const float TIEMPO_MAX = 60f;
        private float timer = 0f;
        private int indicePunto = 0;

        public EstadoBarrerMapa(string conversationId = null)
        {
            this.conversationId = conversationId;
        }

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            timer           = 0f;
            indicePunto     = 0;
            tareaNotificada = false;
            HaTerminado     = false;

            if (bc.PuntosBarrido == null || bc.PuntosBarrido.Count == 0)
            {
                Debug.LogWarning("[EstadoBarrerMapa] No hay puntos de barrido asignados.");
                HaTerminado = true;
                return;
            }

            acciones.MoverHacia(bc.PuntosBarrido[0].position, bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            timer += Time.deltaTime;

            if (timer >= TIEMPO_MAX)
            {
                Debug.Log("[EstadoBarrerMapa] Tiempo agotado → notificando.");
                Completar(cerebro);
                return;
            }

            if (!acciones.HaLlegado()) return;

            indicePunto++;

            if (indicePunto >= bc.PuntosBarrido.Count)
            {
                Debug.Log("[EstadoBarrerMapa] Barrido completado → notificando.");
                Completar(cerebro);
                return;
            }

            acciones.MoverHacia(bc.PuntosBarrido[indicePunto].position, bc.VelocidadPatrulla);
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            if (conversationId != null && !tareaNotificada)
                cerebro.GetComponent<GestorComunicacion>()
                    ?.LiberarConversacion(conversationId);
        }

        private void Completar(Cerebro cerebro)
        {
            tareaNotificada = true;
            cerebro.GetComponent<GestorComunicacion>()
                ?.NotificarTareaCompletada(conversationId);
            HaTerminado = true;
        }
    }
}