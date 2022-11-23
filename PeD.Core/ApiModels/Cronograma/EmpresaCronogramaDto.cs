using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class EmpresaCronogramaDto
    {
        public string Nome { get; set; }
        public List<double> Desembolso { get; set; }
        public List<double> Executado { get; set; }        
        public double Total { get; set; }
    }
}
