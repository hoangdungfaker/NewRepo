using Microsoft.Extensions.Options;
using PBL3_DUTLibrary.Data;
using PBL3_DUTLibrary.Helper;
using PBL3_DUTLibrary.Interfaces;
using MongoDB.Driver;

namespace PBL3_DUTLibrary.Services
{
	public class FeedbackService : IFeedbackService
	{
		private readonly IMongoCollection<FeedbackCustomers> _feedbacks;

		public FeedbackService(IOptions<MongoDbSettings> settings)
		{
			try
			{
				var client = new MongoClient(settings.Value.ConnectionString);
				var database = client.GetDatabase(settings.Value.DatabaseName);
				_feedbacks = database.GetCollection<FeedbackCustomers>(settings.Value.CollectionName);
			}
			catch (Exception ex)
			{
				// Log lỗi và set _feedbacks = null để tránh crash
				Console.WriteLine($"MongoDB connection failed: {ex.Message}");
				_feedbacks = null;
			}
		}
		public async Task<List<FeedbackCustomers>> GetAllFeedback()
		{
			if (_feedbacks == null)
			{
				return new List<FeedbackCustomers>(); // Trả về list rỗng nếu không kết nối được
			}
			try
			{
				return await _feedbacks
					.Find(_ => true)
					.SortByDescending(fb => fb.CreatedAt)
					.Limit(5)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error getting feedbacks: {ex.Message}");
				return new List<FeedbackCustomers>();
			}
		}

		public async Task<FeedbackCustomers> GetFeedbackById(string id)
		{
			if (_feedbacks == null) return null;
			try
			{
				return await _feedbacks.Find(x => x.Id == id).FirstOrDefaultAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error getting feedback by ID: {ex.Message}");
				return null;
			}
		}

		public async Task<FeedbackCustomers> GetFeedbackByEmail(string email)
		{
			if (_feedbacks == null) return null;
			try
			{
				return await _feedbacks.Find(x => x.email == email).FirstOrDefaultAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error getting feedback by email: {ex.Message}");
				return null;
			}
		}
		public async Task AddFeedback(FeedbackCustomers feedback)
		{
			if (_feedbacks == null) return;
			try
			{
				await _feedbacks.InsertOneAsync(feedback);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error adding feedback: {ex.Message}");
			}
		}
		public async Task<List<string>> GetFeedbackTexts()
		{
			if (_feedbacks == null)
			{
				return new List<string>();
			}
			try
			{
				var projection = Builders<FeedbackCustomers>.Projection.Include(x => x.feedback).Exclude("_id");
				var feedbacks = await _feedbacks.Find(_ => true).Project(projection).ToListAsync();
				return feedbacks.Select(fb => fb.GetValue("feedback").AsString).ToList();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error getting feedback texts: {ex.Message}");
				return new List<string>();
			}
		}
	}
}
