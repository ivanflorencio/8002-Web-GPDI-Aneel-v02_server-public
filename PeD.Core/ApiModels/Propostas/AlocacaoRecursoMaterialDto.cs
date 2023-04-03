using TaesaCore.Models;

namespace PeD.Core.ApiModels.Propostas
{
    public class AlocacaoRecursoMaterialDto : BaseEntity
    {
        public int RecursoId { get; set; }
        public string Recurso { get; set; }
        public string RecursoCategoria { get; set; }
        public int EtapaId { get; set; }
        public System.Int16 MesDesembolso { get; set; }

        public int? EmpresaFinanciadoraId { get; set; }
        public string EmpresaFinanciadora { get; set; }

        public string Modalidade { get; set; }

        public int? EmpresaRecebedoraId { get; set; }
        public string EmpresaRecebedora { get; set; }

        public string Justificativa { get; set; }
        public int Quantidade { get; set; }
        public decimal Valor { get; set; }
    }
}