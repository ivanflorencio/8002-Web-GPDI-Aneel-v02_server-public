using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PeD.Core.ApiModels.Cronograma;
using PeD.Core.Models.Propostas;
using TaesaCore.Interfaces;
using TaesaCore.Services;

namespace PeD.Services.Cronograma
{
    public class CronogramaService : BaseService<Proposta>
    {
        private int MOCK_QTD_MESES = 36;
        private int MOCK_QTD_EMPRESAS = 3;
        private int MOCK_QTD_ETAPAS = 12;
        private int MOCK_INICIO_ANO = 2022;
        private int MOCK_INICIO_MES = 10;
        private Random RANDOM = new Random();

        public CronogramaService(IRepository<Proposta> repository) : base(repository)
        {
        }

        private String _getNomeMock() {
            var nomes = new string[] {
                "Lorem ipsum dolor sit amet, consectetur",
                "Adipiscing Elit sed do eiusmod tempor incididunt",
                "Labore Magna Et dolore magna aliqua",
                "Enim Ad minim veniam, quis nostrud exercitation",
                "Ullamco laboris nisi ut aliquip ex ea commodo consequat",
                "Duis Aute irure dolor in reprehenderit in voluptate ",
                "Velit Pariatur cillum dolore eu fugiat nulla pariatur",
                "Excepteur Sint occaecat cupidatat non proident",
                "Sunt Mollit in culpa qui officia deserunt mollit anim id est laborum",
            };
            return nomes[RANDOM.Next(0,8)];
        }

        private List<int> _getDesembolsosMock() {
            var desembolsos = new List<int>();
            for (var index = 0; index < MOCK_QTD_MESES; index++) {
                desembolsos.Add(20000 + RANDOM.Next(0,100000));
            }
            return desembolsos;
        }

        private List<EmpresaCronogramaDto> GetEmpresas() {
            var empresas = new List<EmpresaCronogramaDto>();
            for (var index = 0; index < MOCK_QTD_EMPRESAS; index++) {
                var desembolso = _getDesembolsosMock();
                var nome = this._getNomeMock().Split(' ');
                empresas.Add(new EmpresaCronogramaDto{ Nome = nome[0]+" "+nome[1]+ " Incorporações", Desembolso = desembolso, Total = Enumerable.Sum(desembolso)});
            }
            return empresas;
        }

        private List<RecursoDto> GetRecusos() {
            
            var valores = _getDesembolsosMock();
            var recursos = new List<RecursoDto>();
            var recursoSoma = new RecursoDto();

            for (var index = 0; index < RANDOM.Next(2, MOCK_QTD_EMPRESAS); index++) {
                var nome = this._getNomeMock().Split(' ');
                var recurso = new RecursoDto();
                recurso.Empresa = nome[0]+" "+nome[1]+ " Incorporações";
                
                recurso.QtdAudConFin = RANDOM.Next(1, 20);
                recurso.QtdMatConsu = RANDOM.Next(1, 20);
                recurso.QtdViaDia =RANDOM.Next(1, 20);
                recurso.QtdRH = RANDOM.Next(1, 20);
                recurso.QtdServTerc = RANDOM.Next(1, 20);
                recurso.QtdOutros = RANDOM.Next(1, 20);

                recurso.ValorAudConFin = RANDOM.Next(5000, 10000);
                recurso.ValorMatConsu = RANDOM.Next(5000, 10000);
                recurso.ValorViaDia =RANDOM.Next(5000, 10000);
                recurso.ValorRH = RANDOM.Next(50000, 100000);
                recurso.ValorServTerc = RANDOM.Next(5000, 10000);
                recurso.ValorOutros = RANDOM.Next(5000, 10000);

                recurso.Total = recurso.ValorAudConFin+recurso.ValorMatConsu+recurso.ValorViaDia+recurso.ValorRH+recurso.ValorServTerc+recurso.ValorOutros;

                recursoSoma.QtdAudConFin += recurso.QtdAudConFin;
                recursoSoma.QtdMatConsu += recurso.QtdMatConsu;
                recursoSoma.QtdViaDia += recurso.QtdViaDia;
                recursoSoma.QtdRH += recurso.QtdRH;
                recursoSoma.QtdServTerc += recurso.QtdServTerc;
                recursoSoma.QtdOutros += recurso.QtdOutros;

                recursoSoma.ValorAudConFin += recurso.ValorAudConFin;
                recursoSoma.ValorMatConsu += recurso.ValorMatConsu;
                recursoSoma.ValorViaDia += recurso.ValorViaDia;
                recursoSoma.ValorRH += recurso.ValorRH;
                recursoSoma.ValorServTerc += recurso.ValorServTerc;
                recursoSoma.ValorOutros += recurso.ValorOutros;
                                
                recursoSoma.Total += recurso.Total;

                recursos.Add(recurso);
            }
            recursos.Add(recursoSoma);
            return recursos;
        }

        private List<EtapaCronogramaDto> GetEtapas() {
            var etapas = new List<EtapaCronogramaDto>();
            var tamanho = Decimal.ToInt32(Math.Floor((decimal) MOCK_QTD_MESES / MOCK_QTD_ETAPAS));
            for (var index = 0; index < MOCK_QTD_ETAPAS; index++) {
                etapas.Add(new EtapaCronogramaDto {
                    Numero = index + 1,
                    Etapa = _getNomeMock(),
                    Meses = Enumerable.Range(0, tamanho).ToList().ConvertAll((i) => i + 1 + tamanho * index),
                    Produto = _getNomeMock(),
                });
            }
            return etapas;
        }

        public CronogramaDto GetCronograma(Guid guid)
        {
            var cronograma = new CronogramaDto();
            cronograma.Inicio = new InicioCronogramaDto {Ano = MOCK_INICIO_ANO, Mes = MOCK_INICIO_MES, NumeroMeses = MOCK_QTD_MESES};
            cronograma.Empresas = GetEmpresas();
            cronograma.Etapas = GetEtapas();
            return cronograma;
        }

        public DetalheEtapaDto GetDetalheEtapa(Guid guid, int numeroEtapa)
        {
            var etapa = new DetalheEtapaDto();

            etapa.Etapa = _getNomeMock();
            etapa.Produto = _getNomeMock();
            etapa.InicioPeriodo = new DateTime(2022, 8, 1);
            etapa.FimPeriodo = new DateTime(2022, 11, 1);
            etapa.Recursos = GetRecusos();
            return etapa;
        }

    }
}