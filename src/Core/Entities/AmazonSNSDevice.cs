using System.ComponentModel.DataAnnotations;
using Bit.Core.Utilities;


namespace Bit.Core.Entities;
public class AmazonSNSDevice: ITableObject<long>
{
    public long Id { get; set; }
    public Guid DeviceID { get; set; }
    [MaxLength(2048)]
    public string EndpointARN { get; set; }
    [MaxLength(2048)]
    public string SubscriptionARN { get; set; }
    public DateTime CreationDate { get; internal set; } = DateTime.UtcNow;

    public void SetNewId()
    {
        // int will be auto-populated
        Id = 0;
    }
}
