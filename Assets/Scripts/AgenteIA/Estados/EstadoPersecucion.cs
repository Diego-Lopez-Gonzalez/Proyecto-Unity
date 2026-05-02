using UnityEngine;

namespace GuardiaIA
{
    /// El agente persigue al jugador usando la última posición conocida,
    /// que el SensorVision actualiza en tiempo real a través del Cerebro.
    /// Estado reactivo: no termina por sí solo, espera a que JugadorVisible
    /// pase a false (OnJugadorPerdido) para ser desalojado por el árbitro.
    public class EstadoPersecucion : IEstado
    {
        // El árbitro lo desaloja cuando JugadorVisible pasa a false.
        public bool HaTerminado => false;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            cerebro.SensorTacto.ActivarCaptura();
            acciones.MoverHacia(bc.UltimaPosicionJugador, bc.VelocidadPersecucion);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // La posición del jugador se actualiza en bc por el SensorVision
            // a través de Cerebro.OnActualizarPosicionJugador().
            acciones.MoverHacia(bc.UltimaPosicionJugador, bc.VelocidadPersecucion);
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            cerebro.SensorTacto.DesactivarCaptura();
        }
    }
}