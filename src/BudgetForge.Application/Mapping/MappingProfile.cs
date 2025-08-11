using System.Runtime;
using AutoMapper;
using BudgetForge.Application.DTOs;
using BudgetForge.Domain.Entities;

namespace BudgetForge.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Account, AccountResponse>();
            CreateMap<Transaction, TransactionResponse>();
        }
    }
}