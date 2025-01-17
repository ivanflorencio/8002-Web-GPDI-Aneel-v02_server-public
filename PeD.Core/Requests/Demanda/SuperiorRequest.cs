using FluentValidation;

namespace PeD.Core.Requests.Demanda
{
    public class SuperiorRequest
    {
        public string SuperiorDireto { get; set; }
        public string AnalistaPedId { get; set; }
        public string AnalistaTecnicoId { get; set; }
        public string TabelaValorHoraId { get; set; }        
    }

    public class SuperiorRequestValidator : AbstractValidator<SuperiorRequest>
    {
        public SuperiorRequestValidator()
        {
            RuleFor(r => r.SuperiorDireto).NotEmpty().NotNull();
        }
    }
}