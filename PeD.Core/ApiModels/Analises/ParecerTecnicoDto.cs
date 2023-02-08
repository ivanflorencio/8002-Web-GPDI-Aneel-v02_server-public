using System;
using System.Collections.Generic;
using PeD.Core.Models.Propostas;
using TaesaCore.Models;

namespace PeD.Core.ApiModels.Analises
{
    public class ParecerTecnicoDto : BaseEntity
    {
        public int CriterioId;
        public string DescricaoCriterio;
        public int Peso { get; set; }        
        public string Justificativa { get; set; }        
        public int Pontuacao { get; set; }
    }
}