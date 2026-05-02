using UnityEngine;

namespace GuardiaIA
{
    /// El guardia se mueve al punto de cierre más estratégico disponible en
    /// BaseConocimiento.PuntosCorte (lista editable desde el Inspector de Cerebro).
    ///
    /// Criterio de selección: minimizar (distJugador - distGuardia).
    ///   · Valor negativo  → el guardia llega antes que el jugador: ideal.
    ///   · Valor positivo  → el jugador llegaría antes: se elige el menos malo.
    ///
    /// Si la lista está vacía usa el fallback original (interpolación entre
    /// jugador y punto de patrulla más cercano).
    public class EstadoCerrarZona : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        private const float TIEMPO_VIGILANDO = 15f;

        private float  timerVigilando  = 0f;
        private string conversationId;
        private bool   estaVigilando   = false;
        private bool   tareaNotificada = false;

        public EstadoCerrarZona(string conversationId)
        {
            this.conversationId = conversationId;
        }

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoCerrarZona] Entrar → CERRANDO ZONA");
            HaTerminado     = false;
            estaVigilando   = false;
            tareaNotificada = false;
            timerVigilando  = 0f;

            Vector3 destino = ElegirPuntoCorte(cerebro.transform.position, bc);
            acciones.MoverHacia(destino, bc.VelocidadPersecucion);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            if (!estaVigilando)
            {
                if (acciones.HaLlegado())
                {
                    Debug.Log("[EstadoCerrarZona] Llegado al punto de corte → vigilando.");
                    estaVigilando = true;
                    acciones.Detener();
                }
                return;
            }

            acciones.GirarHacia(bc.UltimaPosicionJugador);

            timerVigilando += Time.deltaTime;
            if (timerVigilando >= TIEMPO_VIGILANDO)
            {
                Debug.Log("[EstadoCerrarZona] Tiempo de vigilancia agotado → notificando y saliendo.");
                tareaNotificada = true;
                cerebro.GetComponent<GestorComunicacion>()
                    ?.NotificarTareaCompletada(conversationId);
                HaTerminado = true;
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            if (conversationId != null && !tareaNotificada)
                cerebro.GetComponent<GestorComunicacion>()
                    ?.LiberarConversacion(conversationId);
        }

        // ── Selección del punto ───────────────────────────────────────────────

        private Vector3 ElegirPuntoCorte(Vector3 posGuardia, BaseConocimiento bc)
        {
            if (bc.PuntosCorte == null || bc.PuntosCorte.Count == 0)
                return FallbackPuntoCorte(bc);

            Transform mejorPunto   = null;
            float     mejorVentaja = float.MaxValue;

            foreach (var punto in bc.PuntosCorte)
            {
                if (punto == null) continue;

                float distJugador = Vector3.Distance(bc.UltimaPosicionJugador, punto.position);
                float distGuardia = Vector3.Distance(posGuardia,               punto.position);
                float ventaja     = distJugador - distGuardia; // negativo = guardia llega antes

                if (ventaja < mejorVentaja)
                {
                    mejorVentaja = ventaja;
                    mejorPunto   = punto;
                }
            }

            if (mejorPunto == null)
                return FallbackPuntoCorte(bc);

            Debug.Log($"[EstadoCerrarZona] Punto elegido: {mejorPunto.name} " +
                      $"(ventaja: {mejorVentaja:F1}m)");

            return mejorPunto.position;
        }

        /// Fallback: interpola entre el jugador y el punto de patrulla más cercano a él.
        private Vector3 FallbackPuntoCorte(BaseConocimiento bc)
        {
            const float DISTANCIA_CORTE = 4f;

            if (bc.RutaPatrulla == null || bc.RutaPatrulla.Length == 0)
                return bc.UltimaPosicionJugador;

            Transform puntoMasCercano = bc.RutaPatrulla[0];
            float     distanciaMin    = float.MaxValue;

            foreach (var punto in bc.RutaPatrulla)
            {
                float d = Vector3.Distance(bc.UltimaPosicionJugador, punto.position);
                if (d < distanciaMin)
                {
                    distanciaMin    = d;
                    puntoMasCercano = punto;
                }
            }

            Vector3 direccion = (puntoMasCercano.position - bc.UltimaPosicionJugador).normalized;
            return bc.UltimaPosicionJugador + direccion * DISTANCIA_CORTE;
        }
    }
}