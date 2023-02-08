using System.Collections.Generic;
using FluentValidation;
using TaesaCore.Models;

namespace PeD.Core.Requests.Analises
{
    public class AnaliseTecnicaRequest : BaseEntity
    {
        public string Justificativa { get; set; }
        public string Comentarios { get; set; }
        public double PontuacaoFinal { get; set; }
        public int PropostaId { get; set; }
        public List<ParecerTecnicoRequest> Pareceres { get; set; }

    }

   
}