using UnityEngine;
using UnityEngine.AI;

namespace GuardiaIA
{
    public class Acciones : MonoBehaviour
    {
        private NavMeshAgent navAgent;

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
        }

        //
        //  MOVIMIENTO
        //

        // Mueve al agente hacia el destino a la velocidad indicada
        public void MoverHacia(Vector3 destino, float velocidad)
        {
            navAgent.speed = velocidad;
            navAgent.SetDestination(destino);
        }

        /// Detiene al agente limpiando su camino actual
        public void Detener()
        {
            navAgent.ResetPath();
        }

        // Devuelve true cuando el agente ha alcanzado su destino actual.
        public bool HaLlegado()
        {
            return !navAgent.pathPending
                && navAgent.remainingDistance <= navAgent.stoppingDistance;
        }

        // Genera un punto aleatorio válido en el NavMesh dentro del radio
        // indicado alrededor del centro. Devuelve el centro si no encuentra
        // ningún punto válido.
        public Vector3 PuntoAleatorioNavMesh(Vector3 centro, float radio)
        {
            Vector2 circulo    = Random.insideUnitCircle * radio;
            Vector3 candidato  = centro + new Vector3(circulo.x, 0f, circulo.y);

            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, radio, NavMesh.AllAreas))
                return hit.position;

            return centro;
        }

        // Rota al agente hacia la posición indicada.
        // Devuelve true cuando el giro ha acabado.
        public bool GirarHacia(Vector3 posicionObjetivo, float velocidadGiro = 5f, float umbralGrados = 2f)
        {
            Vector3 direccion = (posicionObjetivo - transform.position).normalized;
            direccion.y = 0f; // solo rotación en el plano horizontal

            if (direccion == Vector3.zero) return true;

            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacionObjetivo,
                velocidadGiro * Time.deltaTime
            );

            return Quaternion.Angle(transform.rotation, rotacionObjetivo) < umbralGrados;
        }



        // Activa la palanca en el Transform indicado si todavía no está activada.
        public void ActivarPalanca(Transform palancaTransform)
        {
            if (palancaTransform == null)
            {
                Debug.LogWarning("[Acciones] ActivarPalanca: el Transform es nulo.");
                return;
            }

            Palanca palanca = palancaTransform.GetComponent<Palanca>();

            if (palanca == null)
            {
                Debug.LogWarning("[Acciones] ActivarPalanca: el objeto no tiene componente Palanca.");
                return;
            }

            if (!palanca.EstaActivada())
            {
                palanca.Activar();
                Debug.Log("[Acciones] Palanca activada.");
            }
            else
            {
                Debug.Log("[Acciones] La palanca ya estaba activada.");
            }
        }
    }
}