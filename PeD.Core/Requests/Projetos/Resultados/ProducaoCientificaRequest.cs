using System;
using System.ComponentModel.DataAnnotations;
using PeD.Core.Models;
using PeD.Core.Models.Catalogos;
using PeD.Core.Models.Projetos;
using TaesaCore.Models;

namespace PeD.Core.Requests.Projetos.Resultados
{
    public class ProducaoCientificaRequest : BaseEntity
    {
        public string Tipo { get; set; }
        public DateTime DataPublicacao { get; set; }
        public bool ConfirmacaoPublicacao { get; set; }
        [MaxLength(50)] public string NomeEventoPublicacao { get; set; }

        [MaxLength(50)] // @todo verificar se é somente 50 caracteres
        public string LinkPublicacao { get; set; }

        public int PaisId { get; set; }
        [MaxLength(30)] public string Cidade { get; set; }
        [MaxLength(200)] public string TituloTrabalho { get; set; }
    }
}