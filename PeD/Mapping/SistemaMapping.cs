using AutoMapper;
using PeD.Core.ApiModels;
using PeD.Core.ApiModels.Fornecedores;
using PeD.Core.ApiModels.Propostas;
using PeD.Core.ApiModels.Sistema;
using PeD.Core.Models;
using PeD.Core.Models.Sistema;
using PeD.Core.Requests.Sistema.Fornecedores;
using TaesaCore.Models;
using Empresa = PeD.Core.Models.Propostas.Empresa;

namespace PeD.Mapping
{
    public class SistemaMapping : Profile
    {
        public SistemaMapping()
        {
            CreateMap<BaseEntity, BaseEntityDto>().ForMember(b => b.Name, opt => opt.MapFrom(src => src.ToString()));
            CreateMap<Core.Models.Fornecedores.Fornecedor, FornecedorDto>()
                .ForMember(f => f.ResponsavelNome, opt => opt.MapFrom(src => src.Responsavel.NomeCompleto ?? ""))
                .ForMember(f => f.ResponsavelEmail, opt => opt.MapFrom(src => src.Responsavel.Email ?? ""))
                .ReverseMap();
            CreateMap<FornecedorCreateRequest, Core.Models.Fornecedores.Fornecedor>().ReverseMap();
            CreateMap<FornecedorEditRequest, Core.Models.Fornecedores.Fornecedor>().ReverseMap();
            CreateMap<Empresa, EmpresaDto>().ReverseMap();
            CreateMap<TabelaValorHora, TabelaValorHoraDto>().ReverseMap();
            CreateMap<Clausula, ClausulaDto>().ReverseMap();
            CreateMap<ItemAjuda, ItemAjudaDto>()
                .ForMember(i => i.HasContent, opt => opt.MapFrom(
                    src => !string.IsNullOrWhiteSpace(src.Conteudo)
                ));
            CreateMap<FileUpload, FileUploadDto>();
        }
    }
}