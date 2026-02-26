using UnityEngine;

namespace GuardiaIA
{
    // Detecta todo lo que el agente percibe por proximidad física,
    public class SensorTacto : MonoBehaviour
    {
        private Transform jugador;
        private float     radioCaptura;
        private float     rangoDeteccionPuertas;
        private Cerebro   cerebro;

        private bool capturaActiva = false;


        public void Inicializar(
            Cerebro   cerebro,
            Transform jugador,
            float     radioCaptura,
            float     rangoDeteccionPuertas)
        {
            this.cerebro               = cerebro;
            this.jugador               = jugador;
            this.radioCaptura          = radioCaptura;
            this.rangoDeteccionPuertas = rangoDeteccionPuertas;
        }

        // Activa la comprobación de captura. Llamado por el Cerebro
        // al entrar en EstadoPersecucion.
        public void ActivarCaptura()
        {
            capturaActiva = true;
        }

        // Desactiva la comprobación de captura. Llamado por el Cerebro
        // al salir de EstadoPersecucion.
        public void DesactivarCaptura()
        {
            capturaActiva = false;
        }

        //  BUCLE PRINCIPAL

        private void Update()
        {
            if (cerebro == null) return;

            ComprobarCaptura();
            ComprobarPuertas();
        }

        //  CAPTURA

        private void ComprobarCaptura()
        {
            if (!capturaActiva || jugador == null) return;

            if (Vector3.Distance(transform.position, jugador.position) <= radioCaptura)
                cerebro.OnJugadorCapturado();
        }

        //  PUERTAS

        private void ComprobarPuertas()
        {
            Collider[] cercanos = Physics.OverlapSphere(transform.position, rangoDeteccionPuertas);

            foreach (Collider col in cercanos)
            {
                PuertaNavMesh puerta = col.GetComponent<PuertaNavMesh>();
                if (puerta != null && !puerta.EstaAbierta())
                    cerebro.OnPuertaDetectada(puerta);
            }
        }

        //  GIZMOS

        private void OnDrawGizmos()
        {
            // Radio de captura en rojo
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radioCaptura);

            // Radio de puertas en azul
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, rangoDeteccionPuertas);
        }
    }
}