using System;

namespace PeD.Views.Email.Captacao.Propostas
{
    public class ContratoRevisor
    {
        public string Fornecedor { get; set; }
        public Guid PropostaGuid { get; set; }
        public Core.Models.Captacoes.Captacao Captacao { get; set; }
    }
}