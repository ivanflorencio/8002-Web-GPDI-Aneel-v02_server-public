using FluentValidation;
using TaesaCore.Models;

namespace PeD.Core.Requests.Proposta
{
    public class EtapaRequest : BaseEntity
    {
        public string DescricaoAtividades { get; set; }
        public int ProdutoId { get; set; }
        public int MesInicio { get; set; }
        public int MesFinal { get; set; }
    }

    public class EtapaRequestValidator : AbstractValidator<EtapaRequest>
    {
        public EtapaRequestValidator()
        {
            RuleFor(r => r.DescricaoAtividades).NotEmpty();
            RuleFor(r => r.ProdutoId).NotNull();            
        }
    }
}