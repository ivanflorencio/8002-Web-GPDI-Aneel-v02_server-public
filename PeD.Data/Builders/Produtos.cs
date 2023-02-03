using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeD.Core.Models.Catalogos;

namespace PeD.Data.Builders
{
    public static class Produtos
    {
        public static EntityTypeBuilder<ProdutoTipo> Seed(this EntityTypeBuilder<ProdutoTipo> builder)
        {
            var tipos = new[]
            {
                new ProdutoTipo("CM", "Conceito ou Metodologia"),
                new ProdutoTipo("SW", "Software")
                new ProdutoTipo("SM", "Sistema"),
                new ProdutoTipo("CD", "Componente ou Dispositivo"),
                new ProdutoTipo("MS", "Material ou Substância"),
                new ProdutoTipo("ME", "Máquina ou Equipamento"),
            };
            builder.HasData(tipos);
            return builder;
        }
    }
}