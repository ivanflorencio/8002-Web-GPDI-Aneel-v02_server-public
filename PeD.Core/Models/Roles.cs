using System.Collections.Generic;

namespace PeD.Core.Models
{
    public class Roles
    {
        public const string Administrador = "Administrador";
        public const string User = "User";
        public const string Suprimento = "Suprimento";
        public const string Fornecedor = "Fornecedor";
        public const string Colaborador = "Colaborador";
        public const string AnalistaTecnico = "AnalistaTecnico";

        protected static List<string> _allRoles = new List<string>
        {
            Administrador,
            User,
            Colaborador,
            AnalistaTecnico,
            Suprimento,
            Fornecedor
        };

        public static List<string> AllRoles => _allRoles;
    }
}