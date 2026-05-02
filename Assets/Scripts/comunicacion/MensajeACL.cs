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

        // Tarea asignada por el gestor al aceptar una propuesta (AcceptProposal)
        public TareaContrato Tarea;

        // Distancia del contratista al ladrón (usada en Propose para que
        // el gestor pueda comparar ofertas y elegir la mejor)
        public float DistanciaAlLadron;

        // Lista de tareas disponibles que el gestor anuncia en el Cfp.
        // Permite al contratista saber qué puede asumir antes de proponer.
        public TareaContrato[] TareasDisponibles;

        // Tareas que el contratista declara poder ejecutar (enviado en Propose).
        // El gestor cruza esta lista con TareasDisponibles para adjudicar.
        public TareaContrato[] TareasPosibles;
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
    ///
    /// Emisor y Receptor son GestorComunicacion en lugar de Cerebro para que el
    /// sistema de mensajería funcione con cualquier tipo de agente (guardias, cámaras…)
    /// sin depender del tipo concreto del cerebro.
    public class MensajeACL
    {
        // Cabecera (obligatoria)
        public Performativa        Performativa    { get; set; }
        public GestorComunicacion  Emisor          { get; set; }
        public GestorComunicacion  Receptor        { get; set; }

        // Cabecera (opcional)
        public string       Ontologia       { get; set; } = "Seguridad";
        public string       ConversationId  { get; set; }
        public string       InReplyTo       { get; set; }

        // Contenido
        public ContenidoMensaje Contenido   { get; set; }

        public override string ToString() =>
            $"[{Performativa}] {Emisor?.name} → {Receptor?.name} | conv:{ConversationId}";
    }
}
