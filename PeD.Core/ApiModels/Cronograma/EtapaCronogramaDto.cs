using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class EtapaCronogramaDto
    {
        public int Numero { get; set; }
        public string Etapa { get; set; }
        public List<int> Meses { get; set; }
        public string Produto { get; set; }
        public DetalheEtapaDto Detalhe { get; set; }
    }
}
