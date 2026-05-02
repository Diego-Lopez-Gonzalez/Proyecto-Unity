using UnityEngine;

namespace GuardiaIA
{
    public class EstadoBusqueda : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        private float timerActual = 0f;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            HaTerminado = false;
            timerActual = bc.TiempoBusqueda;
            acciones.MoverHacia(bc.UltimaPosicionJugador, bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            timerActual -= Time.deltaTime;

            if (timerActual <= 0f)
            {
                // No elige sucesor: avisa al árbitro y este decide.
                HaTerminado = true;
                return;
            }

            if (acciones.HaLlegado())
            {
                Vector3 nuevoPunto = acciones.PuntoAleatorioNavMesh(
                    bc.UltimaPosicionJugador,
                    bc.RadioBusqueda
                );
                acciones.MoverHacia(nuevoPunto, bc.VelocidadPatrulla);
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones) { }
    }
}