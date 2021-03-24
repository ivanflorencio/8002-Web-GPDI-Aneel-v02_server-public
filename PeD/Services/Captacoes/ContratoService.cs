using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PeD.Core.Models.Propostas;

namespace PeD.Services.Captacoes
{
    public class ContratoService
    {
        private static Dictionary<string, Func<Proposta, string>> shortcodes =
            new Dictionary<string, Func<Proposta, string>>()
            {
                {"Fornecedor.Nome", p => p.Fornecedor.Nome},
                {
                    "Fornecedor.CNPJ",
                    p => Regex.Replace(p.Fornecedor.Cnpj, @"^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$", "$1.$2.$3/$4-$5")
                },
                {"Projeto.Prazo", p => p.Duracao.ToString()},
                {
                    "Projeto.Valor", p =>
                    {
                        try
                        {
                            return p.Etapas.Sum(e =>
                                e.RecursosHumanosAlocacoes.Sum(r => r.Valor) +
                                e.RecursosMateriaisAlocacoes.Sum(r => r.Valor)).ToString("C");
                        }
                        catch (Exception e)
                        {
                            return 0.ToString("C");
                        }
                    }
                },
            };

        public static string ReplaceShortcodes(string text, Proposta proposta)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            foreach (var replacer in shortcodes)
            {
                try
                {
                    var shortcode = @$"{{{replacer.Key}}}";
                    var value = replacer.Value(proposta);
                    text = text.Replace(shortcode, value ?? "Erro!");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return text;
        }
    }
}