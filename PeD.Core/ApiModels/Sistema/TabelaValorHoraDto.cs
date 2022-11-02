using TaesaCore.Models;

namespace PeD.Core.ApiModels.Sistema
{
    public class TabelaValorHoraDto : BaseEntity
    {
        public string Nome { get; set; }
        public string Registros { get; set; }
    }
}