using PeD.Core.Models.Projetos;

namespace PeD.Core.Requests.Projetos
{
    public class CronogramaSimuladoRequest
    {
        public int Id { get; set; }
        public string Proposta { get; set; }
        public string Etapa { get; set; }
        public string MesInicio { get; set; }
        public int Duracao { get; set; }
    }
}