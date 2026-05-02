namespace GuardiaIA
{
    public enum Performativa
    {
        Inform,
        Request,
        Cfp,
        Propose,
        Refuse,
        AcceptProposal,
        RejectProposal,
        InformDone,
        Failure,
        Cancel,
    }

    /// Prioridad de un contrato declarada por el gestor en el Cfp.
    /// El contratista la usa para decidir si interrumpe una tarea activa.
    ///   Alta   → ladrón detectado, persecución en curso.
    ///   Media  → objeto robado, gestión de alarma.
    ///   Baja   → tareas de vigilancia pasiva, inspección.
    public enum PrioridadContrato
    {
        Baja   = 0,
        Media  = 1,
        Alta   = 2,
    }
}