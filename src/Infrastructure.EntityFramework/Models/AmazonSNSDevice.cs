using AutoMapper;

namespace Bit.Infrastructure.EntityFramework.Models;
public class AmazonSNSDevice : Core.Entities.AmazonSNSDevice
{
    public virtual Device Device { get; set; }
}

public class AmazonSNSDeviceMapperProfile : Profile
{
    public AmazonSNSDeviceMapperProfile()
    {
        CreateMap<Core.Entities.AmazonSNSDevice, AmazonSNSDevice>().ReverseMap();
    }
}
