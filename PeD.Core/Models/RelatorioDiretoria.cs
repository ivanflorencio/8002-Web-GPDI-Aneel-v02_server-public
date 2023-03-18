using FluentValidation;
using TaesaCore.Models;

namespace PeD.Core.Models
{
    public class RelatorioDiretoria : BaseEntity
    {
        public string Titulo { get; set; }
        public string Header { get; set; }
        public string Conteudo { get; set; }
        public string Footer { get; set; }
    }

    public class RelatorioDiretoriaValidator : AbstractValidator<RelatorioDiretoria>
    {
        public RelatorioDiretoriaValidator()
        {
            RuleFor(r => r.Titulo).NotEmpty();
            RuleFor(r => r.Header).NotEmpty();
            RuleFor(r => r.Conteudo).NotEmpty();
            RuleFor(r => r.Footer).NotEmpty();
        }
    }
}