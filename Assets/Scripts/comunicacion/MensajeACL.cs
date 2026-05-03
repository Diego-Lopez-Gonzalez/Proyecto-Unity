using UnityEngine;

namespace GuardiaIA
{
    public struct ContenidoMensaje
    {
        public Vector3 PosicionLadron;
        public TareaContrato Tarea;
        public float DistanciaAlLadron;
        public TareaContrato[] TareasDisponibles;
        public TareaContrato[] TareasPosibles;

        /// Prioridad del contrato, declarada por el gestor en el Cfp.
        /// El contratista la compara con su tarea activa para decidir si acepta.
        public PrioridadContrato Prioridad;
    }

    public enum TareaContrato
    {
        Ninguna,
        Perseguir,
        CerrarZona,
        IrAPalanca,
        BarrerMapa,
    }

    public class MensajeACL
    {
        public Performativa        Performativa    { get; set; }
        public GestorComunicacion  Emisor          { get; set; }
        public GestorComunicacion  Receptor        { get; set; }
        public string       Ontologia       { get; set; } = "Seguridad";
        public string       ConversationId  { get; set; }
        public string       InReplyTo       { get; set; }
        public ContenidoMensaje Contenido   { get; set; }

        public override string ToString() =>
            $"[{Performativa}] {Emisor?.name} → {Receptor?.name} | conv:{ConversationId}";
    }
}