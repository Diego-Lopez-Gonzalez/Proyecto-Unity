namespace GuardiaIA
{
    // Contrato que debe cumplir todo estado del agente.
    // Cada estado recibe por parámetro todo lo que necesita.
    public interface IEstado
    {
        // Se ejecuta UNA vez al entrar al estado.
        void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones);

        // Se ejecuta cada frame desde Cerebro.Update().
        void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones);

        // Se ejecuta UNA vez al salir del estado.
        void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones);

        // El estado lo pone a true cuando ha completado su tarea.
        // El Cerebro lo detecta en Update y lanza EvaluarPrioridades.
        // Los estados base (Patrulla, InspeccionAleatoria, Persecucion)
        // nunca lo ponen a true: esperan a ser subsumidos por un evento externo.
        bool HaTerminado { get; }
    }
}