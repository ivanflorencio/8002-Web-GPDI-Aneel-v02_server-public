using System;
using System.Collections.Generic;
using TaesaCore.Models;

namespace PeD.Core.ApiModels.Analises
{
    public class AnaliseTecnicaDto : BaseEntity
    {
        public int DemandaId { get; set; }
        public int PropostaId { get; set; }
        public string Justificativa { get; set; }
        public string Comentarios { get; set; }
        public double PontuacaoFinal { get; set; }
        public string Status { get; set; }
        public List<ParecerTecnicoDto> Pareceres { get; set; }
    }
}