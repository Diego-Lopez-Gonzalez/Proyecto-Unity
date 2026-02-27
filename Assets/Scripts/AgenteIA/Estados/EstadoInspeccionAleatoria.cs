using UnityEngine;

namespace GuardiaIA
{
    public class EstadoInspeccionAleatoria : IEstado
    {
        private float tiempoInspeccion = 0f;
        private float duracionMaxima   = 8f;

        private int puntosVisitados = 0;
        private int maxPuntos       = 3;

        private Vector3 destinoActual;
        private bool moviendose = false;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoInspeccionAleatoria] Entrar → INSPECCIÓN");

            tiempoInspeccion = 0f;
            puntosVisitados  = 0;

            ElegirNuevoDestino(cerebro, acciones);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            tiempoInspeccion += Time.deltaTime;

            if (moviendose)
            {
                if (acciones.HaLlegado())
                {
                    moviendose = false;
                    puntosVisitados++;
                }
            }
            else
            {
                if (puntosVisitados < maxPuntos)
                {
                    ElegirNuevoDestino(cerebro, acciones);
                }
                else
                {
                    cerebro.CambiarEstado(new EstadoPatrulla());
                    return;
                }
            }

            if (tiempoInspeccion >= duracionMaxima)
            {
                cerebro.CambiarEstado(new EstadoPatrulla());
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // No necesita limpieza especial
        }

        private void ElegirNuevoDestino(Cerebro cerebro, Acciones acciones)
        {
            destinoActual = acciones.PuntoAleatorioNavMesh(
                cerebro.transform.position,
                15f
            );

            acciones.MoverHacia(destinoActual, 3.5f);
            moviendose = true;
        }
    }
}