using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PeD.Core.Models.Propostas
{
    [Table("PropostaEtapas")]
    public class Etapa : PropostaNode
    {
        public string DescricaoAtividades { get; set; }

        public int ProdutoId { get; set; }
        public Produto Produto { get; set; }

        public List<int> Meses { get; set; }
    }
}