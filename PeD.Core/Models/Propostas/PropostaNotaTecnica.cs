using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


namespace PeD.Core.Models.Propostas
{
    [Table("PropostaNotaTecnica")]
    public class PropostaNotaTecnica : PropostaNode
    {
        public string Conteudo { get; set; }
        public bool Finalizado { get; set; }
        public RelatorioDiretoria Parent { get; set; }
        public int ParentId { get; set; }
        public int? FileId { get; set; }
        public FileUpload File { get; set; }
    }
}