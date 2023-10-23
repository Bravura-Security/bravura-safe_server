using AutoMapper;

namespace Bit.Infrastructure.EntityFramework.Models;
public class HubConnection : Core.Entities.HubConnection
{
}

public class HubConnectionMapperProfile : Profile
{
    public HubConnectionMapperProfile()
    {
        CreateMap<Core.Entities.HubConnection, HubConnection>().ReverseMap();
    }
}

