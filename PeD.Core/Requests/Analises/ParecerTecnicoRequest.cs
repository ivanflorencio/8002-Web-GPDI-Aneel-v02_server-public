using System.Collections.Generic;
using FluentValidation;
using TaesaCore.Models;

namespace PeD.Core.Requests.Analises
{
    public class ParecerTecnicoRequest
    {
        public int ParecerId { get; set; }
        public int CriterioId { get; set; }
        public string Justificativa { get; set; }
        public int Pontuacao { get; set; }        
        
    }

   
}