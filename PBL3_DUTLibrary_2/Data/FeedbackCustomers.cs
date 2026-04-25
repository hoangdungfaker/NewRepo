using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace PBL3_DUTLibrary.Data
{
	public class FeedbackCustomers
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public string? Id { get; set; }
		public string? name { get; set; }
		public string? email { get; set; }
		public string? profilePicture { get; set; }
		public string? feedback { get; set; }
		public string? username { get; set; }
		[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
