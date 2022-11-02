using FluentValidation;

namespace PeD.Core.Requests.Sistema.TabelaValorHora
{
    public class TabelaValorHoraCreateRequest
    {
        public string Nome { get; set; }        
        public string Registros { get; set; }
        
    }

    public class TabelaValorHoraRequestValidator : AbstractValidator<TabelaValorHoraCreateRequest>
    {
        public TabelaValorHoraRequestValidator()
        {
            RuleFor(f => f.Nome).NotEmpty();
            RuleFor(f => f.Registros).NotEmpty();            
        }
    }
}