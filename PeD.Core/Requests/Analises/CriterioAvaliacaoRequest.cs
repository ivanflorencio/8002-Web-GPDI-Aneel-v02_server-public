using FluentValidation;
using TaesaCore.Models;

namespace PeD.Core.Requests.Analises
{
    public class CriterioAvaliacaoRequest
    {
        public string Descricao { get; set; }
        public int DemandaId { get; set; }
        public int CriterioId { get; set; }
        public int Peso { get; set; }
        public bool DoGestor { get; set; }
    }

    public class CriterioAvaliacaoRequestValidator : AbstractValidator<CriterioAvaliacaoRequest>
    {
        public CriterioAvaliacaoRequestValidator()
        {
            RuleFor(r => r.Descricao).NotEmpty();      
        }
    }
}