using UnityEngine;

namespace GuardiaIA
{
    public class EstadoBusqueda : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        private float timerActual = 0f;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoBusqueda] Entrar → BUSCANDO");
            HaTerminado = false;
            timerActual = bc.TiempoBusqueda;
            acciones.MoverHacia(bc.UltimaPosicionJugador, bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            timerActual -= Time.deltaTime;
            Debug.Log($"[EstadoBusqueda] Timer restante: {timerActual:F1}s");

            if (timerActual <= 0f)
            {
                Debug.Log("[EstadoBusqueda] Tiempo agotado → señalando fin.");
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
                Debug.Log($"[EstadoBusqueda] Explorando nuevo punto: {nuevoPunto}");
                acciones.MoverHacia(nuevoPunto, bc.VelocidadPatrulla);
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones) { }
    }
}