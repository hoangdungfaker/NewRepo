using PBL3_DUTLibrary.Models;

namespace PBL3_DUTLibrary.ViewModels
{
    public class BorrowReturnViewModel
    {
        public List<Borrow> uncompleted = new List<Borrow>();
        public List<Borrow> completed = new List<Borrow>();
        public List<ProlongRequest> uncompletedProlongRequests = new List<ProlongRequest>();
        public List<ProlongRequest> completedProlongRequests = new List<ProlongRequest>();
    }
}
