using UnityEngine;

namespace GuardiaIA
{
    public class EstadoBusqueda : IEstado
    {
        private float timerActual = 0f;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoBusqueda] Entrar → BUSCANDO");
            timerActual = bc.TiempoBusqueda;
            acciones.MoverHacia(bc.UltimaPosicionJugador, bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            timerActual -= Time.deltaTime;
            Debug.Log($"[EstadoBusqueda] Timer restante: {timerActual:F1}s");

            // Tiempo agotado: volver a patrullar
            if (timerActual <= 0f)
            {
                Debug.Log("[EstadoBusqueda] Tiempo agotado → volviendo a patrullar.");
                cerebro.CambiarEstado(new EstadoPatrulla());
                return;
            }

            // Si llegó al punto actual, elegir uno nuevo aleatorio
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


        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // No necesita limpiar nada
        }
    }
}