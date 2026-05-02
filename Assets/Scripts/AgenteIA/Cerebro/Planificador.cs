using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    // ── Eventos que disparan la planificación ─────────────────────────────────

    public enum EventoSeguridad
    {
        JugadorDetectado,   // guardia o cámara ve al ladrón directamente
        ObjetoRobado,       // objeto vigilado ha desaparecido
    }

    // ── Estado del mundo para STRIPS ─────────────────────────────────────────

    /// Representa los hechos conocidos del mundo en un momento dado.
    /// El planificador parte de un EstadoMundo inicial y busca llegar
    /// a uno que satisfaga el objetivo.
    public class EstadoMundo
    {
        public bool LadronVisible      { get; set; }
        public bool ObjetoRobado       { get; set; }
        public bool ZonaCerrada        { get; set; }
        public bool PalancaActivada    { get; set; }
        public bool LadronCapturado    { get; set; }

        /// Copia superficial para no mutar el estado original durante la búsqueda.
        public EstadoMundo Clonar() => new EstadoMundo
        {
            LadronVisible   = LadronVisible,
            ObjetoRobado    = ObjetoRobado,
            ZonaCerrada     = ZonaCerrada,
            PalancaActivada = PalancaActivada,
            LadronCapturado = LadronCapturado,
        };

        public override string ToString() =>
            $"[visible:{LadronVisible} robado:{ObjetoRobado} " +
            $"zona:{ZonaCerrada} palanca:{PalancaActivada} capturado:{LadronCapturado}]";
    }

    // ── Operador STRIPS ───────────────────────────────────────────────────────

    /// Un operador STRIPS tiene:
    ///   · Tarea asociada      → la acción que se delega vía Contract Net.
    ///   · Precondiciones      → función que comprueba si el estado permite aplicarlo.
    ///   · Efectos             → función que muta una copia del estado tras aplicarlo.
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

    // ── Planificador STRIPS con encadenamiento hacia atrás ────────────────────

    /// Genera un plan (lista de tareas) que lleva desde un EstadoMundo inicial
    /// hasta satisfacer un objetivo, usando encadenamiento hacia atrás.
    ///
    /// El plan resultante es la secuencia de TareaContrato que Contract Net
    /// distribuirá entre los guardias disponibles.
    ///
    /// Limitaciones intencionadas (para mantenerlo sencillo):
    ///   · Profundidad máxima configurable (por defecto 4).
    ///   · Sin ciclos: no repite operadores ya usados en la misma rama.
    public static class PlanificadorStrips
    {
        // Catálogo de operadores disponibles.
        // El orden importa: los primeros se intentan antes en la búsqueda.
        private static readonly OperadorStrips[] Operadores = new[]
        {
            // CerrarZona: solo si el ladrón es visible y la zona no está cerrada.
            new OperadorStrips(
                TareaContrato.CerrarZona,
                e =>  e.LadronVisible && !e.ZonaCerrada,
                e => { e.ZonaCerrada = true; }
            ),

            // IrAPalanca: solo si el objeto fue robado y la palanca no está activa.
            new OperadorStrips(
                TareaContrato.IrAPalanca,
                e =>  e.ObjetoRobado && !e.PalancaActivada,
                e => { e.PalancaActivada = true; }
            ),

            // Perseguir: requiere ladrón visible Y zona ya cerrada.
            // Representa la persecución coordinada tras cortar la huida.
            new OperadorStrips(
                TareaContrato.Perseguir,
                e =>  e.LadronVisible && e.ZonaCerrada,
                e => { e.LadronCapturado = true; }
            ),
        };

        /// Busca un plan desde <estado> que satisfaga <objetivo>.
        /// Devuelve la lista de tareas en orden de ejecución, o lista vacía si no hay plan.
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

        // Encadenamiento hacia atrás recursivo con backtracking.
        private static bool Buscar(
            EstadoMundo                    estado,
            System.Func<EstadoMundo, bool> objetivo,
            List<TareaContrato>            plan,
            int                            profundidad,
            HashSet<TareaContrato>         usados)
        {
            // ¿Ya cumplimos el objetivo?
            if (objetivo(estado)) return true;

            // ¿Agotamos la profundidad?
            if (profundidad == 0) return false;

            foreach (var op in Operadores)
            {
                // No repetir operadores en la misma rama.
                if (usados.Contains(op.Tarea)) continue;

                // ¿Se pueden aplicar las precondiciones?
                if (!op.Precondiciones(estado)) continue;

                // Aplicamos el operador sobre una copia del estado.
                EstadoMundo siguiente = estado.Clonar();
                op.Efectos(siguiente);

                plan.Add(op.Tarea);
                usados.Add(op.Tarea);

                if (Buscar(siguiente, objetivo, plan, profundidad - 1, usados))
                    return true;

                // Backtracking: deshacemos si esta rama no lleva al objetivo.
                plan.RemoveAt(plan.Count - 1);
                usados.Remove(op.Tarea);
            }

            return false;
        }
    }

    // ── Planificador público (punto de entrada desde Cerebro/CerebroCamara) ───

    /// Construye el EstadoMundo inicial a partir del evento y la prioridad,
    /// lanza el PlanificadorStrips y devuelve el array de tareas resultante.
    /// Es el único punto de contacto con el resto del sistema: Cerebro y
    /// CerebroCamara llaman solo a este método, sin saber nada de STRIPS.
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
            };

            // Perseguir nunca se delega: el iniciador ya persigue por su cuenta.
            // El plan delegado a contratistas tiene como objetivo máximo ZonaCerrada,
            // tanto si el iniciador es un guardia como si es una cámara.
            System.Func<EstadoMundo, bool> objetivo = prioridad switch
            {
                PrioridadContrato.Alta => e => e.ZonaCerrada && e.PalancaActivada,
                PrioridadContrato.Media => e => e.PalancaActivada,
                _                      => e => true
            };

            var plan = PlanificadorStrips.Planificar(estado, objetivo);
            return plan.ToArray();
        }
    }
}