using UnityEngine;

namespace GuardiaIA
{
    /// El agente reacciona a un sonido percibido en tres fases:
    ///
    ///   FASE 1 — GIRAR: rota para mirar hacia el área donde escuchó el sonido.
    ///   FASE 2 — MOVERSE: se desplaza hasta la posición percibida (aproximada).
    ///   FASE 3 — BUSCAR: explora aleatoriamente el área durante un tiempo limitado.

    public class EstadoInvestigarSonido : IEstado
    {
        private enum Fase { Girar, Esperar, Moverse, Buscar }

        private Fase  faseActual;
        private float timerBusqueda;
        private float timerEspera;

        [Header("Parámetros internos")]
        private const float DURACION_ESPERA = 1.5f; // segundos parado mirando tras el giro


        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoInvestigarSonido] Entrar → INVESTIGANDO SONIDO " +
                      $"en área: {bc.PosicionPercibidaSonido} " +
                      $"(radio incertidumbre: {bc.RadioIncertidumbreSonido:F1}m)");

            faseActual    = Fase.Girar;
            timerBusqueda = bc.TiempoBusqueda;

            // Detenemos el movimiento mientras giramos
            acciones.Detener();
        }


        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            switch (faseActual)
            {
                case Fase.Girar:   EjecutarGiro(bc, acciones);              break;
                case Fase.Esperar: EjecutarEspera(bc, acciones);            break;
                case Fase.Moverse: EjecutarMovimiento(bc, acciones);        break;
                case Fase.Buscar:  EjecutarBusqueda(cerebro, bc, acciones); break;
            }
        }


        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // No necesita limpiar nada
        }


        private void EjecutarGiro(BaseConocimiento bc, Acciones acciones)
        {
            bool giroCompletado = acciones.GirarHacia(bc.PosicionPercibidaSonido);

            if (giroCompletado)
            {
                Debug.Log("[EstadoInvestigarSonido] Giro completado → ESPERANDO");
                timerEspera = DURACION_ESPERA;
                faseActual  = Fase.Esperar;
            }
        }

        private void EjecutarEspera(BaseConocimiento bc, Acciones acciones)
        {
            timerEspera -= Time.deltaTime;
            Debug.Log($"[EstadoInvestigarSonido] Mirando hacia el sonido... {timerEspera:F1}s");

            if (timerEspera <= 0f)
            {
                Debug.Log("[EstadoInvestigarSonido] Espera completada → MOVERSE");
                faseActual = Fase.Moverse;
                acciones.MoverHacia(bc.PosicionPercibidaSonido, bc.VelocidadPatrulla);
            }
        }

        private void EjecutarMovimiento(BaseConocimiento bc, Acciones acciones)
        {
            if (acciones.HaLlegado())
            {
                Debug.Log("[EstadoInvestigarSonido] Llegado al área → BUSCAR");
                faseActual = Fase.Buscar;
            }
        }

        private void EjecutarBusqueda(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            timerBusqueda -= Time.deltaTime;
            Debug.Log($"[EstadoInvestigarSonido] Buscando... {timerBusqueda:F1}s");

            if (timerBusqueda <= 0f)
            {
                Debug.Log("[EstadoInvestigarSonido] Tiempo agotado → PATRULLANDO");
                cerebro.CambiarEstado(new EstadoPatrulla());
                return;
            }

            if (acciones.HaLlegado())
            {
                // El radio de exploración es el de incertidumbre del sonido:
                // buscamos exactamente en el área que "creímos" escuchar
                Vector3 nuevoPunto = acciones.PuntoAleatorioNavMesh(
                    bc.PosicionPercibidaSonido,
                    bc.RadioIncertidumbreSonido
                );
                acciones.MoverHacia(nuevoPunto, bc.VelocidadPatrulla);
            }
        }
    }
}