namespace PeD.Core.ApiModels.Propostas
{
    public class EmpresaDto : PropostaNodeDto
    {
        public bool Required { get; set; }
        public string CNPJ { get; set; }
        public string UF { get; set; }
        public string RazaoSocial { get; set; }
        public string Funcao { get; set; }
        public string Codigo { get; set; }
        public int EmpresaRefId { get; set; }
    }
}