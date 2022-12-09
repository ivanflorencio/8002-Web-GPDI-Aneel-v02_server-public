using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class ProjetoCronogramaDto
    {
        public int ProjetoId { get; set; }
        public string Titulo { get; set; }
        public string TituloCompleto { get; set; }
        public string Codigo { get; set; }
        public string Numero { get; set; }        
        public List<int> Meses { get; set; }
        public int MesInicio { get; set; }
        public int AnoInicio { get; set; }
        public int MesFim { get; set; }
        public int AnoFim { get; set; }        
        public string Etapa { get; set; }        
        public string Produto { get; set; }
    }
}
