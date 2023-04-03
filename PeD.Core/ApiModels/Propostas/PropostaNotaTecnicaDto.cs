using System;
using System.Collections.Generic;

namespace PeD.Core.ApiModels.Propostas
{
    public class PropostaNotaTecnicaDto : PropostaNodeDto
    {
        public string Titulo { get; set; }
        public string Conteudo { get; set; }
        public int RelatorioDiretoriaId { get; set; }
        public string Header { get; set; }
        public string Footer { get; set; }
        public bool Finalizado { get; set; }
    }

}