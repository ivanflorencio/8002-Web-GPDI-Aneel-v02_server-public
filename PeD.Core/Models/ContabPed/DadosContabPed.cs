using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using TaesaCore.Models;

namespace PeD.Core.Models
{

    public class DadosContabPed
    {
        [Column("TIPOS")]
        public string Tipo { get; set; }

        [Column("DESCRICAO")]
        public string Descricao { get; set; }

        [Column("VALOR")]
        public decimal Valor { get; set; }

        [Column("PERIODO")]
        public string Periodo { get; set; }
    }
}