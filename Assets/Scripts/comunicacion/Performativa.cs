namespace GuardiaIA
{
    /// Performativas definidas en FIPA-ACL relevantes para este sistema.
    /// Inform y Request son las básicas; el resto son macros sobre ellas.
    public enum Performativa
    {
        // Básicas
        Inform,         // El emisor informa de un hecho al receptor.
        Request,        // El emisor solicita al receptor que ejecute una acción.

        // Contract Net
        Cfp,            // Call for Proposals: el gestor lanza la convocatoria.
        Propose,        // El contratista ofrece su disponibilidad.
        Refuse,         // El contratista rechaza participar.
        AcceptProposal, // El gestor acepta la propuesta de un contratista.
        RejectProposal, // El gestor rechaza la propuesta de un contratista.
        InformDone,     // El contratista notifica que completó la tarea.
        Failure,        // El contratista notifica que no pudo completar la tarea.
    }
}
