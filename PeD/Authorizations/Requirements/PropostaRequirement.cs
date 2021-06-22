using Microsoft.AspNetCore.Authorization;
using PeD.Core.Models.Propostas;

namespace PeD.Authorizations.Requirements
{
    public class PropostaRequirement : IAuthorizationRequirement
    {
        public bool CanEdit { get; }


        public PropostaRequirement(bool canEdit = false)
        {
            CanEdit = canEdit;
        }
    }
}