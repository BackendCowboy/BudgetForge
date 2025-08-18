using AutoMapper;
using BudgetForge.Application.DTOs;
using BudgetForge.Domain.Entities;

namespace BudgetForge.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // If you map Accounts:
            CreateMap<Account, AccountResponse>();

            // Map entity.Date -> dto.Timestamp
            CreateMap<Transaction, TransactionResponse>()
                .ForMember(d => d.Timestamp, o => o.MapFrom(s => s.Date));
        }
    }
}