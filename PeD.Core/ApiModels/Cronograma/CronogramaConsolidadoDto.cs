using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class CronogramaConsolidadoDto
    {
        public InicioCronogramaDto Inicio { get; set; }
        public List<ProjetoCronogramaDto> Etapas { get; set; }
        public List<EmpresaCronogramaDto> Empresas { get; set; }
    }
}
