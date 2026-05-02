using UnityEngine;

namespace GuardiaIA
{
    public class EstadoYendoPalanca : IEstado
    {
        public bool HaTerminado { get; private set; } = false;

        private readonly string conversationId;
        private bool tareaNotificada = false; // evita doble liberación

        public EstadoYendoPalanca(string conversationId = null)
        {
            this.conversationId = conversationId;
        }

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoYendoPalanca] Entrar → YENDO A PALANCA");
            HaTerminado      = false;
            tareaNotificada  = false;

            if (bc.Palanca == null)
            {
                Debug.LogWarning("[EstadoYendoPalanca] No hay palanca asignada.");
                HaTerminado = true;
                return;
            }

            acciones.MoverHacia(bc.Palanca.position, bc.VelocidadPatrulla);
        }

        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            if (!acciones.HaLlegado()) return;

            acciones.ActivarPalanca(bc.Palanca);

            if (bc.ObjetoDesaparecido && bc.RutaTrasPalanca != null && bc.RutaTrasPalanca.Length > 0)
            {
                bc.RutaPatrulla         = bc.RutaTrasPalanca;
                bc.IndicePatrullaActual = 0;
            }

            cerebro.OnPalancaGestionada();

            // FIX: notificar InformDone al gestor para que EsperandoResultados cierre correctamente.
            // El flujo Done → OnConversacionTerminada → Liberar se encarga de limpiar ambos lados.
            if (conversationId != null)
            {
                tareaNotificada = true;
                cerebro.GetComponent<GestorComunicacion>()?.NotificarTareaCompletada(conversationId);
            }

            HaTerminado = true;
        }

        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // FIX: solo liberamos si fuimos interrumpidos (no notificamos la tarea).
            // Si tareaNotificada == true, el flujo Done ya llamará Liberar.
            if (conversationId != null && !tareaNotificada)
                cerebro.GetComponent<GestorComunicacion>()?.LiberarConversacion(conversationId);
        }
    }
}