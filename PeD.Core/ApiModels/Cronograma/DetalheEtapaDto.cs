using System;
using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class DetalheEtapaDto
    {
        public string Etapa { get; set; }
        public string ProdutoTitulo { get; set; }
        public string ProdutoDescricao { get; set; }
        public string ProdutoTipo { get; set; }
        public string ProdutoTipoDetalhado { get; set; }
        public string FaseCadeia { get; set; }
        public DateTime InicioPeriodo { get; set; }
        public DateTime FimPeriodo { get; set; }
        public List<RecursoDto> Recursos { get; set; }
    }
}
