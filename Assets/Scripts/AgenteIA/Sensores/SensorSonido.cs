using UnityEngine;

namespace GuardiaIA
{
    // Detecta FuenteSonido activas dentro de un radio esférico.

    public class SensorSonido : MonoBehaviour
    {
        private float   radioEscucha;       // hasta dónde puede oír el guardia
        private float   radioIncertidumbre; // cuánto se desvía la posición percibida
        private Cerebro cerebro;

        // Control interno
        private bool activo = true;

        public void Inicializar(Cerebro cerebro, float radioEscucha, float radioIncertidumbre)
        {
            this.cerebro             = cerebro;
            this.radioEscucha        = radioEscucha;
            this.radioIncertidumbre  = radioIncertidumbre;
        }

        public void Activar()   => activo = true;
        public void Desactivar() => activo = false;

        //  BUCLE PRINCIPAL

        // Se usa LateUpdate en lugar de Update para garantizar que el sensor
        // siempre se ejecuta DESPUÉS de que cualquier FuenteSonido haya llamado
        // a Emitir() en su Update.
        private void LateUpdate()
        {
            if (!activo || cerebro == null) return;

            ComprobarSonidos();
        }

        //  DETECCIÓN

        private void ComprobarSonidos()
        {
            Collider[] colisiones = Physics.OverlapSphere(transform.position, radioEscucha);

            // Buscamos la fuente de sonido activa más cercana
            FuenteSonido fuenteMasCercana  = null;
            float        distanciaMinima   = float.MaxValue;

            foreach (Collider col in colisiones)
            {
                FuenteSonido fuente = col.GetComponent<FuenteSonido>();
                if (fuente == null || !fuente.EstaActiva) continue;

                float distancia = Vector3.Distance(transform.position, fuente.transform.position);

                // Comprobamos también que la intensidad de la fuente
                if (distancia > fuente.Intensidad) continue;

                if (distancia < distanciaMinima)
                {
                    distanciaMinima  = distancia;
                    fuenteMasCercana = fuente;
                }
            }

            if (fuenteMasCercana == null) return;

            // Reseteamos el flag ANTES de notificar para que no se detecte
            // dos veces si el cerebro tarda más de un frame en procesar
            fuenteMasCercana.EstaActiva = false;

            // Calculamos el área aproximada: posición real + desplazamiento aleatorio
            // El desplazamiento es proporcional a la distancia
            float factorImprecision   = distanciaMinima / radioEscucha;
            float radioDesvio         = radioIncertidumbre * factorImprecision;

            Vector3 posicionReal      = fuenteMasCercana.transform.position;
            Vector3 posicionPercibida = AplicarIncertidumbre(posicionReal, radioDesvio);

            cerebro.OnSonidoDetectado(posicionPercibida, radioDesvio);
        }

        //  UTILIDADES PRIVADAS

        // Desplaza la posición origen dentro de un círculo aleatorio
        // para simular la imprecisión auditiva del agente.
        private Vector3 AplicarIncertidumbre(Vector3 origen, float radio)
        {
            if (radio <= 0f) return origen;

            Vector2 circulo = Random.insideUnitCircle * radio;
            return origen + new Vector3(circulo.x, 0f, circulo.y);
        }

        //  GIZMOS

        private void OnDrawGizmos()
        {
            // Radio de escucha en verde transparente
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, radioEscucha);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radioEscucha);
        }
    }
}