using UnityEngine;

namespace GuardiaIA
{
    /// Contenido posible de un mensaje en este sistema.
    /// Usamos un struct con campos opcionales en lugar de serializar strings
    /// para mantener el tipado fuerte en C# sin parsear texto.
    public struct ContenidoMensaje
    {
        // Posición del ladrón (usada en Inform y Cfp de avistamiento)
        public Vector3 PosicionLadron;

        // Tarea asignada por el gestor al aceptar una propuesta
        public TareaContrato Tarea;

        // Distancia del contratista al ladrón (usada en Propose para que
        // el gestor pueda comparar ofertas y elegir la mejor)
        public float DistanciaAlLadron;
    }

    /// Tareas que el gestor puede asignar a los contratistas.
    public enum TareaContrato
    {
        Ninguna,
        Perseguir,   // Reservado para uso interno del árbitro (no se asigna por contrato).
        CerrarZona,  // Contratista 1 (más cercano): bloquear zona de escape.
        IrAPalanca,  // Contratista 2: activar la palanca de alarma.
    }

    /// Mensaje FIPA-ACL.
    /// Refleja la estructura estándar: performativa + parámetros de cabecera + contenido.
    public class MensajeACL
    {
        // Cabecera (obligatoria)
        public Performativa Performativa    { get; set; }
        public Cerebro      Emisor          { get; set; }
        public Cerebro      Receptor        { get; set; }

        // Cabecera (opcional)
        public string       Ontologia       { get; set; } = "Seguridad";
        public string       ConversationId  { get; set; }
        public string       InReplyTo       { get; set; }  // id del mensaje al que responde

        // Contenido
        public ContenidoMensaje Contenido   { get; set; }

        public override string ToString() =>
            $"[{Performativa}] {Emisor?.name} → {Receptor?.name} | conv:{ConversationId}";
    }
}