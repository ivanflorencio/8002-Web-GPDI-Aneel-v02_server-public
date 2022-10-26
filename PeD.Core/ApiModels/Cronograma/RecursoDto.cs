using System.Collections.Generic;

namespace PeD.Core.ApiModels.Cronograma
{
    public class RecursoDto
    {
        public string Empresa { get; set; }

        public int QtdRH { get; set; }
        public double ValorRH { get; set; }

        public int QtdServTerc { get; set; }
        public double ValorServTerc { get; set; }

        public double ValorMatConsu { get; set; }
        public int QtdMatConsu { get; set; }

        public double ValorMatPerm { get; set; }
        public int QtdMatPerm { get; set; }

        public double ValorViaDia { get; set; }
        public int QtdViaDia { get; set; }

        public double ValorAudConFin { get; set; }
        public int QtdAudConFin { get; set; }

        public double ValorStartups { get; set; }
        public int QtdStartups { get; set; }
        
        public double ValorOutros { get; set; }
        public int QtdOutros { get; set; }
       
        public double Total { get; set; }
    }
}
