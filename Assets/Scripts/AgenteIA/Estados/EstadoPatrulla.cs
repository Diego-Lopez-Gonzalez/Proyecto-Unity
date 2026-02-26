using UnityEngine;

namespace GuardiaIA
{
    /// El agente recorre en bucle los puntos de patrulla definidos en la
    /// BaseConocimiento, esperando un tiempo configurable en cada punto.
    
     public class EstadoPatrulla : IEstado
    {
        // Estado interno del comportamiento
        private float tiempoEnPunto    = 0f;
        private bool  esperandoEnPunto = false;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoPatrulla] Entrar → PATRULLANDO");
            esperandoEnPunto = false;
            tiempoEnPunto    = 0f;

            if (bc.RutaPatrulla == null || bc.RutaPatrulla.Length == 0)
            {
                Debug.LogWarning("[EstadoPatrulla] La ruta de patrulla está vacía.");
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
                // Acaba de llegar: iniciar espera
                esperandoEnPunto = true;
                tiempoEnPunto    = 0f;
                return;
            }

            // Acumulamos tiempo de espera
            tiempoEnPunto += Time.deltaTime;

            if (tiempoEnPunto >= bc.TiempoEsperaEnPunto)
            {
                // Avanzar al siguiente punto en bucle
                bc.IndicePatrullaActual = (bc.IndicePatrullaActual + 1) % bc.RutaPatrulla.Length;
                acciones.MoverHacia(PuntoActual(bc), bc.VelocidadPatrulla);
                esperandoEnPunto = false;
                tiempoEnPunto    = 0f;
            }
        }


        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // No necesita limpiar nada
        }

        //  UTILIDAD PRIVADA

        private Vector3 PuntoActual(BaseConocimiento bc)
            => bc.RutaPatrulla[bc.IndicePatrullaActual].position;
    }
}