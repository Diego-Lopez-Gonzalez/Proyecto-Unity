using UnityEngine;

namespace GuardiaIA
{
    public class EstadoYendoPalanca : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        // Cuando el estado es activado por un contrato, guardamos el id
        // para liberar la conversación al salir (sea por éxito o por interrupción).
        private readonly string conversationId;

        public EstadoYendoPalanca(string conversationId = null)
        {
            this.conversationId = conversationId;
        }

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoYendoPalanca] Entrar → YENDO A PALANCA");
            HaTerminado = false;

            if (bc.Palanca == null)
            {
                Debug.LogWarning("[EstadoYendoPalanca] No hay palanca asignada.");
                // Señalamos fin: el árbitro decidirá el estado siguiente.
                HaTerminado = true;
                return;
            }

            acciones.MoverHacia(bc.Palanca.position, bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            if (!acciones.HaLlegado()) return;

            acciones.ActivarPalanca(bc.Palanca);

            // La ruta post-palanca solo se activa si este agente sabe que el objeto
            // fue robado (hecho propio). Un contratista asignado por Contract Net
            // no tiene ese hecho y debe volver a su ruta de patrulla normal.
            if (bc.ObjetoDesaparecido && bc.RutaTrasPalanca != null && bc.RutaTrasPalanca.Length > 0)
            {
                bc.RutaPatrulla         = bc.RutaTrasPalanca;
                bc.IndicePatrullaActual = 0;
            }

            cerebro.OnPalancaGestionada();
            HaTerminado = true;
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // Si fuimos activados por un contrato, liberamos la conversación
            // independientemente de si terminamos bien o fuimos interrumpidos.
            if (conversationId != null)
                cerebro.GetComponent<GestorComunicacion>()?.LiberarConversacion(conversationId);
        }
    }
}