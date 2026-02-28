using UnityEngine;

namespace GuardiaIA
{
    public class EstadoYendoPalanca : IEstado
    {

        public void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            Debug.Log("[EstadoYendoPalanca] Entrar → YENDO A PALANCA");

            if (bc.Palanca == null)
            {
                Debug.LogWarning("[EstadoYendoPalanca] No hay palanca asignada. Volviendo a patrullar.");
                cerebro.CambiarEstado(new EstadoPatrulla());
                return;
            }

            acciones.MoverHacia(bc.Palanca.position, bc.VelocidadPatrulla);
        }


        public void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            // Esperamos a llegar
            if (!acciones.HaLlegado()) return;

            // Ha llegado: activar la palanca
            acciones.ActivarPalanca(bc.Palanca);

            // Si hay una nueva ruta, la aplicamos en la base de conocimiento
            if (bc.RutaTrasPalanca != null && bc.RutaTrasPalanca.Length > 0)
            {
                Debug.Log("[EstadoYendoPalanca] Cambiando ruta de patrulla.");
                bc.RutaPatrulla         = bc.RutaTrasPalanca;
                bc.IndicePatrullaActual = 0;
            }

            // Secuencia completada: volver a patrullar
            cerebro.CambiarEstado(new EstadoPatrulla());
        }


        public void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones)
        {
            cerebro.OnPalancaGestionada();
        }
    }
}