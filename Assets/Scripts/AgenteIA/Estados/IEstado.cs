namespace GuardiaIA
{
    // Contrato que debe cumplir todo estado del agente.
    // Cada estado recibe por parámetro todo lo que necesita;
    public interface IEstado
    {
        //Se ejecuta UNA vez al entrar al estado.
        void Entrar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones);

        /// Se ejecuta cada frame desde Cerebro.Update().
        void Ejecutar(Cerebro cerebro, BaseConocimiento bc, Acciones acciones);

        /// Se ejecuta UNA vez al salir del estado.
        void Salir(Cerebro cerebro, BaseConocimiento bc, Acciones acciones);
    }
}