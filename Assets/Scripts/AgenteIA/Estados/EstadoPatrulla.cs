using UnityEngine;

namespace GuardiaIA
{
    /// El agente recorre en bucle los puntos de patrulla definidos en la
    /// BaseConocimiento, esperando un tiempo configurable en cada punto.
    /// Es el estado base: nunca termina por sí solo, espera ser subsumido.
    public class EstadoPatrulla : IEstado
    {
        // Estado base: nunca termina por iniciativa propia.
        public bool HaTerminado => false;

        private float tiempoEnPunto    = 0f;
        private bool  esperandoEnPunto = false;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            esperandoEnPunto = false;
            tiempoEnPunto    = 0f;

            if (bc.RutaPatrulla == null || bc.RutaPatrulla.Length == 0)
            {
                return;
            }

            acciones.MoverHacia(PuntoActual(bc), bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            if (bc.RutaPatrulla == null || bc.RutaPatrulla.Length == 0) return;
            if (!acciones.HaLlegado()) return;

            if (!esperandoEnPunto)
            {
                esperandoEnPunto = true;
                tiempoEnPunto    = 0f;
                return;
            }

            tiempoEnPunto += Time.deltaTime;

            if (tiempoEnPunto >= bc.TiempoEsperaEnPunto)
            {
                bc.IndicePatrullaActual = (bc.IndicePatrullaActual + 1) % bc.RutaPatrulla.Length;
                acciones.MoverHacia(PuntoActual(bc), bc.VelocidadPatrulla);
                esperandoEnPunto = false;
                tiempoEnPunto    = 0f;
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones) { }

        private Vector3 PuntoActual(BaseConocimiento bc)
            => bc.RutaPatrulla[bc.IndicePatrullaActual].position;
    }
}