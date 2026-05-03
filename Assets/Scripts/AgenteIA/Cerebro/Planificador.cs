using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public enum EventoSeguridad
    {
        JugadorDetectado,
        ObjetoRobado,
    }

    public class EstadoMundo
    {
        public bool LadronVisible   { get; set; }
        public bool ObjetoRobado    { get; set; }
        public bool ZonaCerrada     { get; set; }
        public bool PalancaActivada { get; set; }
        public bool LadronCapturado { get; set; }
        public bool MapaBarrido     { get; set; }

        public EstadoMundo Clonar() => new EstadoMundo
        {
            LadronVisible   = LadronVisible,
            ObjetoRobado    = ObjetoRobado,
            ZonaCerrada     = ZonaCerrada,
            PalancaActivada = PalancaActivada,
            LadronCapturado = LadronCapturado,
            MapaBarrido     = MapaBarrido,
        };

        public override string ToString() =>
            $"[visible:{LadronVisible} robado:{ObjetoRobado} " +
            $"zona:{ZonaCerrada} palanca:{PalancaActivada} " +
            $"capturado:{LadronCapturado} barrido:{MapaBarrido}]";
    }

    public class OperadorStrips
    {
        public TareaContrato                  Tarea          { get; }
        public System.Func<EstadoMundo, bool> Precondiciones { get; }
        public System.Action<EstadoMundo>     Efectos        { get; }

        public OperadorStrips(
            TareaContrato                  tarea,
            System.Func<EstadoMundo, bool> precondiciones,
            System.Action<EstadoMundo>     efectos)
        {
            Tarea          = tarea;
            Precondiciones = precondiciones;
            Efectos        = efectos;
        }
    }

    public static class PlanificadorStrips
    {
        private static readonly OperadorStrips[] Operadores = new[]
        {
            // CerrarZona: ladrón visible y zona no cerrada.
            new OperadorStrips(
                TareaContrato.CerrarZona,
                e =>  e.LadronVisible && !e.ZonaCerrada,
                e => { e.ZonaCerrada = true; }
            ),

            // IrAPalanca: objeto robado y palanca no activada.
            new OperadorStrips(
                TareaContrato.IrAPalanca,
                e =>  e.ObjetoRobado && !e.PalancaActivada,
                e => { e.PalancaActivada = true; }
            ),

            // BarrerMapa: ladrón no visible y mapa no barrido.
            // Se delega cuando el ladrón se pierde tras un robo.
            new OperadorStrips(
                TareaContrato.BarrerMapa,
                e =>  e.ObjetoRobado && !e.LadronVisible && !e.MapaBarrido,
                e => { e.MapaBarrido = true; }
            ),

            // Perseguir: ladrón visible y zona ya cerrada.
            new OperadorStrips(
                TareaContrato.Perseguir,
                e =>  e.LadronVisible && e.ZonaCerrada,
                e => { e.LadronCapturado = true; }
            ),
        };

        public static List<TareaContrato> Planificar(
            EstadoMundo                    estado,
            System.Func<EstadoMundo, bool> objetivo,
            int                            profundidadMax = 4)
        {
            var plan = new List<TareaContrato>();
            bool encontrado = Buscar(estado, objetivo, plan, profundidadMax, new HashSet<TareaContrato>());

            if (encontrado)
                Debug.Log($"[PlanificadorStrips] Plan encontrado: [{string.Join(", ", plan)}]");
            else
                Debug.Log($"[PlanificadorStrips] No se encontró plan desde {estado}");

            return plan;
        }

        private static bool Buscar(
            EstadoMundo                    estado,
            System.Func<EstadoMundo, bool> objetivo,
            List<TareaContrato>            plan,
            int                            profundidad,
            HashSet<TareaContrato>         usados)
        {
            if (objetivo(estado)) return true;
            if (profundidad == 0) return false;

            foreach (var op in Operadores)
            {
                if (usados.Contains(op.Tarea))    continue;
                if (!op.Precondiciones(estado))    continue;

                EstadoMundo siguiente = estado.Clonar();
                op.Efectos(siguiente);

                plan.Add(op.Tarea);
                usados.Add(op.Tarea);

                if (Buscar(siguiente, objetivo, plan, profundidad - 1, usados))
                    return true;

                plan.RemoveAt(plan.Count - 1);
                usados.Remove(op.Tarea);
            }

            return false;
        }
    }

    public static class Planificador
    {
        public static TareaContrato[] PlanParaEvento(
            EventoSeguridad   evento,
            PrioridadContrato prioridad)
        {
            var estado = new EstadoMundo
            {
                LadronVisible   = evento == EventoSeguridad.JugadorDetectado,
                ObjetoRobado    = evento == EventoSeguridad.ObjetoRobado
                                  || evento == EventoSeguridad.JugadorDetectado,
                ZonaCerrada     = false,
                PalancaActivada = false,
                LadronCapturado = false,
                MapaBarrido     = false,
            };

            System.Func<EstadoMundo, bool> objetivo = prioridad switch
            {
                // Alta: jugador detectado → cerrar zona + palanca.
                PrioridadContrato.Alta  => e => e.ZonaCerrada && e.PalancaActivada,
                // Media: objeto robado → palanca + barrer mapa.
                PrioridadContrato.Media => e => e.PalancaActivada && e.MapaBarrido,
                _                       => e => true
            };

            var plan = PlanificadorStrips.Planificar(estado, objetivo);
            return plan.ToArray();
        }
    }
}