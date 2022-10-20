using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class EmpresaCronogramaDto
    {
        public string Nome { get; set; }
        public List<int> Desembolso { get; set; }
        public int Total { get; set; }
    }
}
