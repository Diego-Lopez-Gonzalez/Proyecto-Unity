using UnityEngine;

namespace GuardiaIA
{
    /// El guardia se mueve a una posición de corte entre el ladrón y
    /// el punto de escape más cercano de su ruta de patrulla.
    /// La idea es bloquear la huida sin ir directamente a por el ladrón,
    /// complementando al guardia que persigue.
    ///
    /// Cuando llega al punto de corte, espera parado vigilando la zona.
    /// Si transcurre el timeout sin novedad, notifica InformDone al gestor
    /// y vuelve a patrullar.
    public class EstadoCerrarZona : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        private const float TIEMPO_VIGILANDO  = 15f; // segundos esperando en el punto de corte
        private const float DISTANCIA_CORTE   = 4f;  // cuánto delante del ladrón nos ponemos

        private float  timerVigilando = 0f;
        private string conversationId;
        private bool   estaVigilando  = false;

        public EstadoCerrarZona(string conversationId)
        {
            this.conversationId = conversationId;
        }

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoCerrarZona] Entrar → CERRANDO ZONA");
            HaTerminado   = false;
            estaVigilando = false;
            timerVigilando = 0f;

            Vector3 puntoCorte = CalcularPuntoCorte(cerebro, bc);
            acciones.MoverHacia(puntoCorte, bc.VelocidadPersecucion);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            if (!estaVigilando)
            {
                // Esperando llegar al punto de corte
                if (acciones.HaLlegado())
                {
                    Debug.Log("[EstadoCerrarZona] Llegado al punto de corte → vigilando.");
                    estaVigilando = true;
                    acciones.Detener();
                }
                return;
            }

            // Vigilando: giramos hacia la última posición conocida del ladrón
            acciones.GirarHacia(bc.UltimaPosicionJugador);

            timerVigilando += Time.deltaTime;
            if (timerVigilando >= TIEMPO_VIGILANDO)
            {
                Debug.Log("[EstadoCerrarZona] Tiempo de vigilancia agotado → notificando y saliendo.");
                cerebro.GetComponent<GestorComunicacion>()
                    ?.NotificarTareaCompletada(conversationId);
                HaTerminado = true;
            }
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            cerebro.GetComponent<GestorComunicacion>()
                ?.LiberarConversacion(conversationId);
        }

        /// Calcula un punto de corte: nos situamos entre el ladrón y el punto
        /// de patrulla más cercano a él (que sería su posible ruta de escape).
        private Vector3 CalcularPuntoCorte(Cerebro cerebro, BaseConocimiento bc)
        {
            if (bc.RutaPatrulla == null || bc.RutaPatrulla.Length == 0)
                return bc.UltimaPosicionJugador;

            // Encontrar el punto de patrulla más cercano al ladrón
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

            // Nos posicionamos entre el ladrón y ese punto de escape
            Vector3 direccion = (puntoMasCercano.position - bc.UltimaPosicionJugador).normalized;
            return bc.UltimaPosicionJugador + direccion * DISTANCIA_CORTE;
        }
    }
}
