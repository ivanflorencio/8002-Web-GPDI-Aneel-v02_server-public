using System.Linq;
using AutoMapper;
using PeD.Core.ApiModels.Propostas;
using PeD.Core.Models.Propostas;

namespace PeD.Mapping
{
    public class PropostaMapping : Profile
    {
        public PropostaMapping()
        {
            CreateMap<Proposta, PropostaDto>()
                .ForMember(dest => dest.Fornecedor, opt => opt.MapFrom(src => src.Fornecedor.Nome))
                .ForMember(dest => dest.Captacao, opt => opt.MapFrom(src => src.Captacao.Titulo))
                .ForMember(dest => dest.DataTermino, opt => opt.MapFrom(src => src.Captacao.Termino))
                .ForMember(dest => dest.Consideracoes, opt => opt.MapFrom(src => src.Captacao.Consideracoes))
                .ForMember(dest => dest.Arquivos,
                    opt => opt
                        .MapFrom(src => src.Captacao.Arquivos.Where(a => a.AcessoFornecedor)))
                ;

            CreateMap<Contrato, ContratoDto>()
                .ForMember(c => c.Titulo, opt => opt.MapFrom(src => src.Parent.Titulo));
            CreateMap<Contrato, ContratoListItemDto>()
                .ForMember(c => c.Titulo, opt => opt.MapFrom(src => src.Parent.Titulo));
            CreateMap<ContratoRevisao, ContratoRevisaoDto>();
        }
    }
}