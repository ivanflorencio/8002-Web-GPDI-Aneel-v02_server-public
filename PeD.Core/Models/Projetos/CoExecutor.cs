using System.ComponentModel.DataAnnotations.Schema;

namespace PeD.Core.Models.Projetos
{
    public enum CoExecutorFuncao
    {
        Executora, // Empresas cadastradas pelo fornecedor
        Proponente, // Não usado
        Cooperada, // Empresa Taesa
        Interveniente // Não foi usado
    }

    [Table("ProjetoCoExecutores")]
    public class CoExecutor : ProjetoNode
    {
        public string CNPJ { get; set; }
        public string UF { get; set; }
        
        public string Codigo { get; set; }
        public string RazaoSocial { get; set; }
        public string Nome => RazaoSocial;

        public CoExecutorFuncao Funcao { get; set; }
    }
}