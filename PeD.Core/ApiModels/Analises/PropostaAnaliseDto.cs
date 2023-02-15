using System;
using System.Collections.Generic;
using TaesaCore.Models;

namespace PeD.Core.ApiModels.Analises
{
    public class PropostaAnaliseDto
    {
        public int DemandaId { get; set; }
        public string TituloDemanda { get; set; }
        public int PropostaId { get; set; }
        public string Fornecedor { get; set; }
        public string DataHora { get; set; }
        public string AnalistaResponsavel { get; set; }
        public string StatusAnalise { get; set; }  
        public double Pontuacao { get; set; }
    }
}