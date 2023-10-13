using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bit.Core.Entities;
public class HubConnection: ITableObject<long>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public string ConnectionId { get; set; }
	
	[Required]
    public Guid Token { get; set; }

    [Required]
    [MaxLength(32)]
    public string MessageType { get; set; }

    [Required]
    [MaxLength(2048)]
    public string MessagePayload { get; set; }

    [Required]
    public DateTime CreationDate { get; internal set; } = DateTime.UtcNow;

    [Required]
    public DateTime RevisionDate { get; internal set; } = DateTime.UtcNow;

    public void SetNewId()
    {
        // int will be auto-populated
        Id = 0;
    }
}
