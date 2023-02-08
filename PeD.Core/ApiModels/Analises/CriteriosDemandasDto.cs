using System;
using System.Collections.Generic;
using PeD.Core.Models.Propostas;

namespace PeD.Core.ApiModels.Analises
{
    public class CriteriosDemandasDto
    {
        public int DemandaId { get; set; }
        public string TituloDemanda { get; set; }
        public List<CriterioAvaliacao> CriteriosAvaliacao { get; set; }
    }
}