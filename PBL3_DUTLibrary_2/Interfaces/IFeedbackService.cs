using PBL3_DUTLibrary.Data;

namespace PBL3_DUTLibrary.Interfaces
{
	public interface IFeedbackService
	{
		Task<List<FeedbackCustomers>> GetAllFeedback();
		Task<FeedbackCustomers> GetFeedbackById(string id);
		Task<FeedbackCustomers> GetFeedbackByEmail(string email);
		Task AddFeedback(FeedbackCustomers feedback);
		Task<List<string>> GetFeedbackTexts();
	}
}
