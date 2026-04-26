using UnityEngine;

namespace GuardiaIA
{
    /// El guardia recorre los puntos de patrulla normales, pero en lugar de
    /// esperar parado en cada uno, hace una mini-inspección: se mueve a puntos
    /// aleatorios cercanos durante X segundos antes de avanzar al siguiente.
    ///
    /// Estado base secundario: no termina por señales externas, pero sí tiene
    /// una condición interna de salida: si pasa demasiado tiempo sin novedad,
    /// decide revisar la palanca de nuevo poniendo HaTerminado = true.
    /// Antes de hacerlo escribe PalancaYaGestionada = false en el conocimiento,
    /// para que EvaluarPrioridades llegue a P2 (YendoPalanca) en lugar de P3.
    public class EstadoInspeccionAleatoria : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        private const float DURACION_INSPECCION_POR_PUNTO = 12f;
        private const float RADIO_INSPECCION              = 10f;
        private const float TIEMPO_ANTES_REVISAR_PALANCA  = 60f;

        private enum Fase { MoverAlPunto, InspeccionarPunto }
        private Fase  faseActual;

        private float timerInspeccionEnPunto = 0f;
        private float timerRevisarPalanca    = 0f;

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoInspeccionAleatoria] Entrar → INSPECCIÓN PATRULLA");
            HaTerminado            = false;
            timerRevisarPalanca    = 0f;
            timerInspeccionEnPunto = 0f;
            faseActual             = Fase.MoverAlPunto;

            acciones.MoverHacia(PuntoActual(bc), bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            timerRevisarPalanca += Time.deltaTime;
            if (timerRevisarPalanca >= TIEMPO_ANTES_REVISAR_PALANCA)
            {
                Debug.Log("[EstadoInspeccionAleatoria] Tiempo sin novedad → revisando palanca.");

                // Escribimos el hecho ANTES de señalar HaTerminado,
                // para que EvaluarPrioridades lo encuentre ya actualizado.
                cerebro.OnRevisarPalanca();   // PalancaYaGestionada = false
                HaTerminado = true;
                return;
            }

            switch (faseActual)
            {
                case Fase.MoverAlPunto:
                    EjecutarMoverAlPunto(bc, acciones);
                    break;
                case Fase.InspeccionarPunto:
                    EjecutarInspeccion(bc, acciones);
                    break;
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones) { }

        private void EjecutarMoverAlPunto(BaseConocimiento bc, Acciones acciones)
        {
            if (!acciones.HaLlegado()) return;

            Debug.Log($"[EstadoInspeccionAleatoria] Llegado a punto {bc.IndicePatrullaActual} → inspeccionando.");
            timerInspeccionEnPunto = 0f;
            faseActual             = Fase.InspeccionarPunto;
            ElegirPuntoAleatorio(bc, acciones);
        }

        private void EjecutarInspeccion(BaseConocimiento bc, Acciones acciones)
        {
            timerInspeccionEnPunto += Time.deltaTime;

            if (acciones.HaLlegado())
                ElegirPuntoAleatorio(bc, acciones);

            if (timerInspeccionEnPunto >= DURACION_INSPECCION_POR_PUNTO)
            {
                bc.IndicePatrullaActual = (bc.IndicePatrullaActual + 1) % bc.RutaPatrulla.Length;
                Debug.Log($"[EstadoInspeccionAleatoria] Inspección completa → punto {bc.IndicePatrullaActual}.");
                acciones.MoverHacia(PuntoActual(bc), bc.VelocidadPatrulla);
                faseActual = Fase.MoverAlPunto;
            }
        }

        private void ElegirPuntoAleatorio(BaseConocimiento bc, Acciones acciones)
        {
            Vector3 punto = acciones.PuntoAleatorioNavMesh(
                bc.RutaPatrulla[bc.IndicePatrullaActual].position,
                RADIO_INSPECCION
            );
            acciones.MoverHacia(punto, bc.VelocidadPatrulla);
        }

        private Vector3 PuntoActual(BaseConocimiento bc)
            => bc.RutaPatrulla[bc.IndicePatrullaActual].position;
    }
}