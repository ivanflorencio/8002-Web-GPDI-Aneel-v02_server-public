using System;
using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class DetalheEtapaDto
    {
        public string Etapa { get; set; }
        public string Produto { get; set; }
        public DateTime InicioPeriodo { get; set; }
        public DateTime FimPeriodo { get; set; }
        public List<RecursoDto> Recursos { get; set; }
    }
}
