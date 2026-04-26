using UnityEngine;

namespace GuardiaIA
{
    /// El agente reacciona a un sonido percibido en tres fases:
    ///
    ///   FASE 1 — GIRAR: rota para mirar hacia el área donde escuchó el sonido.
    ///   FASE 2 — ESPERAR: parado mirando, procesando lo que oyó.
    ///   FASE 3 — MOVERSE: se desplaza hasta la posición percibida (aproximada).
    ///   FASE 4 — BUSCAR: explora aleatoriamente el área durante un tiempo limitado.
    public class EstadoInvestigarSonido : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        private enum Fase { Girar, Esperar, Moverse, Buscar }

        private Fase  faseActual;
        private float timerBusqueda;
        private float timerEspera;

        private const float DURACION_ESPERA = 1.5f;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoInvestigarSonido] Entrar → INVESTIGANDO SONIDO " +
                      $"en área: {bc.PosicionPercibidaSonido} " +
                      $"(radio incertidumbre: {bc.RadioIncertidumbreSonido:F1}m)");

            HaTerminado   = false;
            faseActual    = Fase.Girar;
            timerBusqueda = bc.TiempoBusqueda;

            acciones.Detener();
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            switch (faseActual)
            {
                case Fase.Girar:   EjecutarGiro(bc, acciones);       break;
                case Fase.Esperar: EjecutarEspera(bc, acciones);     break;
                case Fase.Moverse: EjecutarMovimiento(bc, acciones); break;
                case Fase.Buscar:  EjecutarBusqueda(bc, acciones);   break;
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones) { }

        private void EjecutarGiro(BaseConocimiento bc, Acciones acciones)
        {
            if (acciones.GirarHacia(bc.PosicionPercibidaSonido))
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

        private void EjecutarBusqueda(BaseConocimiento bc, Acciones acciones)
        {
            timerBusqueda -= Time.deltaTime;
            Debug.Log($"[EstadoInvestigarSonido] Buscando... {timerBusqueda:F1}s");

            if (timerBusqueda <= 0f)
            {
                Debug.Log("[EstadoInvestigarSonido] Tiempo agotado → señalando fin.");
                HaTerminado = true;
                return;
            }

            if (acciones.HaLlegado())
            {
                Vector3 nuevoPunto = acciones.PuntoAleatorioNavMesh(
                    bc.PosicionPercibidaSonido,
                    bc.RadioIncertidumbreSonido
                );
                acciones.MoverHacia(nuevoPunto, bc.VelocidadPatrulla);
            }
        }
    }
}