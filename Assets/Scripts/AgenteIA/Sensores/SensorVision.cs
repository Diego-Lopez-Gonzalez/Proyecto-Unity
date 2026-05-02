using UnityEngine;

namespace GuardiaIA
{
    public class SensorVision : MonoBehaviour
    {
        private Transform  jugador;
        private Transform  objetoVigilado;
        private float      rangoVision;
        private float      anguloVision;
        private LayerMask  capasObstaculo;
        private IAgente    agente;                        // antes: Cerebro cerebro
        private bool jugadorVisibleAnterior  = false;

        // Para el objeto vigilado:
        private bool    objetoConfirmado     = false;
        private bool    objetoYaNotificado   = false;
        private Vector3 posicionMemorizada;

        // Gizmos
        private float  _rangoGizmo;
        private float  _anguloGizmo;


        // El agente propietario llama a este método en su Start().
        // A partir de aquí el sensor trabaja de forma autónoma.
        public void Inicializar(
            IAgente    agente,                            // antes: Cerebro cerebro
            Transform  jugador,
            Transform  objetoVigilado,
            float      rangoVision,
            float      anguloVision,
            LayerMask  capasObstaculo)
        {
            this.agente         = agente;
            this.jugador        = jugador;
            this.objetoVigilado = objetoVigilado;
            this.rangoVision    = rangoVision;
            this.anguloVision   = anguloVision;
            this.capasObstaculo = capasObstaculo;

            _rangoGizmo  = rangoVision;
            _anguloGizmo = anguloVision;
        }

        //  BUCLE PRINCIPAL

        private void Update()
        {
            if (agente == null) return;

            ComprobarJugador();
            ComprobarObjetoVigilado();
        }

        //  DETECCIÓN DEL JUGADOR

        private void ComprobarJugador()
        {
            if (jugador == null) return;

            bool visibleAhora = EstaEnCono(jugador.position);

            if (visibleAhora)
            {
                if (!jugadorVisibleAnterior)
                    agente.OnJugadorDetectado(jugador.position);
                else
                    agente.OnActualizarPosicionJugador(jugador.position);
            }
            else if (jugadorVisibleAnterior)
            {
                agente.OnJugadorPerdido();
            }

            jugadorVisibleAnterior = visibleAhora;
        }

        //  VIGILANCIA DEL OBJETO

        private void ComprobarObjetoVigilado()
        {
            // Si ya notificamos la desaparición no hacemos nada más
            if (objetoYaNotificado) return;

            // Sin confirmación previa no podemos saber si desapareció
            if (!objetoConfirmado)
            {
                if (objetoVigilado != null
                    && objetoVigilado.gameObject.activeInHierarchy
                    && EstaEnCono(objetoVigilado.position))
                {
                    objetoConfirmado   = true;
                    posicionMemorizada = objetoVigilado.position;
                }
                return;
            }

            // Si sigue visible y activo: actualizamos la posición memorizada
            // (por si el objeto puede moverse levemente).
            if (objetoVigilado != null
                && objetoVigilado.gameObject.activeInHierarchy
                && EstaEnCono(objetoVigilado.position))
            {
                posicionMemorizada = objetoVigilado.position;
                return;
            }

            // El objeto NO está visible ahora. Comprobamos si estamos mirando
            // hacia donde estaba.
            // Concluimos que ha desaparecido
            // si simplemente miramos para otro lado, no podemos saberlo todavía.
            if (EstaEnCono(posicionMemorizada))
            {
                agente.OnObjetoDesaparecido();
                objetoYaNotificado = true;
            }
        }

        //  UTILIDADES PRIVADAS

        // Devuelve true si la posición dada está dentro del cono de visión
        private bool EstaEnCono(Vector3 posicionObjetivo)
        {
            Vector3 origen    = transform.position + Vector3.up * 1.5f;
            Vector3 direccion = posicionObjetivo - transform.position;
            float   distancia = direccion.magnitude;

            if (distancia > rangoVision) return false;
            if (Vector3.Angle(transform.forward, direccion) > anguloVision * 0.5f) return false;
            if (Physics.Raycast(origen, direccion.normalized, distancia, capasObstaculo)) return false;

            return true;
        }

        //  GIZMOS

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            float semi = _anguloGizmo * 0.5f;
            Vector3 o  = transform.position + Vector3.up * 1.5f;

            Gizmos.DrawRay(o, Quaternion.Euler(0,  semi, 0) * transform.forward * _rangoGizmo);
            Gizmos.DrawRay(o, Quaternion.Euler(0, -semi, 0) * transform.forward * _rangoGizmo);
            Gizmos.DrawRay(o, transform.forward * _rangoGizmo);
        }
    }
}