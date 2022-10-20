using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class CronogramaDto
    {
        public InicioCronogramaDto Inicio { get; set; }
        public List<EtapaCronogramaDto> Etapas { get; set; }
        public List<EmpresaCronogramaDto> Empresas { get; set; }
    }
}
