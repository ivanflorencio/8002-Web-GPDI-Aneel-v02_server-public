namespace PeD.Core.Requests.Proposta
{
    public class RelatorioDiretoriaRequest
    {
        public int Id { get; set; }
        public bool Draft { get; set; }
        public string Conteudo { get; set; }
    }
}