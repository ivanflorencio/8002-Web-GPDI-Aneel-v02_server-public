using System.Linq;
using AutoMapper;
using PeD.Core.ApiModels.Propostas;
using PeD.Core.Models.Captacoes;
using PeD.Core.Models.Propostas;
using PeD.Core.Requests.Proposta;
using PeD.Services.Captacoes;
using PeD.Views.Email.Captacao.Propostas;

namespace PeD.Mapping
{
    public class PropostaMapping : Profile
    {
        public PropostaMapping()
        {
            CreateMap<Proposta, PropostaSelecaoDto>()
                .ForMember(dest => dest.Fornecedor, opt => opt.MapFrom(src => src.Fornecedor.Nome))
                .ForMember(dest => dest.DataTermino, opt => opt.MapFrom(src => src.Captacao.Termino))
                .ForMember(dest => dest.ContratoFinalizado, opt => opt.MapFrom(src => src.Contrato.Finalizado))
                .ForMember(dest => dest.PlanoFinalizado, opt => opt.MapFrom(src => src.Finalizado));
            CreateMap<Proposta, PropostaDto>()
                .ForMember(dest => dest.Fornecedor, opt => opt.MapFrom(src => src.Fornecedor.Nome))
                .ForMember(dest => dest.Captacao, opt => opt.MapFrom(src => src.Captacao.Titulo))
                .ForMember(dest => dest.CaptacaoStatus, opt => opt.MapFrom(src => src.Captacao.Status))
                .ForMember(dest => dest.DataTermino, opt => opt.MapFrom(src => src.Captacao.Termino))
                .ForMember(dest => dest.Consideracoes, opt => opt.MapFrom(src => src.Captacao.Consideracoes))
                .ForMember(dest => dest.ContratoFinalizado, opt => opt.MapFrom(src => src.Contrato.Finalizado))
                .ForMember(dest => dest.PlanoFinalizado, opt => opt.MapFrom(src => src.Finalizado))
                .ForMember(dest => dest.Arquivos,
                    opt => opt.MapFrom(src => src.Captacao.Arquivos.Where(a => a.AcessoFornecedor)))
                ;

            CreateMap<PropostaContrato, PropostaContratoDto>()
                .ForMember(c => c.Titulo, opt => opt.MapFrom(src => src.Parent.Titulo))
                .ForMember(c => c.Header,
                    opt => opt.MapFrom(src => ContratoService.ReplaceShortcodes(src.Parent.Header, src.Proposta)))
                .ForMember(c => c.Footer,
                    opt => opt.MapFrom(src => ContratoService.ReplaceShortcodes(src.Parent.Footer, src.Proposta)))
                ;

            CreateMap<PropostaContrato, ContratoListItemDto>()
                .ForMember(c => c.Titulo, opt => opt.MapFrom(src => src.Parent.Titulo));
            CreateMap<PropostaContratoRevisao, ContratoRevisaoDto>();
            CreateMap<PropostaContratoRevisao, ContratoRevisaoListItemDto>()
                .ForMember(r => r.Name, opt => opt.MapFrom(src => src.Parent.Parent.Titulo));

            CreateMap<PropostaRelatorioDiretoria, PropostaRelatorioDiretoriaDto>();

            CreateMap<PlanoTrabalho, PlanoTrabalhoDto>()
                .ForMember(dest => dest.Arquivos, options =>
                    options.MapFrom(src => src.Proposta.Arquivos.Select(a => a.Arquivo)));
            CreateMap<PlanoTrabalhoRequest, PlanoTrabalho>();

            CreateMap<PropostaProdutoRequest, Produto>();
            CreateMap<Produto, PropostaProdutoDto>()
                .ForMember(dest => dest.FaseCadeia, opt => opt.MapFrom(src => src.FaseCadeia.Nome))
                .ForMember(dest => dest.ProdutoTipo, opt => opt.MapFrom(src => src.ProdutoTipo.Nome))
                .ForMember(dest => dest.TipoDetalhado, opt => opt.MapFrom(src => src.TipoDetalhado.Nome))
                ;

            CreateMap<Etapa, EtapaDto>()
                .ForMember(dest => dest.Produto, opt => opt.MapFrom(src => src.Produto.Titulo));
            CreateMap<EtapaRequest, Etapa>()
                .ForMember(d => d.Meses, o => o.MapFrom(s =>
                    new int[s.MesFinal - s.MesInicio + 1].Select((n, i) => s.MesInicio + i)
                ));


            CreateMap<Escopo, PropostaEscopoDto>().ReverseMap();
            CreateMap<Meta, PropostaEscopoDto.MetaDto>().ReverseMap();

            CreateMap<Risco, PropostaRiscoDto>();
            CreateMap<RiscoRequest, Risco>();

            CreateMap<RecursoHumano, RecursoHumanoDto>()
                .ForMember(dest => dest.Empresa, opt =>
                    opt.MapFrom(src => src.Empresa.Nome));
            CreateMap<RecursoHumanoRequest, RecursoHumano>();

            CreateMap<AlocacaoRecursoHumanoRequest, AlocacaoRh>()
                .ForMember(d => d.HorasMeses, o => o.MapFrom(s => s.HoraMeses.Select(h => new AlocacaoRhHorasMes()
                {
                    AlocacaoRhId = s.Id,
                    Mes = h.Key,
                    Horas = h.Value
                })));

            CreateMap<AlocacaoRh, AlocacaoRecursoHumanoDto>()
                .ForMember(dest => dest.EmpresaFinanciadora, opt =>
                    opt.MapFrom(src => src.EmpresaFinanciadora.RazaoSocial))
                .ForMember(dest => dest.Recurso, opt => opt
                    .MapFrom(src => src.Recurso.NomeCompleto))
                .ForMember(dest => dest.Etapa, opt => opt
                    .MapFrom(src => src.Etapa.Ordem))
                .ForMember(dest => dest.Valor, opt =>
                    opt.MapFrom(src => src.Recurso.ValorHora * src.HorasMeses.Sum(m => m.Horas)))
                .ForMember(d => d.HoraMeses, o => o.MapFrom(s => s.HorasMeses.ToDictionary(i => i.Mes, i => i.Horas)))
                ;

            CreateMap<RecursoMaterialRequest, RecursoMaterial>();
            CreateMap<RecursoMaterial, RecursoMaterialDto>()
                .ForMember(dest => dest.CategoriaContabil, opt =>
                    opt.MapFrom(src => src.CategoriaContabil.Nome));

            CreateMap<AlocacaoRecursoMaterialRequest, AlocacaoRm>();
            CreateMap<AlocacaoRm, AlocacaoRecursoMaterialDto>()
                .ForMember(dest => dest.EmpresaFinanciadora, opt =>
                    opt.MapFrom(src => src.EmpresaFinanciadora.RazaoSocial))
                .ForMember(dest => dest.EmpresaRecebedora, opt =>
                    opt.MapFrom(src => src.EmpresaRecebedora.Nome))
                .ForMember(dest => dest.Recurso, opt => opt.MapFrom(src =>
                    src.Recurso.Nome))
                .ForMember(dest => dest.RecursoCategoria, opt => opt.MapFrom(src =>
                    src.Recurso.CategoriaContabil.Nome))
                .ForMember(dest => dest.Valor, opt =>
                    opt.MapFrom(src => src.Recurso.ValorUnitario * src.Quantidade))
                ;

            CreateMap<Captacao, Core.Models.Relatorios.Fornecedores.Proposta>();
            CreateMap<Escopo, Core.Models.Relatorios.Fornecedores.Proposta>();
            CreateMap<PlanoTrabalho, Core.Models.Relatorios.Fornecedores.Proposta>();
            CreateMap<Proposta, Core.Models.Relatorios.Fornecedores.Proposta>()
                .IncludeMembers(
                    p => p.Captacao,
                    p => p.PlanoTrabalho,
                    p => p.Escopo)
                .ForMember(dest => dest.FaseCadeia, opt =>
                    opt.MapFrom(src =>
                        src.Produtos.Find(p => p.Classificacao == ProdutoClassificacao.Final).FaseCadeia))
                .ForMember(dest => dest.Demandas, opt =>
                    opt.MapFrom(src => src.Captacao.SubTemas))
                ;

            CreateMap<Proposta, PropostaFinalizada>()
                .ForMember(dest => dest.Projeto, opt => opt.MapFrom(src =>
                    src.Captacao.Titulo))
                .ForMember(dest => dest.Fornecedor, opt => opt.MapFrom(src =>
                    src.Fornecedor.Nome))
                .ForMember(dest => dest.Cancelada, opt => opt.MapFrom(src => src
                    .Participacao == StatusParticipacao.Rejeitado));

            CreateMap<Comentario, ComentarioDto>()
                .ForMember(c => c.Author, opt => opt.MapFrom(src => src.Author.NomeCompleto));
            CreateMap<ContratoComentario, ComentarioDto>().IncludeBase<Comentario, ComentarioDto>()
                .ForMember(c => c.Files, opt => opt.MapFrom(src => src.Files.Select(f => f.File)));
            CreateMap<PlanoComentario, ComentarioDto>().IncludeBase<Comentario, ComentarioDto>()
                .ForMember(c => c.Files, opt => opt.MapFrom(src => src.Files.Select(f => f.File)));
        }
    }
}