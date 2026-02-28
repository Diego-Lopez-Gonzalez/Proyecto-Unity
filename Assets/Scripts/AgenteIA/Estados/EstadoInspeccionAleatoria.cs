using UnityEngine;

namespace GuardiaIA
{
    /// El guardia recorre los puntos de patrulla normales, pero en lugar de
    /// esperar parado en cada uno, hace una mini-inspección: se mueve a puntos
    /// aleatorios cercanos durante X segundos antes de avanzar al siguiente.
    ///
    /// Además lleva un timer global: si pasa demasiado tiempo sin ver al jugador
    /// desde que entró en este estado, decide ir a revisar la palanca de nuevo.
    public class EstadoInspeccionAleatoria : IEstado
    {
        private const float DURACION_INSPECCION_POR_PUNTO = 12f; // segundos inspeccionando cada punto
        private const float RADIO_INSPECCION              = 10f; // radio de puntos aleatorios
        private const float TIEMPO_ANTES_REVISAR_PALANCA  = 60f; // segundos totales antes de ir a la palanca

        // stado interno
        private enum Fase { MoverAlPunto, InspeccionarPunto }
        private Fase  faseActual;

        private float timerInspeccionEnPunto = 0f; // tiempo inspeccionando el punto actual
        private float timerRevisarPalanca    = 0f; // timer global desde que entró al estado

        //

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoInspeccionAleatoria] Entrar → INSPECCIÓN PATRULLA");

            timerRevisarPalanca    = 0f;
            timerInspeccionEnPunto = 0f;
            faseActual             = Fase.MoverAlPunto;

            // Ir al punto de patrulla actual
            acciones.MoverHacia(PuntoActual(bc), bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // Timer global: si lleva demasiado tiempo sin novedad, revisar palanca
            timerRevisarPalanca += Time.deltaTime;
            if (timerRevisarPalanca >= TIEMPO_ANTES_REVISAR_PALANCA)
            {
                Debug.Log("[EstadoInspeccionAleatoria] Tiempo sin novedad → revisando palanca.");
                cerebro.OnRevisarPalanca();
                cerebro.CambiarEstado(new EstadoYendoPalanca());
                return;
            }

            switch (faseActual)
            {
                case Fase.MoverAlPunto:
                    EjecutarMoverAlPunto(bc, acciones);
                    break;

                case Fase.InspeccionarPunto:
                    EjecutarInspeccion(bc, acciones, cerebro);
                    break;
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // No necesita limpieza
        }

        // Fases

        private void EjecutarMoverAlPunto(BaseConocimiento bc, Acciones acciones)
        {
            if (!acciones.HaLlegado()) return;

            // Llegó al punto: empezar inspección
            Debug.Log($"[EstadoInspeccionAleatoria] Llegado a punto {bc.IndicePatrullaActual} → inspeccionando.");
            timerInspeccionEnPunto = 0f;
            faseActual             = Fase.InspeccionarPunto;

            // Primer punto aleatorio de la inspección
            ElegirPuntoAleatorio(bc, acciones);
        }

        private void EjecutarInspeccion(BaseConocimiento bc, Acciones acciones, Cerebro cerebro)
        {
            timerInspeccionEnPunto += Time.deltaTime;

            // Si llegó al punto aleatorio, elegir otro
            if (acciones.HaLlegado())
                ElegirPuntoAleatorio(bc, acciones);

            // Inspección completada: avanzar al siguiente punto
            if (timerInspeccionEnPunto >= DURACION_INSPECCION_POR_PUNTO)
            {
                bc.IndicePatrullaActual = (bc.IndicePatrullaActual + 1) % bc.RutaPatrulla.Length;
                Debug.Log($"[EstadoInspeccionAleatoria] Inspección completa → punto {bc.IndicePatrullaActual}.");
                acciones.MoverHacia(PuntoActual(bc), bc.VelocidadPatrulla);
                faseActual = Fase.MoverAlPunto;
            }
        }

        // Utilidades

        private void ElegirPuntoAleatorio(BaseConocimiento bc, Acciones acciones)
        {
            // El punto aleatorio se genera alrededor del punto actual,
            // no de la posición del guardia, para que la inspección sea
            // coherente con el área que toca vigilar.
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